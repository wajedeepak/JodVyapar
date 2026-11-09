using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Profiles;

public enum AddRoleStatus
{
    Added,
    AlreadyHeld,
    UserNotFound,
    CompanyProfileRequired,
}

/// <summary>Response is set for Added and AlreadyHeld (a fresh token either way).</summary>
public sealed record AddRoleResult(AddRoleStatus Status, AuthResponse? Response = null);

/// <summary>The signed-in user's own profile, the public view of other users, and self-service roles.</summary>
public sealed class ProfileService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    AuthService authService,
    IActivityLogger activityLogger,
    ILogger<ProfileService> logger)
{
    public async Task<MyProfileDto?> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                Email = u.Email ?? "",
                u.EmailConfirmed,
                u.CreatedAt,
                Company = u.CompanyProfile == null
                    ? null
                    : new MyCompanyDto(
                        u.CompanyProfile.CompanyName,
                        u.CompanyProfile.StateId,
                        u.CompanyProfile.State.Name,
                        u.CompanyProfile.CityId,
                        u.CompanyProfile.City.Name,
                        u.CompanyProfile.Mobile,
                        u.CompanyProfile.Gstin,
                        u.CompanyProfile.About,
                        u.CompanyProfile.IsMobileVerified,
                        u.CompanyProfile.IsGstinVerified),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var roles = await GetRolesAsync(userId, cancellationToken);
        return new MyProfileDto(profile.Id, profile.FullName, profile.Email, roles, profile.EmailConfirmed, profile.CreatedAt, profile.Company);
    }

    /// <summary>
    /// Updates the user's name and company details, creating the company profile if the user has none
    /// (Admin-only accounts). Changing the GSTIN or mobile clears its verified flag. Null if the user is gone.
    /// </summary>
    public async Task<MyProfileDto?> UpdateMyProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName;

        // Includes a soft-deleted row: UserId is unique, so it is restored rather than duplicated.
        var company = await db.Set<CompanyProfile>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (company is null)
        {
            company = new CompanyProfile { UserId = user.Id, CompanyName = request.CompanyName, Mobile = request.Mobile };
            db.Add(company);
        }
        else if (company.IsDeleted)
        {
            company.IsDeleted = false;
            company.DeletedAt = null;
        }

        if (!string.Equals(company.Gstin, request.Gstin, StringComparison.Ordinal))
        {
            // A changed GSTIN has not been verified (verification itself comes in a later phase).
            company.IsGstinVerified = false;
        }

        if (!string.Equals(company.Mobile, request.Mobile, StringComparison.Ordinal))
        {
            company.IsMobileVerified = false;
        }

        company.CompanyName = request.CompanyName;
        company.Mobile = request.Mobile;
        company.StateId = request.StateId!.Value;
        company.CityId = request.CityId!.Value;
        company.Gstin = request.Gstin;
        company.About = request.About;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Profile updated for user {UserId}", userId);

        return await GetMyProfileAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Gives the user the Buyer or Seller role and returns a fresh token that carries it. Adding a role
    /// the user already holds changes nothing (still returns a fresh token). Requires a company profile,
    /// because buying and selling both happen on behalf of a company.
    /// </summary>
    public async Task<AddRoleResult> AddRoleAsync(Guid userId, AddRoleRequest request, CancellationToken cancellationToken)
    {
        // The validator has already rejected anything other than Buyer/Seller.
        var role = AppRoles.FindSelfAssignable(request.Role)!;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
        {
            return new AddRoleResult(AddRoleStatus.UserNotFound);
        }

        if (await userManager.IsInRoleAsync(user, role))
        {
            return new AddRoleResult(AddRoleStatus.AlreadyHeld, await authService.CreateAuthResponseAsync(user, cancellationToken));
        }

        if (!await db.Set<CompanyProfile>().AnyAsync(p => p.UserId == userId, cancellationToken))
        {
            return new AddRoleResult(AddRoleStatus.CompanyProfileRequired);
        }

        var added = await userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded)
        {
            throw new InvalidOperationException($"Could not add role '{role}' to user {userId}: {string.Join(" ", added.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("User {UserId} added the {Role} role", userId, role);
        return new AddRoleResult(AddRoleStatus.Added, await authService.CreateAuthResponseAsync(user, cancellationToken));
    }

    /// <summary>
    /// The public view of a user, or null if they don't exist, are disabled (soft-deleted), haven't
    /// verified their email, or have no company profile. Logs ProfileViewed when the viewer isn't the owner.
    /// </summary>
    public async Task<PublicProfileDto?> GetPublicProfileAsync(Guid viewerUserId, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted && u.EmailConfirmed && u.CompanyProfile != null)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.CompanyProfile!.CompanyName,
                CityName = u.CompanyProfile.City.Name,
                StateName = u.CompanyProfile.State.Name,
                u.CompanyProfile.About,
                u.CompanyProfile.IsGstinVerified,
                u.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        // Only marketplace roles are public; an Admin role is never advertised.
        var roles = (await GetRolesAsync(userId, cancellationToken)).Where(r => r != AppRoles.Admin).ToList();

        if (viewerUserId != userId)
        {
            await activityLogger.LogAsync(ActivityEventTypes.ProfileViewed, viewerUserId, ActivityTargetTypes.User, userId, cancellationToken);
        }

        return new PublicProfileDto(
            profile.Id,
            profile.FullName,
            profile.CompanyName,
            profile.CityName,
            profile.StateName,
            roles,
            profile.About,
            IsEmailVerified: true,
            profile.IsGstinVerified,
            profile.CreatedAt);
    }

    /// <summary>Whether the user's public profile and listings can be shown (same rule as <see cref="GetPublicProfileAsync"/>).</summary>
    public Task<bool> IsPublicAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted && u.EmailConfirmed && u.CompanyProfile != null, cancellationToken);

    private async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(cancellationToken);

        return AppRoles.Ordered(roles);
    }
}
