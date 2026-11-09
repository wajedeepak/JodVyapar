using JodVyapar.Api.Common;
using JodVyapar.Api.Common.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JodVyapar.Api.Features.Identity;

[ApiController]
[Route(ApiRoutes.Prefix + "/auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    private static readonly CodeRequestedResponse CodeRequested =
        new("If an account needs a code for this email, we've sent one. It can take a minute to arrive.");

    private static readonly CodeRequestedResponse ResetCodeRequested =
        new("If an account exists for this email, we've sent a password reset code. It can take a minute to arrive.");

    /// <summary>Creates a Buyer, Seller or Buyer + Seller account with its company profile and emails a verification code. Returns no token.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result.Response is null)
        {
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(field, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    /// <summary>Verifies the email with the 6-digit code and signs the user in.</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.VerifyEmailAsync(request, cancellationToken);
        return response is null ? InvalidOrExpiredCode() : Ok(response);
    }

    /// <summary>Emails a new verification code. Always answers the same, whether or not the email is registered.</summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification(EmailRequest request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationAsync(request, cancellationToken);
        return Accepted(CodeRequested);
    }

    /// <summary>
    /// Email + password login. Returns a JWT access token and basic user info. A correct password on an
    /// unverified email returns 403 with code EMAIL_NOT_VERIFIED and emails a fresh verification code.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.Status switch
        {
            LoginStatus.Succeeded => Ok(result.Response),
            LoginStatus.LockedOut => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Account temporarily locked.",
                detail: "Too many failed attempts. Please try again in 15 minutes."),
            LoginStatus.EmailNotVerified => CodedProblem(
                StatusCodes.Status403Forbidden,
                AuthErrorCodes.EmailNotVerified,
                "Email not verified.",
                "Please verify your email address. We've sent you a new code."),
            _ => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed.",
                detail: "Invalid email or password."),
        };
    }

    /// <summary>Emails a sign-in code. Always answers the same, whether or not the email is registered.</summary>
    [HttpPost("login/otp/request")]
    [AllowAnonymous]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestLoginCode(EmailRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestLoginCodeAsync(request, cancellationToken);
        return Accepted(CodeRequested);
    }

    /// <summary>Signs in with the emailed code. Also marks the email as verified.</summary>
    [HttpPost("login/otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyLoginCode(VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.VerifyLoginCodeAsync(request, cancellationToken);
        return response is null ? InvalidOrExpiredCode() : Ok(response);
    }

    /// <summary>Emails a password reset code. Always answers the same, whether or not the email is registered.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(EmailRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(ResetCodeRequested);
    }

    /// <summary>
    /// Sets a new password with the emailed code. Also verifies the email and clears any lockout.
    /// Does not sign the user in; the client sends them to Login.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType<PasswordResetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        if (result is null)
        {
            return InvalidOrExpiredCode();
        }

        if (!result.Succeeded)
        {
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(field, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        return Ok(new PasswordResetResponse("Your password has been changed. Please sign in with your new password."));
    }

    /// <summary>The signed-in user with their company profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var me = await authService.GetMeAsync(userId, cancellationToken);
        return me is null ? Unauthorized() : Ok(me);
    }

    /// <summary>One answer for wrong, expired, used and burnt-out codes, so failures reveal nothing about the account.</summary>
    private ObjectResult InvalidOrExpiredCode() =>
        CodedProblem(
            StatusCodes.Status400BadRequest,
            AuthErrorCodes.InvalidOrExpiredCode,
            "Invalid or expired code.",
            "That code is invalid or has expired. Check it, or request a new code.");

    private ObjectResult CodedProblem(int status, string code, string title, string detail)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title, detail: detail);
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
