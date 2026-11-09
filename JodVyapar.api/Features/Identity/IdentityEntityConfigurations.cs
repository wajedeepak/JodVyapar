using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JodVyapar.Api.Features.Identity;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.FullName).HasMaxLength(100);

        // Identity creates these indexes; only the names change.
        builder.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UX_Users_NormalizedUserName");
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("IX_Users_NormalizedEmail");
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        // Roles are seeded by Database/02_SeedData.sql.
        builder.ToTable("Roles");
        builder.HasIndex(r => r.NormalizedName).HasDatabaseName("UX_Roles_NormalizedName");
    }
}

// The remaining Identity tables only get business-friendly names; columns and keys are Identity's defaults.
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("UserRoles");
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("UserClaims");
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTable("RoleClaims");
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("UserLogins");
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("UserTokens");
}

internal sealed class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
{
    public void Configure(EntityTypeBuilder<EmailOtp> builder)
    {
        builder.ToTable("EmailOtps");

        builder.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(30).IsUnicode(false);
        builder.Property(o => o.CodeHash).HasMaxLength(32);

        // Serves the "latest active code", resend cooldown and hourly cap lookups.
        builder.HasIndex(o => new { o.UserId, o.Purpose, o.CreatedAt });
        builder.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ToTable("CompanyProfiles");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.CompanyName).HasMaxLength(150);
        builder.Property(p => p.Mobile).HasMaxLength(15).IsUnicode(false);
        builder.Property(p => p.Gstin).HasMaxLength(15).IsUnicode(false);
        builder.Property(p => p.About).HasMaxLength(500);

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("UX_CompanyProfiles_UserId");
        builder.HasOne(p => p.User)
            .WithOne(u => u.CompanyProfile)
            .HasForeignKey<CompanyProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.State).WithMany().HasForeignKey(p => p.StateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.City).WithMany().HasForeignKey(p => p.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}
