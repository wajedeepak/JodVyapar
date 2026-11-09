using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JodVyapar.Api.Features.Catalog;

public enum ProductStatus
{
    Active,
    Paused,
    Removed,
}

public class Product : AuditableEntity, ISoftDeletable
{
    public Guid SellerUserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Always a subcategory (a category with a parent).</summary>
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal PriceMin { get; set; }
    public decimal? PriceMax { get; set; }

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal MinOrderQty { get; set; }

    public Guid StateId { get; set; }
    public State State { get; set; } = null!;

    public Guid CityId { get; set; }
    public City City { get; set; } = null!;

    /// <summary>Comma-separated, written by KeywordNormalizer only.</summary>
    public string? Keywords { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public List<ProductImage> Images { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>One of up to three images of a product. The file itself lives in IFileStorage.</summary>
public class ProductImage : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public required string StoragePath { get; set; }
    public required string ContentType { get; set; }
    public int SortOrder { get; set; }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Title).HasMaxLength(120);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.PriceMin).HasPrecision(12, 2);
        builder.Property(p => p.PriceMax).HasPrecision(12, 2);
        builder.Property(p => p.MinOrderQty).HasPrecision(12, 2);
        builder.Property(p => p.Keywords).HasMaxLength(500);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);

        builder.HasIndex(p => new { p.CategoryId, p.Status });
        builder.HasIndex(p => new { p.StateId, p.CityId });
        builder.HasIndex(p => p.SellerUserId);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.SellerUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.Unit).WithMany().HasForeignKey(p => p.UnitId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.State).WithMany().HasForeignKey(p => p.StateId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(p => p.City).WithMany().HasForeignKey(p => p.CityId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        // Images of a soft-deleted product disappear with it.
        builder.HasQueryFilter(i => !i.Product.IsDeleted);

        builder.Property(i => i.StoragePath).HasMaxLength(260);
        builder.Property(i => i.ContentType).HasMaxLength(50).IsUnicode(false);

        builder.HasIndex(i => new { i.ProductId, i.SortOrder });
        builder.HasOne(i => i.Product).WithMany(p => p.Images).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
