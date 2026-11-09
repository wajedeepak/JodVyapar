using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Tests;

/// <summary>Forgot password (email code) and reset password.</summary>
[Collection(ApiCollection.Name)]
public sealed class PasswordResetTests(ApiFactory factory)
{
    private const string NewPassword = "N3wSecret99";
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> ForgotAsync(string email) =>
        _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });

    private Task<HttpResponseMessage> ResetAsync(string email, string code, string newPassword = NewPassword) =>
        _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, code, newPassword });

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

    private static async Task AssertInvalidCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AuthErrorCodes.InvalidOrExpiredCode, await TestAccounts.ProblemCodeAsync(response));
    }

    /// <summary>A verified account with a reset code already emailed; returns the code.</summary>
    private async Task<string> VerifiedAccountWithResetCodeAsync(string email)
    {
        await TestAccounts.RegisterVerifiedAsync(_client, factory, email);
        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(email)).StatusCode);
        return await factory.Emails.NextCodeAsync(email);
    }

    [Fact]
    public async Task ForgotPassword_ForExistingAndUnknownEmail_ReturnsSameResponse()
    {
        var known = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterVerifiedAsync(_client, factory, known);
        var unknown = TestAccounts.UniqueEmail();

        var forKnown = await ForgotAsync(known);
        var forUnknown = await ForgotAsync(unknown);

        Assert.Equal(HttpStatusCode.Accepted, forKnown.StatusCode);
        Assert.Equal(forKnown.StatusCode, forUnknown.StatusCode);
        Assert.Equal(await forKnown.Content.ReadAsStringAsync(), await forUnknown.Content.ReadAsStringAsync());

        var email = await factory.Emails.NextEmailAsync(known);
        Assert.Equal("Your JodVyapar password reset code", email.Subject);
        await factory.Emails.AssertNoNewEmailAsync(unknown);
    }

    [Fact]
    public async Task ResetPassword_WithValidCode_ChangesPasswordAndNotifies()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await VerifiedAccountWithResetCodeAsync(email);

        var response = await ResetAsync(email, code);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
        Assert.Equal("Your JodVyapar password was changed", (await factory.Emails.NextEmailAsync(email)).Subject);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, TestAccounts.Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_VerifiesEmailAndClearsLockout()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterAsync(_client, factory, email); // left unverified
        await factory.WithDbAsync(db => db.Set<ApplicationUser>()
            .Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.LockoutEnd, DateTimeOffset.UtcNow.AddMinutes(15))
                .SetProperty(u => u.AccessFailedCount, 5)));

        await ForgotAsync(email);
        var code = await factory.Emails.NextCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);

        // A locked or unverified account would get 401/403 here.
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredCode_Fails()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await VerifiedAccountWithResetCodeAsync(email);
        await factory.WithDbAsync(db => db.Set<EmailOtp>()
            .Where(o => o.User.Email == email && o.Purpose == OtpPurpose.PasswordReset)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ExpiresAt, DateTime.UtcNow.AddSeconds(-1))));

        await AssertInvalidCodeAsync(await ResetAsync(email, code));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, TestAccounts.Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithUsedCode_Fails()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await VerifiedAccountWithResetCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);

        await AssertInvalidCodeAsync(await ResetAsync(email, code, "An0therPass1"));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithWrongCode_Fails()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await VerifiedAccountWithResetCodeAsync(email);

        await AssertInvalidCodeAsync(await ResetAsync(email, WrongCode(code)));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, TestAccounts.Password)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithOtherPurposeCode_Fails()
    {
        var email = TestAccounts.UniqueEmail();
        await TestAccounts.RegisterVerifiedAsync(_client, factory, email);
        await _client.PostAsJsonAsync("/api/v1/auth/login/otp/request", new { email });
        var loginCode = await factory.Emails.NextCodeAsync(email);

        await AssertInvalidCodeAsync(await ResetAsync(email, loginCode));
    }

    [Fact]
    public async Task ResetPassword_ForUnknownEmail_ReturnsSameInvalidCodeResponse()
    {
        await AssertInvalidCodeAsync(await ResetAsync(TestAccounts.UniqueEmail(), "123456"));
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ReturnsFieldErrorAndKeepsCode()
    {
        var email = TestAccounts.UniqueEmail();
        var code = await VerifiedAccountWithResetCodeAsync(email);

        var weak = await ResetAsync(email, code, "weakpass");

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        var problem = await weak.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("newPassword", problem!.Errors.Keys);

        // The rejected password didn't spend the code.
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_FollowsResendCooldown()
    {
        var email = TestAccounts.UniqueEmail();
        await VerifiedAccountWithResetCodeAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, (await ForgotAsync(email)).StatusCode);
        await factory.Emails.AssertNoNewEmailAsync(email);
    }
}
