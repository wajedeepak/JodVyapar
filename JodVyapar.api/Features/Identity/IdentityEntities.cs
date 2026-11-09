using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace JodVyapar.Api.Features.Identity;

public static class AppRoles
{
    public const string Buyer = "Buyer";
    public const string Seller = "Seller";
    public const string Admin = "Admin";

    public static readonly string[] All = [Buyer, Seller, Admin];

    /// <summary>
    /// Roles a user may give themselves (at registration, or later via POST /profile/me/roles).
    /// A user can hold Buyer, Seller or both; Admin is never self-assignable.
    /// </summary>
    public static readonly string[] SelfAssignable = [Buyer, Seller];

    /// <summary>The canonical spelling of a self-assignable role name (case-insensitive), or null.</summary>
    public static string? FindSelfAssignable(string? role) =>
        SelfAssignable.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Roles in a stable display order (Buyer, Seller, Admin), so clients never depend on database order.</summary>
    public static IReadOnlyList<string> Ordered(IEnumerable<string> roles)
    {
        var held = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return All.Where(held.Contains).ToList();
    }
}

/// <summary>
/// Authorization policy names. They check the role claims in the JWT (the roles the user actually
/// holds). The client's "active mode" (Buyer/Seller) is a UI preference and is never sent or trusted.
/// </summary>
public static class AuthPolicies
{
    public const string RequireBuyer = "RequireBuyer";
    public const string RequireSeller = "RequireSeller";
    public const string RequireAdmin = "RequireAdmin";
}

public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDeletable
{
    public required string FullName { get; set; }

    /// <summary>When the user accepted the terms / privacy notice at registration (DPDP Act record).</summary>
    public DateTime ConsentAcceptedAt { get; set; }

    public CompanyProfile? CompanyProfile { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum OtpPurpose
{
    EmailVerification,
    Login,
    PasswordReset,
}

/// <summary>
/// A one-time 6-digit code sent by email. Only an HMAC of the code is stored. Short-lived, so no
/// soft delete or UpdatedAt; a used, replaced or burnt-out code has ConsumedAt set.
/// </summary>
public class EmailOtp
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public OtpPurpose Purpose { get; set; }
    public required byte[] CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
}

/// <summary>One per Buyer/Seller user (Admin-only users have none until they fill in their company details).</summary>
public class CompanyProfile : AuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public required string CompanyName { get; set; }

    public Guid StateId { get; set; }
    public State State { get; set; } = null!;

    public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    public required string Mobile { get; set; }
    public string? Gstin { get; set; }

    // Verification flags exist for later phases; nothing sets them to true yet. Changing the mobile or
    // GSTIN resets the matching flag. Email verification lives on the user (ApplicationUser.EmailConfirmed).
    public bool IsMobileVerified { get; set; }
    public bool IsGstinVerified { get; set; }

    /// <summary>Short public description of the business (max 500 characters).</summary>
    public string? About { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
