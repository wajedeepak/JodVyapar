using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JodVyapar.Api.Tests;

/// <summary>Registration and sign-in helpers shared by the auth test classes.</summary>
public static class TestAccounts
{
    public const string Password = "Passw0rd!";

    public static object NewRegistration(string email, string[]? roles = null, bool consent = true, string city = "Mumbai") => new
    {
        fullName = "Asha Patel",
        companyName = "Patel Textiles Pvt Ltd",
        email,
        mobile = "9876543210",
        password = Password,
        stateId = SeedIds.For("state:MH"),
        cityId = SeedIds.For($"city:MH:{city}"),
        roles = roles ?? ["Seller"],
        consentAccepted = consent,
    };

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    /// <summary>Registers an account (left unverified) and returns the emailed verification code.</summary>
    public static async Task<string> RegisterAsync(HttpClient client, ApiFactory factory, string email, string[]? roles = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", NewRegistration(email, roles));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await factory.Emails.NextCodeAsync(email);
    }

    /// <summary>Registers and verifies an account; returns the session from verify-email.</summary>
    public static async Task<AuthResponse> RegisterVerifiedAsync(HttpClient client, ApiFactory factory, string email, string[]? roles = null)
    {
        var code = await RegisterAsync(client, factory, email, roles);
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, code });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    /// <summary>The "code" extension of a ProblemDetails response.</summary>
    public static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>Sends a request with a bearer token and an optional JSON body.</summary>
    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    /// <summary>The "role" claims inside an access token, in token order.</summary>
    public static string[] RoleClaims(string accessToken) =>
        new JsonWebTokenHandler().ReadJsonWebToken(accessToken).Claims
            .Where(c => c.Type == AuthClaims.Role)
            .Select(c => c.Value)
            .ToArray();
}
