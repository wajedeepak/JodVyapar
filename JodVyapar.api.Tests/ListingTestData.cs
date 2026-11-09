using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JodVyapar.Api.Features.Catalog;
using JodVyapar.Api.Features.Requirements;

namespace JodVyapar.Api.Tests;

/// <summary>Request bodies and helpers shared by the Products and Requirements tests.</summary>
public static class ListingTestData
{
    public const string Products = "/api/v1/products";
    public const string Requirements = "/api/v1/requirements";

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3];
    public static readonly byte[] Webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 1, 2, 3];

    public static object Product(
        string title = "Combed cotton yarn",
        string category = "category:fabrics",
        decimal? priceMin = 100,
        decimal? priceMax = 150,
        decimal? moq = 50,
        string? state = "MH",
        string? city = "Pune",
        string[]? keywords = null) => new
    {
        title,
        description = "Good quality.",
        categoryId = SeedIds.For(category),
        priceMin,
        priceMax,
        unitId = SeedIds.For("unit:kg"),
        minOrderQty = moq,
        stateId = state is null ? (Guid?)null : SeedIds.For($"state:{state}"),
        cityId = city is null ? (Guid?)null : SeedIds.For($"city:{state ?? "MH"}:{city}"),
        keywords = keywords ?? ["cotton", "yarn"],
    };

    public static object Requirement(
        string title = "Need cotton yarn",
        string category = "category:fabrics",
        decimal? budgetMin = 100,
        decimal? budgetMax = 200,
        decimal? quantity = 500,
        string? state = "MH",
        string? city = "Pune",
        string[]? keywords = null,
        string? neededBy = null) => new
    {
        title,
        description = "Monthly order.",
        categoryId = SeedIds.For(category),
        budgetMin,
        budgetMax,
        quantity,
        unitId = SeedIds.For("unit:kg"),
        stateId = state is null ? (Guid?)null : SeedIds.For($"state:{state}"),
        cityId = city is null ? (Guid?)null : SeedIds.For($"city:{state ?? "MH"}:{city}"),
        keywords = keywords ?? ["cotton"],
        neededBy,
    };

    public static async Task<ProductDto> CreateProductAsync(HttpClient client, string token, object? body = null)
    {
        var response = await TestAccounts.SendAsync(client, HttpMethod.Post, Products, token, body ?? Product());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public static async Task<RequirementDto> CreateRequirementAsync(HttpClient client, string token, object? body = null)
    {
        var response = await TestAccounts.SendAsync(client, HttpMethod.Post, Requirements, token, body ?? Requirement());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RequirementDto>())!;
    }

    public static Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        string token,
        Guid productId,
        byte[] bytes,
        string fileName = "photo.png",
        string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Products}/{productId}/images")
        {
            Content = new MultipartFormDataContent { { file, "file", fileName } },
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    /// <summary>Asserts a 400 ValidationProblemDetails that has an error for the given field.</summary>
    public static async Task AssertFieldErrorAsync(HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 but got {(int)response.StatusCode}: {body}");
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _), $"No error for '{field}': {body}");
    }
}
