namespace JodVyapar.Api.Features.Identity;

public sealed class RegisterRequest
{
    public string FullName { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Mobile { get; init; } = "";
    public string Password { get; init; } = "";
    public Guid? StateId { get; init; }
    public Guid? CityId { get; init; }

    /// <summary>"Buyer", "Seller" or both. At least one; Admin is rejected.</summary>
    public IReadOnlyList<string>? Roles { get; init; }

    public bool ConsentAccepted { get; init; }
}

public sealed class LoginRequest
{
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}

/// <summary>Body for endpoints that only take an email (resend verification, request login code).</summary>
public sealed class EmailRequest
{
    public string Email { get; init; } = "";
}

/// <summary>Body for verify-email and login/otp/verify.</summary>
public sealed class VerifyCodeRequest
{
    public string Email { get; init; } = "";
    public string Code { get; init; } = "";
}

/// <summary>Body for reset-password: the emailed code plus the new password.</summary>
public sealed class ResetPasswordRequest
{
    public string Email { get; init; } = "";
    public string Code { get; init; } = "";
    public string NewPassword { get; init; } = "";
}

/// <summary>Registration no longer signs the user in; the client goes to the Verify Email screen.</summary>
public sealed record RegisterResponse(bool RequiresEmailVerification, string Email);

/// <summary>Same body whether or not the email belongs to an account.</summary>
public sealed record CodeRequestedResponse(string Message);

/// <summary>The password was changed; the client sends the user to Login (no token is issued).</summary>
public sealed record PasswordResetResponse(string Message);

/// <summary>Machine-readable "code" values added to ProblemDetails responses.</summary>
public static class AuthErrorCodes
{
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string InvalidOrExpiredCode = "INVALID_OR_EXPIRED_CODE";
}

/// <summary>The signed-in user as the client caches it. Roles are every role held, in display order (Buyer, Seller, Admin).</summary>
public sealed record UserDto(Guid Id, string FullName, string Email, IReadOnlyList<string> Roles, string? CompanyName);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public sealed record CompanyProfileDto(
    string CompanyName,
    Guid StateId,
    string StateName,
    Guid CityId,
    string CityName,
    string Mobile,
    string? Gstin,
    bool IsEmailVerified,
    bool IsMobileVerified,
    bool IsGstinVerified);

public sealed record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    DateTime ConsentAcceptedAt,
    CompanyProfileDto? Company);
