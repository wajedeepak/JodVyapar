using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFactory factory)
{
    private const string Password = TestAccounts.Password;
    private readonly HttpClient _client = factory.CreateClient();

    private static object NewRegistration(string email) => TestAccounts.NewRegistration(email);

    private static string UniqueEmail() => TestAccounts.UniqueEmail();

    /// <summary>Registers and verifies, so the account can sign in.</summary>
    private Task<AuthResponse> RegisterAsync(string email) => TestAccounts.RegisterVerifiedAsync(_client, factory, email);

    [Fact]
    public async Task RegisterThenVerify_ReturnsTokenAndUser()
    {
        var email = UniqueEmail();

        var auth = await RegisterAsync(email);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(email, auth.User.Email);
        Assert.Equal(["Seller"], auth.User.Roles);
        Assert.Equal("Patel Textiles Pvt Ltd", auth.User.CompanyName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsEmailError()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", NewRegistration(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal("An account with this email already exists.", Assert.Single(problem!.Errors["email"]));
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsFieldErrors()
    {
        // Admin is not self-assignable, consent is missing, and Delhi is not in Maharashtra.
        var invalid = new
        {
            fullName = "",
            companyName = "Acme",
            email = "not-an-email",
            mobile = "12345",
            password = "short",
            stateId = SeedIds.For("state:MH"),
            cityId = SeedIds.For("city:DL:New Delhi"),
            roles = new[] { "Admin" },
            consentAccepted = false,
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equivalent(
            new[] { "fullName", "email", "mobile", "password", "cityId", "roles", "consentAccepted" },
            problem!.Errors.Keys);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.Equal("Asha Patel", auth.User.FullName);
        Assert.True(auth.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Wrong-Passw0rd" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid email or password.", problem!.Detail);
    }

    [Fact]
    public async Task Login_AfterFiveFailures_IsLockedOutEvenWithCorrectPassword()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Wrong-Passw0rd" });
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Account temporarily locked.", problem!.Title);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsUserAndCompanyProfile()
    {
        var email = UniqueEmail();
        var auth = await RegisterAsync(email);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(email, me!.Email);
        Assert.Equal(["Seller"], me.Roles);
        Assert.Equal("Maharashtra", me.Company!.StateName);
        Assert.Equal("Mumbai", me.Company.CityName);
        Assert.True(me.Company.IsEmailVerified);
        Assert.False(me.Company.IsMobileVerified);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_ReturnsUnauthorized()
    {
        var auth = await RegisterAsync(UniqueEmail());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken[..^4] + "AAAA");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
