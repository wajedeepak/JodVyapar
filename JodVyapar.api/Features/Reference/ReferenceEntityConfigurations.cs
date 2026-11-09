using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JodVyapar.Api.Features.Reference;

// Reference rows are seeded by Database/02_SeedData.sql.

internal sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("States");
        builder.Property(s => s.Name).HasMaxLength(100);
        builder.Property(s => s.Code).HasMaxLength(2).IsUnicode(false);
        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName("UX_States_Name");
        builder.HasIndex(s => s.Code).IsUnique().HasDatabaseName("UX_States_Code");
    }
}

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.HasIndex(c => new { c.StateId, c.Name }).IsUnique().HasDatabaseName("UX_Cities_StateId_Name");
        builder.HasOne(c => c.State)
            .WithMany(s => s.Cities)
            .HasForeignKey(c => c.StateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.Slug).HasMaxLength(120).IsUnicode(false);
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("UX_Categories_Slug");
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
