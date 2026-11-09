using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JodVyapar.Api.Features.Requirements;

public enum RequirementStatus
{
    Active,
    Paused,
    Fulfilled,
    Expired,
    Removed,
}

public class Requirement : AuditableEntity, ISoftDeletable
{
    public Guid BuyerUserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Always a subcategory (a category with a parent).</summary>
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public Guid StateId { get; set; }
    public State State { get; set; } = null!;

    public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    /// <summary>Comma-separated, written by KeywordNormalizer only.</summary>
    public string? Keywords { get; set; }

    public DateOnly? NeededBy { get; set; }
    public RequirementStatus Status { get; set; } = RequirementStatus.Active;
    public DateTime ExpiresAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

internal sealed class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("Requirements");
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property(r => r.Title).HasMaxLength(120);
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.BudgetMin).HasPrecision(12, 2);
        builder.Property(r => r.BudgetMax).HasPrecision(12, 2);
        builder.Property(r => r.Quantity).HasPrecision(12, 2);
        builder.Property(r => r.Keywords).HasMaxLength(500);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);

        builder.HasIndex(r => new { r.CategoryId, r.Status });
        builder.HasIndex(r => new { r.StateId, r.CityId });
        builder.HasIndex(r => r.BuyerUserId);
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.BuyerUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.Unit).WithMany().HasForeignKey(r => r.UnitId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.State).WithMany().HasForeignKey(r => r.StateId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(r => r.City).WithMany().HasForeignKey(r => r.CityId).OnDelete(DeleteBehavior.NoAction);
    }
}
