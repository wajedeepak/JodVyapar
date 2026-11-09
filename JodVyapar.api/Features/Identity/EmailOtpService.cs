using System.Security.Cryptography;
using System.Text;
using JodVyapar.Api.Common.Email;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JodVyapar.Api.Features.Identity;

/// <summary>Bound from the "Otp" section. HmacKey comes from user-secrets / environment variables.</summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public string HmacKey { get; init; } = "";
    public int CodeLifetimeMinutes { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;
    public int ResendCooldownSeconds { get; init; } = 60;
    public int MaxRequestsPerHour { get; init; } = 5;
}

/// <summary>
/// Issues and checks 6-digit email codes. Rules: crypto-random, valid for CodeLifetimeMinutes,
/// single use, a new code replaces older unused ones (same user + purpose), MaxAttempts tries per
/// code, a cooldown per user + purpose and an hourly cap per user. Codes are never logged.
/// </summary>
public sealed class EmailOtpService(
    AppDbContext db,
    EmailQueue emailQueue,
    IOptions<OtpOptions> options,
    ILogger<EmailOtpService> logger)
{
    private OtpOptions Rules => options.Value;

    /// <summary>Creates and emails a new code, unless the cooldown or hourly cap applies (then nothing is sent).</summary>
    public async Task<bool> IssueAsync(ApplicationUser user, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var recent = await db.Set<EmailOtp>()
            .Where(o => o.UserId == user.Id && o.CreatedAt > now.AddHours(-1))
            .Select(o => new { o.Purpose, o.CreatedAt })
            .ToListAsync(cancellationToken);

        if (recent.Any(o => o.Purpose == purpose && o.CreatedAt > now.AddSeconds(-Rules.ResendCooldownSeconds)))
        {
            logger.LogInformation("OTP not sent to user {UserId} ({Purpose}): resend cooldown", user.Id, purpose);
            return false;
        }

        if (recent.Count >= Rules.MaxRequestsPerHour)
        {
            logger.LogWarning("OTP not sent to user {UserId} ({Purpose}): hourly limit reached", user.Id, purpose);
            return false;
        }

        // Only the newest code is ever valid.
        await db.Set<EmailOtp>()
            .Where(o => o.UserId == user.Id && o.Purpose == purpose && o.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ConsumedAt, now), cancellationToken);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otp = new EmailOtp
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = [],
            ExpiresAt = now.AddMinutes(Rules.CodeLifetimeMinutes),
            CreatedAt = now,
        };
        otp.CodeHash = Hash(otp.Id, code);
        db.Add(otp);
        await db.SaveChangesAsync(cancellationToken);

        var email = user.Email ?? "";
        emailQueue.Enqueue(purpose switch
        {
            OtpPurpose.Login => EmailTemplates.LoginCode(email, user.FullName, code, Rules.CodeLifetimeMinutes),
            OtpPurpose.PasswordReset => EmailTemplates.PasswordResetCode(email, user.FullName, code, Rules.CodeLifetimeMinutes),
            _ => EmailTemplates.EmailVerificationCode(email, user.FullName, code, Rules.CodeLifetimeMinutes),
        });

        logger.LogInformation("OTP {OtpId} sent to user {UserId} ({Purpose})", otp.Id, user.Id, purpose);
        return true;
    }

    /// <summary>
    /// Checks the code against the user's newest active code and consumes it on success. Every try
    /// (right or wrong) uses one attempt; once MaxAttempts are used the code is dead.
    /// </summary>
    public async Task<bool> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var otp = await db.Set<EmailOtp>()
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Purpose == purpose && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            logger.LogInformation("OTP check failed for user {UserId} ({Purpose}): no active code", userId, purpose);
            return false;
        }

        if (otp.ExpiresAt <= now)
        {
            logger.LogInformation("OTP check failed for user {UserId} ({Purpose}): code {OtpId} expired", userId, purpose, otp.Id);
            return false;
        }

        // Reserve an attempt atomically before comparing, so parallel guesses can't exceed the limit.
        var reserved = await db.Set<EmailOtp>()
            .Where(o => o.Id == otp.Id && o.ConsumedAt == null && o.AttemptCount < Rules.MaxAttempts)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1), cancellationToken);
        if (reserved == 0)
        {
            logger.LogWarning("OTP check refused for user {UserId} ({Purpose}): code {OtpId} is locked or used", userId, purpose, otp.Id);
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(Hash(otp.Id, code), otp.CodeHash))
        {
            var attempts = otp.AttemptCount + 1;
            if (attempts >= Rules.MaxAttempts)
            {
                logger.LogWarning("OTP {OtpId} locked for user {UserId} ({Purpose}) after {Attempts} wrong attempts", otp.Id, userId, purpose, attempts);
            }
            else
            {
                logger.LogInformation("OTP check failed for user {UserId} ({Purpose}): wrong code, attempt {Attempts}", userId, purpose, attempts);
            }

            return false;
        }

        // Single use: only one request can flip ConsumedAt.
        var consumed = await db.Set<EmailOtp>()
            .Where(o => o.Id == otp.Id && o.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.ConsumedAt, now), cancellationToken);
        if (consumed == 0)
        {
            return false;
        }

        logger.LogInformation("OTP {OtpId} verified for user {UserId} ({Purpose})", otp.Id, userId, purpose);
        return true;
    }

    /// <summary>HMAC binds the code to its row, so equal codes never share a hash and a leaked table can't be brute-forced offline.</summary>
    private byte[] Hash(Guid otpId, string code) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(Rules.HmacKey), Encoding.UTF8.GetBytes($"{otpId:N}:{code}"));
}
