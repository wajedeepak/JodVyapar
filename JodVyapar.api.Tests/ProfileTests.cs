using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Profiles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests(ApiFactory factory)
{
    private const string MeUrl = "/api/v1/profile/me";
    private readonly HttpClient _client = factory.CreateClient();

    private Task<AuthResponse> RegisterAsync(string[]? roles = null) =>
        TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), roles);

    private static object ValidUpdate(
        string mobile = "9123456780",
        string? gstin = "27ABCDE1234F1Z5",
        string city = "Pune",
        string state = "MH",
        string? about = "We make cotton fabric.") => new
    {
        fullName = "Asha P. Patel",
        companyName = "Patel Fabrics LLP",
        mobile,
        stateId = SeedIds.For($"state:{state}"),
        cityId = SeedIds.For($"city:{state}:{city}"),
        gstin,
        about,
    };

    [Fact]
    public async Task GetMe_ReturnsFullPrivateProfile()
    {
        var auth = await RegisterAsync(["Buyer", "Seller"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Get, MeUrl, auth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.Equal(auth.User.Id, me!.Id);
        Assert.Equal(auth.User.Email, me.Email);
        Assert.Equal(["Buyer", "Seller"], me.Roles);
        Assert.True(me.IsEmailVerified);
        Assert.Equal("Patel Textiles Pvt Ltd", me.Company!.CompanyName);
        Assert.Equal("9876543210", me.Company.Mobile);
        Assert.Equal("Mumbai", me.Company.CityName);
        Assert.Equal("Maharashtra", me.Company.StateName);
        Assert.Null(me.Company.Gstin);
        Assert.False(me.Company.IsGstinVerified);
    }

    [Fact]
    public async Task UpdateMe_TrimsAndSaves_AndKeepsEmailAndRolesReadOnly()
    {
        var auth = await RegisterAsync(["Seller"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, new
        {
            fullName = "  Asha P. Patel ",
            companyName = " Patel Fabrics LLP ",
            mobile = " 9123456780 ",
            stateId = SeedIds.For("state:MH"),
            cityId = SeedIds.For("city:MH:Pune"),
            gstin = " 27abcde1234f1z5 ",
            about = "  We make cotton fabric.  ",
            // Not part of the request; must be ignored.
            email = "hijack@example.com",
            roles = new[] { "Admin" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.Equal("Asha P. Patel", me!.FullName);
        Assert.Equal(auth.User.Email, me.Email);
        Assert.Equal(["Seller"], me.Roles);
        Assert.Equal("Patel Fabrics LLP", me.Company!.CompanyName);
        Assert.Equal("9123456780", me.Company.Mobile);
        Assert.Equal("Pune", me.Company.CityName);
        Assert.Equal("27ABCDE1234F1Z5", me.Company.Gstin);
        Assert.Equal("We make cotton fabric.", me.Company.About);

        var reloaded = await (await TestAccounts.SendAsync(_client, HttpMethod.Get, MeUrl, auth.AccessToken))
            .Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.Equivalent(me, reloaded, strict: true);
    }

    [Fact]
    public async Task UpdateMe_WithBlankOptionalFields_StoresNull()
    {
        var auth = await RegisterAsync();

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(gstin: "  ", about: ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.Null(me!.Company!.Gstin);
        Assert.Null(me.Company.About);
    }

    [Fact]
    public async Task UpdateMe_ChangingGstin_KeepsItUnverified()
    {
        var auth = await RegisterAsync();
        await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(gstin: "27ABCDE1234F1Z5"));
        await SetGstinVerifiedAsync(auth.User.Id);

        // Same GSTIN: the (pretend) verification stands.
        var same = await (await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(gstin: "27abcde1234f1z5")))
            .Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.True(same!.Company!.IsGstinVerified);

        var changed = await (await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(gstin: "29ABCDE1234F1Z5")))
            .Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.False(changed!.Company!.IsGstinVerified);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidFields_ReturnsFieldErrors()
    {
        var auth = await RegisterAsync();

        // Mobile starts with 5, GSTIN is malformed, Bengaluru is not in Maharashtra, About is too long.
        var response = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, new
        {
            fullName = "",
            companyName = "Patel Fabrics",
            mobile = "5123456789",
            stateId = SeedIds.For("state:MH"),
            cityId = SeedIds.For("city:KA:Bengaluru"),
            gstin = "27ABCDE1234F1X5",
            about = new string('a', 501),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equivalent(new[] { "fullName", "mobile", "cityId", "gstin", "about" }, problem!.Errors.Keys);
        Assert.Equal("Please enter a valid 10-digit Indian mobile number.", Assert.Single(problem.Errors["mobile"]));
        Assert.Equal("Please choose a city in the selected state.", Assert.Single(problem.Errors["cityId"]));
    }

    [Theory]
    [InlineData("98765")]
    [InlineData("98765432101")]
    [InlineData("+919876543210")]
    [InlineData("98765 43210")]
    public async Task UpdateMe_WithBadMobile_IsRejected(string mobile)
    {
        var auth = await RegisterAsync();

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(mobile: mobile));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["mobile"], problem!.Errors.Keys);
    }

    [Theory]
    [InlineData("27ABCDE1234F1Z")]
    [InlineData("2AABCDE1234F1Z5")]
    [InlineData("27ABCDE1234F0Z5")]
    [InlineData("27ABCDE1234F1Z5X")]
    public async Task UpdateMe_WithBadGstin_IsRejected(string gstin)
    {
        var auth = await RegisterAsync();

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, auth.AccessToken, ValidUpdate(gstin: gstin));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["gstin"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task PublicProfile_ShowsBusinessInfo_AndHidesContactDetails()
    {
        var owner = await RegisterAsync(["Buyer", "Seller"]);
        await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, owner.AccessToken, ValidUpdate(gstin: "27ABCDE1234F1Z5"));
        var viewer = await RegisterAsync(["Buyer"]);

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{owner.User.Id}", viewer.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<PublicProfileDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Asha P. Patel", profile!.FullName);
        Assert.Equal("Patel Fabrics LLP", profile.CompanyName);
        Assert.Equal("Pune", profile.CityName);
        Assert.Equal("Maharashtra", profile.StateName);
        Assert.Equal(["Buyer", "Seller"], profile.Roles);
        Assert.Equal("We make cotton fabric.", profile.About);
        Assert.True(profile.IsEmailVerified);
        Assert.False(profile.IsGstinVerified);

        // No contact details, by field name or by value.
        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();
        Assert.DoesNotContain("email", fields);
        Assert.DoesNotContain("mobile", fields);
        Assert.DoesNotContain("gstin", fields);
        Assert.DoesNotContain(owner.User.Email, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("9123456780", json);
        Assert.DoesNotContain("27ABCDE1234F1Z5", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicProfile_ForUnknownUser_IsNotFound()
    {
        var viewer = await RegisterAsync();

        var response = await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{Guid.NewGuid()}", viewer.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicProfile_ForUnverifiedOrDisabledUser_IsNotFound()
    {
        var viewer = await RegisterAsync();
        var unverifiedEmail = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, unverifiedEmail);
        var disabled = await RegisterAsync();

        Guid unverifiedId = default;
        await factory.WithDbAsync(async db =>
        {
            unverifiedId = await db.Users.Where(u => u.Email == unverifiedEmail).Select(u => u.Id).SingleAsync();
            await db.Users.Where(u => u.Id == disabled.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true));
        });

        foreach (var userId in new[] { unverifiedId, disabled.User.Id })
        {
            var response = await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{userId}", viewer.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task PublicProfile_LogsProfileViewed_OnlyWhenViewerIsNotTheOwner()
    {
        var owner = await RegisterAsync();
        var viewer = await RegisterAsync();

        await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{owner.User.Id}", owner.AccessToken);
        await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{owner.User.Id}", viewer.AccessToken);

        await factory.WithDbAsync(async db =>
        {
            var events = await db.Set<ActivityEvent>().Where(e => e.TargetId == owner.User.Id).ToListAsync();
            var viewed = Assert.Single(events);
            Assert.Equal(ActivityEventTypes.ProfileViewed, viewed.EventType);
            Assert.Equal(viewer.User.Id, viewed.ActorUserId);
            Assert.Equal(ActivityTargetTypes.User, viewed.TargetType);
        });
    }

    [Fact]
    public async Task UserWithoutCompanyProfile_MustAddCompanyDetailsBeforeAddingARole()
    {
        var (userId, token) = await CreateAdminWithoutCompanyAsync();

        var me = await (await TestAccounts.SendAsync(_client, HttpMethod.Get, MeUrl, token)).Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.Null(me!.Company);
        Assert.Equal(["Admin"], me.Roles);

        var blocked = await TestAccounts.SendAsync(_client, HttpMethod.Post, "/api/v1/profile/me/roles", token, new { role = "Buyer" });
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal(ProfileErrorCodes.CompanyProfileRequired, await TestAccounts.ProblemCodeAsync(blocked));

        var updated = await TestAccounts.SendAsync(_client, HttpMethod.Put, MeUrl, token, ValidUpdate());
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.NotNull((await updated.Content.ReadFromJsonAsync<MyProfileDto>())!.Company);

        var added = await TestAccounts.SendAsync(_client, HttpMethod.Post, "/api/v1/profile/me/roles", token, new { role = "Buyer" });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal(["Buyer", "Admin"], (await added.Content.ReadFromJsonAsync<AuthResponse>())!.User.Roles);

        // The public view never advertises the Admin role.
        var viewer = await RegisterAsync();
        var profile = await (await TestAccounts.SendAsync(_client, HttpMethod.Get, $"/api/v1/profile/{userId}", viewer.AccessToken))
            .Content.ReadFromJsonAsync<PublicProfileDto>();
        Assert.Equal(["Buyer"], profile!.Roles);
    }

    [Theory]
    [InlineData("GET", MeUrl)]
    [InlineData("PUT", MeUrl)]
    [InlineData("POST", "/api/v1/profile/me/roles")]
    [InlineData("GET", "/api/v1/profile/00000000-0000-0000-0000-000000000001")]
    public async Task ProfileEndpoints_WithoutToken_AreUnauthorized(string method, string url)
    {
        var response = await TestAccounts.SendAsync(_client, new HttpMethod(method), url, token: null, method == "GET" ? null : ValidUpdate());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task SetGstinVerifiedAsync(Guid userId) =>
        factory.WithDbAsync(db => db.Set<CompanyProfile>()
            .Where(p => p.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsGstinVerified, true)));

    /// <summary>An Admin-only account (no company profile), like the seeded admin. Returns its Id and token.</summary>
    private async Task<(Guid UserId, string Token)> CreateAdminWithoutCompanyAsync()
    {
        var email = TestAccounts.UniqueEmail();
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Site Admin",
                ConsentAcceptedAt = DateTime.UtcNow,
            };
            Assert.True((await userManager.CreateAsync(user, TestAccounts.Password)).Succeeded);
            Assert.True((await userManager.AddToRoleAsync(user, AppRoles.Admin)).Succeeded);
        }

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = TestAccounts.Password });
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (auth.User.Id, auth.AccessToken);
    }
}
