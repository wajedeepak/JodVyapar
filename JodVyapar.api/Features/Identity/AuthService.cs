using JodVyapar.Api.Common.Email;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Identity;

public sealed record RegisterResult(RegisterResponse? Response, IReadOnlyDictionary<string, string[]> Errors);

public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
    EmailNotVerified,
}

public sealed record LoginResult(LoginStatus Status, AuthResponse? Response = null);

public sealed record ResetPasswordResult(bool Succeeded, IReadOnlyDictionary<string, string[]> Errors);

/// <summary>
/// Registration and sign-in. Rule: a JWT is only ever issued to a user whose email is verified
/// (ApplicationUser.EmailConfirmed is the single source of truth) and who is not locked out.
/// </summary>
public sealed class AuthService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    EmailOtpService otpService,
    EmailQueue emailQueue,
    ILogger<AuthService> logger)
{
    /// <summary>Creates the user, their roles (Buyer, Seller or both) and company profile in one transaction, then emails a verification code.</summary>
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        // The validator has already rejected empty lists and anything other than Buyer/Seller.
        var roles = AppRoles.Ordered(request.Roles!.Select(AppRoles.FindSelfAssignable).OfType<string>());

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            ConsentAcceptedAt = DateTime.UtcNow,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return new RegisterResult(null, MapIdentityErrors(created.Errors));
        }

        var rolesAdded = await userManager.AddToRolesAsync(user, roles);
        if (!rolesAdded.Succeeded)
        {
            throw new InvalidOperationException($"Could not assign roles '{string.Join(", ", roles)}': {string.Join(" ", rolesAdded.Errors.Select(e => e.Description))}");
        }

        db.Add(new CompanyProfile
        {
            UserId = user.Id,
            CompanyName = request.CompanyName.Trim(),
            StateId = request.StateId!.Value,
            CityId = request.CityId!.Value,
            Mobile = request.Mobile.Trim(),
        });
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Registered user {UserId} as {Roles}", user.Id, string.Join(", ", roles));

        await otpService.IssueAsync(user, OtpPurpose.EmailVerification, cancellationToken);

        return new RegisterResult(new RegisterResponse(RequiresEmailVerification: true, email), new Dictionary<string, string[]>());
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        // Counts failures and locks the account per the Identity lockout settings.
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            logger.LogWarning("Login blocked for locked-out user {UserId}", user.Id);
            return new LoginResult(LoginStatus.LockedOut);
        }

        if (!result.Succeeded)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        // Checked only after the password, so this answer never reveals anything to a stranger.
        if (!user.EmailConfirmed)
        {
            logger.LogInformation("Password login for unverified user {UserId}; sending a verification code", user.Id);
            await otpService.IssueAsync(user, OtpPurpose.EmailVerification, cancellationToken);
            return new LoginResult(LoginStatus.EmailNotVerified);
        }

        return new LoginResult(LoginStatus.Succeeded, await CreateAuthResponseAsync(user, cancellationToken));
    }

    /// <summary>Verifies the email with a code and signs the user in. Null means invalid or expired (deliberately vague).</summary>
    public async Task<AuthResponse?> VerifyEmailAsync(VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null || user.EmailConfirmed || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await otpService.VerifyAsync(user.Id, OtpPurpose.EmailVerification, request.Code, cancellationToken))
        {
            return null;
        }

        await MarkEmailVerifiedAsync(user);
        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    /// <summary>Sends a new verification code if the account exists and is unverified. The caller always gets the same answer.</summary>
    public async Task ResendVerificationAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        await otpService.IssueAsync(user, OtpPurpose.EmailVerification, cancellationToken);
    }

    /// <summary>Sends a sign-in code if the account exists and is not locked out. The caller always gets the same answer.</summary>
    public async Task RequestLoginCodeAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null)
        {
            return;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login code not sent to locked-out user {UserId}", user.Id);
            return;
        }

        await otpService.IssueAsync(user, OtpPurpose.Login, cancellationToken);
    }

    /// <summary>Signs in with an emailed code. Success also proves email ownership. Null means invalid or expired.</summary>
    public async Task<AuthResponse?> VerifyLoginCodeAsync(VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await otpService.VerifyAsync(user.Id, OtpPurpose.Login, request.Code, cancellationToken))
        {
            return null;
        }

        if (!user.EmailConfirmed)
        {
            await MarkEmailVerifiedAsync(user);
        }

        logger.LogInformation("User {UserId} signed in with an email code", user.Id);
        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    /// <summary>Sends a password reset code if the account exists. The caller always gets the same answer.</summary>
    public async Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null)
        {
            return;
        }

        // Locked-out users get a code too: resetting the password is how they get back in.
        await otpService.IssueAsync(user, OtpPurpose.PasswordReset, cancellationToken);
    }

    /// <summary>
    /// Sets a new password with an emailed code. Success also verifies the email and clears any
    /// lockout; it does not sign the user in. Returns null for an unknown email or a wrong, expired,
    /// used or burnt-out code (deliberately vague), or field errors if the password breaks the policy.
    /// </summary>
    public async Task<ResetPasswordResult?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(request.Email);
        if (user is null)
        {
            return null;
        }

        // Checked before the code, so a rejected password doesn't use up one of the code's attempts.
        var policyErrors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, request.NewPassword);
            policyErrors.AddRange(validation.Errors);
        }

        if (policyErrors.Count > 0)
        {
            return new ResetPasswordResult(false, new Dictionary<string, string[]>
            {
                ["newPassword"] = policyErrors.Select(e => e.Description).Distinct().ToArray(),
            });
        }

        if (!await otpService.VerifyAsync(user.Id, OtpPurpose.PasswordReset, request.Code, cancellationToken))
        {
            return null;
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.NewPassword);
        user.EmailConfirmed = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        // Saves all of the above in one update and replaces the security stamp.
        var updated = await userManager.UpdateSecurityStampAsync(user);
        if (!updated.Succeeded)
        {
            throw new InvalidOperationException($"Could not reset the password for user {user.Id}: {string.Join(" ", updated.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Password reset for user {UserId}", user.Id);
        emailQueue.Enqueue(EmailTemplates.PasswordChanged(user.Email ?? "", user.FullName));

        return new ResetPasswordResult(true, new Dictionary<string, string[]>());
    }

    public async Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var company = await db.Set<CompanyProfile>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new CompanyProfileDto(
                p.CompanyName,
                p.StateId,
                p.State.Name,
                p.CityId,
                p.City.Name,
                p.Mobile,
                p.Gstin,
                p.User.EmailConfirmed,
                p.IsMobileVerified,
                p.IsGstinVerified))
            .FirstOrDefaultAsync(cancellationToken);

        return new MeResponse(user.Id, user.FullName, user.Email ?? "", AppRoles.Ordered(roles), user.ConsentAcceptedAt, company);
    }

    private async Task<ApplicationUser?> FindActiveUserAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null || user.IsDeleted ? null : user;
    }

    private async Task MarkEmailVerifiedAsync(ApplicationUser user)
    {
        user.EmailConfirmed = true;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw new InvalidOperationException($"Could not mark email verified for user {user.Id}: {string.Join(" ", updated.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Email verified for user {UserId}", user.Id);
    }

    /// <summary>
    /// A fresh token and user info carrying every role the user holds right now. Also used after a
    /// role is added, so the new role takes effect immediately.
    /// </summary>
    public async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var companyName = await db.Set<CompanyProfile>()
            .Where(p => p.UserId == user.Id)
            .Select(p => p.CompanyName)
            .FirstOrDefaultAsync(cancellationToken);

        var ordered = AppRoles.Ordered(roles);
        var (token, expiresAt) = tokenService.CreateAccessToken(user, ordered);
        return new AuthResponse(token, expiresAt, new UserDto(user.Id, user.FullName, user.Email ?? "", ordered, companyName));
    }

    /// <summary>Maps Identity errors onto request fields so the form can show them in place.</summary>
    private static Dictionary<string, string[]> MapIdentityErrors(IEnumerable<IdentityError> errors) =>
        errors
            .Select<IdentityError, (string Field, string Message)>(e => e.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => ("email", "An account with this email already exists."),
                "InvalidEmail" or "InvalidUserName" => ("email", "Please enter a valid email address."),
                _ when e.Code.StartsWith("Password", StringComparison.Ordinal) => ("password", e.Description),
                _ => ("", e.Description),
            })
            .GroupBy(e => e.Field)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).Distinct().ToArray());
}
