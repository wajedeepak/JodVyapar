using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Catalog;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Requirements;
using Microsoft.EntityFrameworkCore;
using static JodVyapar.Api.Tests.ListingTestData;

namespace JodVyapar.Api.Tests;

/// <summary>GET /profile/{userId}/products and /requirements.</summary>
[Collection(ApiCollection.Name)]
public sealed class PublicListingTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<AuthResponse> RegisterAsync(params string[] roles) =>
        TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), roles);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token) =>
        TestAccounts.SendAsync(_client, method, url, token);

    private static string Url(Guid userId, string list) => $"/api/v1/profile/{userId}/{list}";

    [Fact]
    public async Task Products_ListOnlyActiveOnes()
    {
        var seller = await RegisterAsync("Seller");
        var viewer = await RegisterAsync("Buyer");
        var active = await CreateProductAsync(_client, seller.AccessToken);
        var paused = await CreateProductAsync(_client, seller.AccessToken);
        var deleted = await CreateProductAsync(_client, seller.AccessToken);
        await SendAsync(HttpMethod.Post, $"{Products}/{paused.Id}/pause", seller.AccessToken);
        await SendAsync(HttpMethod.Delete, $"{Products}/{deleted.Id}", seller.AccessToken);

        var page = await (await SendAsync(HttpMethod.Get, Url(seller.User.Id, "products"), viewer.AccessToken)).Content.ReadFromJsonAsync<PagedResult<PublicProductDto>>();

        Assert.Equal(active.Id, Assert.Single(page!.Items).Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Requirements_ListOnlyActiveAndUnexpiredOnes()
    {
        var buyer = await RegisterAsync("Buyer");
        var viewer = await RegisterAsync("Seller");
        var active = await CreateRequirementAsync(_client, buyer.AccessToken);
        var paused = await CreateRequirementAsync(_client, buyer.AccessToken);
        var fulfilled = await CreateRequirementAsync(_client, buyer.AccessToken);
        var overdue = await CreateRequirementAsync(_client, buyer.AccessToken);
        await SendAsync(HttpMethod.Post, $"{Requirements}/{paused.Id}/pause", buyer.AccessToken);
        await SendAsync(HttpMethod.Post, $"{Requirements}/{fulfilled.Id}/fulfill", buyer.AccessToken);

        // Past its expiry but still Active: the hourly job hasn't reached it yet.
        await factory.WithDbAsync(db => db.Set<Requirement>().Where(r => r.Id == overdue.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresAt, DateTime.UtcNow.AddMinutes(-5))));

        var page = await (await SendAsync(HttpMethod.Get, Url(buyer.User.Id, "requirements"), viewer.AccessToken)).Content.ReadFromJsonAsync<PagedResult<PublicRequirementDto>>();

        Assert.Equal(active.Id, Assert.Single(page!.Items).Id);
    }

    [Fact]
    public async Task Lists_NeverExposeContactDetails()
    {
        var user = await RegisterAsync("Buyer", "Seller");
        var viewer = await RegisterAsync("Buyer");
        await CreateProductAsync(_client, user.AccessToken);
        await CreateRequirementAsync(_client, user.AccessToken);

        foreach (var list in new[] { "products", "requirements" })
        {
            var json = await (await SendAsync(HttpMethod.Get, Url(user.User.Id, list), viewer.AccessToken)).Content.ReadAsStringAsync();

            Assert.DoesNotContain("9876543210", json);
            Assert.DoesNotContain(user.User.Email, json);
            Assert.DoesNotContain("mobile", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gstin", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Lists_ArePaged()
    {
        var seller = await RegisterAsync("Seller");
        var viewer = await RegisterAsync("Buyer");
        await CreateProductAsync(_client, seller.AccessToken);
        await CreateProductAsync(_client, seller.AccessToken);

        var page = await (await SendAsync(HttpMethod.Get, $"{Url(seller.User.Id, "products")}?page=1&pageSize=1", viewer.AccessToken)).Content.ReadFromJsonAsync<PagedResult<PublicProductDto>>();

        Assert.Single(page!.Items);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Lists_RejectPageSizeOverLimit()
    {
        var seller = await RegisterAsync("Seller");

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Get, $"{Url(seller.User.Id, "products")}?pageSize=51", seller.AccessToken), "pageSize");
    }

    [Fact]
    public async Task Lists_NeedASignedInUser()
    {
        var seller = await RegisterAsync("Seller");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Url(seller.User.Id, "products"))).StatusCode);
    }

    [Theory]
    [InlineData("products")]
    [InlineData("requirements")]
    public async Task Lists_Return404_ForUnknownOrUnverifiedUsers(string list)
    {
        var viewer = await RegisterAsync("Buyer");
        var unverifiedEmail = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, unverifiedEmail, ["Buyer", "Seller"]);
        var unverifiedId = Guid.Empty;
        await factory.WithDbAsync(async db => unverifiedId = await db.Users.Where(u => u.Email == unverifiedEmail).Select(u => u.Id).SingleAsync());

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, Url(Guid.NewGuid(), list), viewer.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, Url(unverifiedId, list), viewer.AccessToken)).StatusCode);
    }
}
