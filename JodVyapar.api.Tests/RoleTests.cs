using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Tests;

/// <summary>Dual roles: register as Buyer and/or Seller, add a role later, and policies that check held roles.</summary>
[Collection(ApiCollection.Name)]
public sealed class RoleTests(ApiFactory factory)
{
    private const string AddRoleUrl = "/api/v1/profile/me/roles";
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(new[] { "Buyer" }, new[] { "Buyer" })]
    [InlineData(new[] { "Seller" }, new[] { "Seller" })]
    [InlineData(new[] { "Seller", "Buyer" }, new[] { "Buyer", "Seller" })]
    [InlineData(new[] { "buyer", "BUYER" }, new[] { "Buyer" })]
    public async Task Register_WithBuyerSellerOrBoth_GivesExactlyThoseRoles(string[] requested, string[] expected)
    {
        var auth = await TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), requested);

        Assert.Equal(expected, auth.User.Roles);
        Assert.Equal(expected, TestAccounts.RoleClaims(auth.AccessToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("Buyer,Admin")]
    [InlineData("Manager")]
    public async Task Register_WithNoRolesOrANonSelfAssignableRole_IsRejected(string commaSeparatedRoles)
    {
        var email = TestAccounts.UniqueEmail();
        var roles = commaSeparatedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", TestAccounts.NewRegistration(email, roles));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["roles"], problem!.Errors.Keys);
        await factory.WithDbAsync(async db => Assert.False(await db.Users.AnyAsync(u => u.Email == email)));
    }

    [Fact]
    public async Task Register_WithRolesMissing_IsRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "Asha Patel",
            companyName = "Patel Textiles",
            email = TestAccounts.UniqueEmail(),
            mobile = "9876543210",
            password = TestAccounts.Password,
            stateId = SeedIds.For("state:MH"),
            cityId = SeedIds.For("city:MH:Mumbai"),
            consentAccepted = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("roles", problem!.Errors.Keys);
    }

    [Fact]
    public async Task AddRole_ReturnsFreshTokenWithTheNewRole_AndPersists()
    {
        var email = TestAccounts.UniqueEmail();
        var buyer = await TestAccounts.RegisterVerifiedAsync(_client, factory, email, ["Buyer"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Post, AddRoleUrl, buyer.AccessToken, new { role = "seller" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotEqual(buyer.AccessToken, auth!.AccessToken);
        Assert.Equal(["Buyer", "Seller"], auth.User.Roles);
        Assert.Equal(["Buyer", "Seller"], TestAccounts.RoleClaims(auth.AccessToken));

        // A later sign-in still has both roles.
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = TestAccounts.Password });
        var session = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(["Buyer", "Seller"], session!.User.Roles);
    }

    [Fact]
    public async Task AddRole_ThatIsAlreadyHeld_IsHarmless()
    {
        var seller = await TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), ["Seller"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Post, AddRoleUrl, seller.AccessToken, new { role = "Seller" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(["Seller"], auth!.User.Roles);
        Assert.Equal(["Seller"], TestAccounts.RoleClaims(auth.AccessToken));
        await factory.WithDbAsync(async db =>
            Assert.Equal(1, await db.UserRoles.CountAsync(ur => ur.UserId == seller.User.Id)));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("")]
    [InlineData("Owner")]
    public async Task AddRole_OtherThanBuyerOrSeller_IsRejected(string role)
    {
        var buyer = await TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), ["Buyer"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Post, AddRoleUrl, buyer.AccessToken, new { role });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal("Please choose Buyer or Seller.", Assert.Single(problem!.Errors["role"]));
        await factory.WithDbAsync(async db =>
            Assert.Equal(1, await db.UserRoles.CountAsync(ur => ur.UserId == buyer.User.Id)));
    }

    [Fact]
    public async Task AddRole_WithoutToken_IsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(AddRoleUrl, new { role = "Seller" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Policies_CheckHeldRoles_NotTheClientMode()
    {
        var buyer = await TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), ["Buyer"]);

        // The UI mode is never trusted: claiming "Seller mode" in a header or query changes nothing.
        using var claimingSellerMode = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test-probe/seller?mode=Seller");
        claimingSellerMode.Headers.Authorization = new("Bearer", buyer.AccessToken);
        claimingSellerMode.Headers.Add("X-Active-Mode", "Seller");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(claimingSellerMode)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Probe("buyer", buyer.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Probe("admin", buyer.AccessToken)).StatusCode);

        // After adding Seller, the fresh token passes the Seller policy straight away.
        var added = await TestAccounts.SendAsync(_client, HttpMethod.Post, AddRoleUrl, buyer.AccessToken, new { role = "Seller" });
        var auth = await added.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.OK, (await Probe("seller", auth!.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Probe("buyer", auth.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Policies_WithoutToken_AreUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Probe("seller", null)).StatusCode);
    }

    private Task<HttpResponseMessage> Probe(string policy, string? token) =>
        TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/test-probe/{policy}", token);
}
