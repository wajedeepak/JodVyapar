using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Catalog;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Reference;
using Microsoft.EntityFrameworkCore;
using static JodVyapar.Api.Tests.ListingTestData;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ProductTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<AuthResponse> RegisterAsync(params string[] roles) =>
        TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), roles);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) =>
        TestAccounts.SendAsync(_client, method, url, token, body);

    [Fact]
    public async Task Units_AreListed()
    {
        var units = await _client.GetFromJsonAsync<List<UnitDto>>("/api/v1/reference/units");

        Assert.Equal(14, units!.Count);
        Assert.Equal("kg", units[0].Symbol);
    }

    [Fact]
    public async Task Create_SavesProduct_NormalizesKeywords_AndLogsEvent()
    {
        var seller = await RegisterAsync("Seller");

        var product = await CreateProductAsync(_client, seller.AccessToken, Product(title: "  Cotton yarn  ", keywords: [" Cotton, YARN", "yarn"]));

        Assert.Equal("Cotton yarn", product.Title);
        Assert.Equal(["cotton", "yarn"], product.Keywords);
        Assert.Equal("Active", product.Status);
        Assert.Equal("Fabrics", product.CategoryName);
        Assert.Equal("Textiles", product.ParentCategoryName);
        Assert.Equal("Pune", product.CityName);
        Assert.Equal("kg", product.UnitSymbol);

        var logged = false;
        await factory.WithDbAsync(async db => logged = await db.Set<ActivityEvent>()
            .AnyAsync(e => e.EventType == ActivityEventTypes.ProductCreated && e.TargetId == product.Id && e.ActorUserId == seller.User.Id));
        Assert.True(logged);
    }

    [Fact]
    public async Task Create_WithoutLocation_UsesProfileLocation()
    {
        var seller = await RegisterAsync("Seller");

        var product = await CreateProductAsync(_client, seller.AccessToken, Product(state: null, city: null));

        Assert.Equal("Mumbai", product.CityName);
        Assert.Equal("Maharashtra", product.StateName);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);

        var response = await SendAsync(HttpMethod.Put, $"{Products}/{product.Id}", seller.AccessToken, Product(title: "New title", priceMin: 200, priceMax: null, state: "KA", city: "Bengaluru"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("New title", updated!.Title);
        Assert.Equal(200, updated.PriceMin);
        Assert.Null(updated.PriceMax);
        Assert.Equal("Bengaluru", updated.CityName);
    }

    // Role enforcement

    [Fact]
    public async Task Anonymous_IsRejected()
    {
        var response = await _client.PostAsJsonAsync(Products, Product());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BuyerOnly_CannotUseProducts()
    {
        var buyer = await RegisterAsync("Buyer");

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, Products, buyer.AccessToken, Product())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"{Products}/mine", buyer.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanCreateProductsAndRequirements()
    {
        var user = await RegisterAsync("Buyer", "Seller");

        await CreateProductAsync(_client, user.AccessToken);
        await CreateRequirementAsync(_client, user.AccessToken);
    }

    // Ownership

    [Fact]
    public async Task OtherSeller_Gets404_EverywhereOnSomeoneElsesProduct()
    {
        var owner = await RegisterAsync("Seller");
        var other = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, owner.AccessToken);
        var url = $"{Products}/{product.Id}";

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, url, other.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Put, url, other.AccessToken, Product(title: "Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, $"{url}/pause", other.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, $"{url}/activate", other.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, url, other.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(_client, other.AccessToken, product.Id, Png)).StatusCode);

        var unchanged = await (await SendAsync(HttpMethod.Get, url, owner.AccessToken)).Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(product.Title, unchanged!.Title);
        Assert.Equal("Active", unchanged.Status);
    }

    // Validation

    public static TheoryData<object, string> InvalidProducts => new()
    {
        { Product(category: "category:textiles"), "categoryId" },
        { Product(state: "KA", city: null), "cityId" },
        { Product(priceMin: 200, priceMax: 100), "priceMax" },
        { Product(priceMin: 0), "priceMin" },
        { Product(priceMin: null), "priceMin" },
        { Product(moq: 0), "minOrderQty" },
        { Product(moq: -1), "minOrderQty" },
        { Product(title: "   "), "title" },
        { Product(title: new string('x', 121)), "title" },
        { Product(keywords: ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"]), "keywords" },
        { Product(keywords: [new string('x', 41)]), "keywords" },
    };

    [Theory]
    [MemberData(nameof(InvalidProducts))]
    public async Task Create_Invalid_ReturnsFieldError(object body, string field)
    {
        var seller = await RegisterAsync("Seller");

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Post, Products, seller.AccessToken, body), field);
    }

    [Fact]
    public async Task Create_CityInAnotherState_ReturnsFieldError()
    {
        var seller = await RegisterAsync("Seller");
        var body = new
        {
            title = "Yarn",
            categoryId = SeedIds.For("category:fabrics"),
            priceMin = 10,
            unitId = SeedIds.For("unit:kg"),
            minOrderQty = 5,
            stateId = SeedIds.For("state:KA"),
            cityId = SeedIds.For("city:MH:Mumbai"),
        };

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Post, Products, seller.AccessToken, body), "cityId");
    }

    [Fact]
    public async Task Mine_RejectsUnknownStatus()
    {
        var seller = await RegisterAsync("Seller");

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Get, $"{Products}/mine?status=Bogus", seller.AccessToken), "status");
    }

    // Pause, activate, delete

    [Fact]
    public async Task PauseActivate_ChangeStatus_AndMineFilters()
    {
        var seller = await RegisterAsync("Seller");
        var kept = await CreateProductAsync(_client, seller.AccessToken);
        var paused = await CreateProductAsync(_client, seller.AccessToken);

        var pause = await SendAsync(HttpMethod.Post, $"{Products}/{paused.Id}/pause", seller.AccessToken);
        Assert.Equal("Paused", (await pause.Content.ReadFromJsonAsync<ProductDto>())!.Status);

        var active = await (await SendAsync(HttpMethod.Get, $"{Products}/mine?status=Active", seller.AccessToken)).Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.Equal(kept.Id, Assert.Single(active!.Items).Id);
        Assert.Equal(1, active.TotalCount);

        var activate = await SendAsync(HttpMethod.Post, $"{Products}/{paused.Id}/activate", seller.AccessToken);
        Assert.Equal("Active", (await activate.Content.ReadFromJsonAsync<ProductDto>())!.Status);
    }

    [Fact]
    public async Task Mine_IsPaged()
    {
        var seller = await RegisterAsync("Seller");
        for (var i = 0; i < 3; i++)
        {
            await CreateProductAsync(_client, seller.AccessToken);
        }

        var page = await (await SendAsync(HttpMethod.Get, $"{Products}/mine?page=2&pageSize=2", seller.AccessToken)).Content.ReadFromJsonAsync<PagedResult<ProductDto>>();

        Assert.Single(page!.Items);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task Delete_IsSoft_AndHidesTheProduct()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{Products}/{product.Id}", seller.AccessToken)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"{Products}/{product.Id}", seller.AccessToken)).StatusCode);
        var mine = await (await SendAsync(HttpMethod.Get, $"{Products}/mine", seller.AccessToken)).Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.Empty(mine!.Items);

        await factory.WithDbAsync(async db =>
        {
            var row = await db.Set<Product>().IgnoreQueryFilters().SingleAsync(p => p.Id == product.Id);
            Assert.True(row.IsDeleted);
            Assert.NotNull(row.DeletedAt);
            Assert.Equal(ProductStatus.Removed, row.Status);
        });
    }

    // Images

    [Theory]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.JPEG", "image/jpeg")]
    [InlineData("photo.webp", "image/webp")]
    public async Task Upload_AcceptsAllowedTypes_AndServesTheFile(string fileName, string contentType)
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        var bytes = contentType switch { "image/png" => Png, "image/jpeg" => Jpeg, _ => Webp };

        var response = await UploadAsync(_client, seller.AccessToken, product.Id, bytes, fileName, contentType);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var image = Assert.Single((await response.Content.ReadFromJsonAsync<ProductDto>())!.Images);

        var served = await _client.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(contentType, served.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await served.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("photo.gif", "image/gif")] // type not allowed
    [InlineData("photo.png", "image/jpeg")] // name and content type disagree
    [InlineData("photo.jpg", "image/jpeg")] // bytes are really a PNG
    [InlineData("photo.exe", "image/png")] // bad extension
    public async Task Upload_RejectsBadType(string fileName, string contentType)
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);

        await AssertFieldErrorAsync(await UploadAsync(_client, seller.AccessToken, product.Id, Png, fileName, contentType), "file");
    }

    [Fact]
    public async Task Upload_RejectsContentThatIsNotAnImage()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);

        await AssertFieldErrorAsync(await UploadAsync(_client, seller.AccessToken, product.Id, "<html></html>"u8.ToArray()), "file");
    }

    [Fact]
    public async Task Upload_RejectsOver2Mb()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        var bytes = new byte[(2 * 1024 * 1024) + 1];
        Png.CopyTo(bytes, 0);

        await AssertFieldErrorAsync(await UploadAsync(_client, seller.AccessToken, product.Id, bytes), "file");
    }

    [Fact]
    public async Task Upload_RejectsFourthImage()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await UploadAsync(_client, seller.AccessToken, product.Id, Png)).StatusCode);
        }

        await AssertFieldErrorAsync(await UploadAsync(_client, seller.AccessToken, product.Id, Png), "file");
    }

    [Fact]
    public async Task RemoveImage_DeletesFile_AndRenumbers()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        await UploadAsync(_client, seller.AccessToken, product.Id, Png);
        var withTwo = await (await UploadAsync(_client, seller.AccessToken, product.Id, Png)).Content.ReadFromJsonAsync<ProductDto>();
        var first = withTwo!.Images[0];

        var response = await SendAsync(HttpMethod.Delete, $"{Products}/{product.Id}/images/{first.Id}", seller.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var remaining = Assert.Single((await response.Content.ReadFromJsonAsync<ProductDto>())!.Images);
        Assert.Equal(withTwo.Images[1].Id, remaining.Id);
        Assert.Equal(0, remaining.SortOrder);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(first.Url)).StatusCode);
    }

    [Fact]
    public async Task ReorderImages_ChangesOrder_AndRejectsWrongSet()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        await UploadAsync(_client, seller.AccessToken, product.Id, Png);
        var withTwo = await (await UploadAsync(_client, seller.AccessToken, product.Id, Png)).Content.ReadFromJsonAsync<ProductDto>();
        var url = $"{Products}/{product.Id}/images/order";

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Put, url, seller.AccessToken, new { imageIds = new[] { withTwo!.Images[0].Id } }), "imageIds");

        var response = await SendAsync(HttpMethod.Put, url, seller.AccessToken, new { imageIds = new[] { withTwo.Images[1].Id, withTwo.Images[0].Id } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reordered = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(withTwo.Images[1].Id, reordered!.Images[0].Id);
    }

    [Fact]
    public async Task DeletedProduct_ImagesAreNoLongerServed()
    {
        var seller = await RegisterAsync("Seller");
        var product = await CreateProductAsync(_client, seller.AccessToken);
        var image = (await (await UploadAsync(_client, seller.AccessToken, product.Id, Png)).Content.ReadFromJsonAsync<ProductDto>())!.Images[0];

        await SendAsync(HttpMethod.Delete, $"{Products}/{product.Id}", seller.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(image.Url)).StatusCode);
    }
}
