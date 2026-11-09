using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Tests;

/// <summary>Email verification and email-code (OTP) login.</summary>
[Collection(ApiCollection.Name)]
public sealed class EmailCodeTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> PostAsync(string path, object body) =>
        _client.PostAsJsonAsync($"/api/v1/auth/{path}", body);

    private Task<HttpResponseMessage> VerifyEmailAsync(string email, string code) => PostAsync("verify-email", new { email, code });

    private Task<HttpResponseMessage> VerifyLoginCodeAsync(string email, string code) => PostAsync("login/otp/verify", new { email, code });

    private Task<HttpResponseMessage> PasswordLoginAsync(string email) => PostAsync("login", new { email, password = TestAccounts.Password });

    private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

    private static async Task AssertInvalidCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AuthErrorCodes.InvalidOrExpiredCode, await TestAccounts.ProblemCodeAsync(response));
    }

    /// <summary>Moves a user's codes back in time instead of waiting out the resend cooldown.</summary>
    private Task AgeCodesAsync(string email, int seconds) =>
        factory.WithDbAsync(db => db.Set<EmailOtp>()
            .Where(o => o.User.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAt, o => o.CreatedAt.AddSeconds(-seconds))));

    [Fact]
    public async Task Register_SendsCode_AndReturnsNoToken()
    {
        var email = TestAccounts.UniqueEmail();

        var response = await PostAsync("register", TestAccounts.NewRegistration(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("requiresEmailVerification").GetBoolean());
        Assert.Equal(email, json.RootElement.GetProperty("email").GetString());
        Assert.False(json.RootElement.TryGetProperty("accessToken", out _));
        Assert.Matches("^[0-9]{6}$", await factory.Emails.NextCodeAsync(email));
    }

    [Fact]
    public async Task VerifyEmail_WithCorrectCode_SignsInAndMarksVerified()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await TestAccounts.RegisterAsync(_client, factory, email);

        var response = await VerifyEmailAsync(email, code);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(email, auth.User.Email);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var profile = await (await _client.SendAsync(me)).Content.ReadFromJsonAsync<MeResponse>();
        Assert.True(profile!.Company!.IsEmailVerified);

        Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(email)).StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithWrongCode_ReturnsInvalidOrExpired()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await TestAccounts.RegisterAsync(_client, factory, email);

        await AssertInvalidCodeAsync(await VerifyEmailAsync(email, WrongCode(code)));

        // A wrong guess doesn't burn the code.
        Assert.Equal(HttpStatusCode.OK, (await VerifyEmailAsync(email, code)).StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithExpiredCode_ReturnsInvalidOrExpired()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await TestAccounts.RegisterAsync(_client, factory, email);
        await factory.WithDbAsync(db => db.Set<EmailOtp>()
            .Where(o => o.User.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ExpiresAt, DateTime.UtcNow.AddSeconds(-1))));

        await AssertInvalidCodeAsync(await VerifyEmailAsync(email, code));
    }

    [Fact]
    public async Task VerifyEmail_WithMalformedCode_ReturnsValidationError()
    {
        var response = await VerifyEmailAsync(TestAccounts.UniqueEmail(), "12ab5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"code\":[", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoginCode_CannotBeReused()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterVerifiedAsync(_client, factory, email);
        await PostAsync("login/otp/request", new { email });
        var code = await factory.Emails.NextCodeAsync(email);

        Assert.Equal(HttpStatusCode.OK, (await VerifyLoginCodeAsync(email, code)).StatusCode);
        await AssertInvalidCodeAsync(await VerifyLoginCodeAsync(email, code));
    }

    [Fact]
    public async Task Code_AfterFiveWrongAttempts_IsDeadEvenWithCorrectCode()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await TestAccounts.RegisterAsync(_client, factory, email);
        for (var i = 0; i < 5; i++)
        {
            await AssertInvalidCodeAsync(await VerifyEmailAsync(email, WrongCode(code)));
        }

        await AssertInvalidCodeAsync(await VerifyEmailAsync(email, code));

        // A new code works again.
        await AgeCodesAsync(email, 61);
        await PostAsync("resend-verification", new { email });
        var fresh = await factory.Emails.NextCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await VerifyEmailAsync(email, fresh)).StatusCode);
    }

    [Fact]
    public async Task Resend_WithinCooldown_SendsNothing_AfterCooldown_ReplacesOldCode()
    {
        var email = TestAccounts.UniqueEmail();
        var first = await TestAccounts.RegisterAsync(_client, factory, email);

        var early = await PostAsync("resend-verification", new { email });
        Assert.Equal(HttpStatusCode.Accepted, early.StatusCode);
        await factory.Emails.AssertNoNewEmailAsync(email);

        await AgeCodesAsync(email, 61);
        await PostAsync("resend-verification", new { email });
        var second = await factory.Emails.NextCodeAsync(email);

        if (first != second)
        {
            await AssertInvalidCodeAsync(await VerifyEmailAsync(email, first));
        }

        Assert.Equal(HttpStatusCode.OK, (await VerifyEmailAsync(email, second)).StatusCode);
    }

    [Fact]
    public async Task CodeRequests_AreCappedAtFivePerHour()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, email); // request 1
        for (var i = 2; i <= 5; i++)
        {
            await AgeCodesAsync(email, 61);
            await PostAsync("resend-verification", new { email });
            await factory.Emails.NextCodeAsync(email);
        }

        await AgeCodesAsync(email, 61);
        var sixth = await PostAsync("resend-verification", new { email });

        Assert.Equal(HttpStatusCode.Accepted, sixth.StatusCode);
        await factory.Emails.AssertNoNewEmailAsync(email);
    }

    [Fact]
    public async Task PasswordLogin_WhenUnverified_Returns403AndSendsNewCode()
    {
        var email = TestAccounts.UniqueEmail();
        var first = await TestAccounts.RegisterAsync(_client, factory, email);
        await AgeCodesAsync(email, 61);

        var response = await PasswordLoginAsync(email);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(AuthErrorCodes.EmailNotVerified, await TestAccounts.ProblemCodeAsync(response));
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
        var fresh = await factory.Emails.NextCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await VerifyEmailAsync(email, fresh)).StatusCode);
    }

    [Fact]
    public async Task PasswordLogin_WhenUnverifiedButWrongPassword_Returns401()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, email);

        var response = await PostAsync("login", new { email, password = "Wrong-Passw0rd" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OtpLogin_ForUnverifiedUser_SignsInAndMarksVerified()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, email);

        var request = await PostAsync("login/otp/request", new { email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var code = await factory.Emails.NextCodeAsync(email);
        var response = await VerifyLoginCodeAsync(email, code);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(HttpStatusCode.OK, (await PasswordLoginAsync(email)).StatusCode);
    }

    [Fact]
    public async Task OtpLogin_WithWrongCode_ReturnsInvalidOrExpired()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterVerifiedAsync(_client, factory, email);
        await PostAsync("login/otp/request", new { email });
        var code = await factory.Emails.NextCodeAsync(email);

        await AssertInvalidCodeAsync(await VerifyLoginCodeAsync(email, WrongCode(code)));
    }

    [Theory]
    [InlineData("resend-verification")]
    [InlineData("login/otp/request")]
    public async Task CodeRequest_ForUnknownEmail_LooksTheSameAsForRealAccount(string path)
    {
        var known = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, known);
        await AgeCodesAsync(known, 61);
        var unknown = TestAccounts.UniqueEmail();

        var forKnown = await PostAsync(path, new { email = known });
        var forUnknown = await PostAsync(path, new { email = unknown });

        Assert.Equal(HttpStatusCode.Accepted, forKnown.StatusCode);
        Assert.Equal(forKnown.StatusCode, forUnknown.StatusCode);
        Assert.Equal(await forKnown.Content.ReadAsStringAsync(), await forUnknown.Content.ReadAsStringAsync());
        await factory.Emails.NextCodeAsync(known);
        await factory.Emails.AssertNoNewEmailAsync(unknown);
    }

    [Fact]
    public async Task VerifyLoginCode_ForUnknownEmail_ReturnsSameInvalidCodeResponse()
    {
        await AssertInvalidCodeAsync(await VerifyLoginCodeAsync(TestAccounts.UniqueEmail(), "123456"));
    }

    [Fact]
    public async Task OtpLogin_ForLockedOutUser_SendsNoCode()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterVerifiedAsync(_client, factory, email);
        await factory.WithDbAsync(db => db.Set<ApplicationUser>()
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LockoutEnd, DateTimeOffset.UtcNow.AddMinutes(15))));

        var response = await PostAsync("login/otp/request", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await factory.Emails.AssertNoNewEmailAsync(email);
    }

    [Fact]
    public async Task AuthEndpoints_AreRateLimitedPerIp()
    {
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "2"));
        var client = limited.CreateClient();
        var body = new { email = TestAccounts.UniqueEmail() };

        await client.PostAsJsonAsync("/api/v1/auth/login/otp/request", body);
        await client.PostAsJsonAsync("/api/v1/auth/resend-verification", body);
        var third = await client.PostAsJsonAsync("/api/v1/auth/login/otp/request", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains("Too many requests.", await third.Content.ReadAsStringAsync());
    }
}
