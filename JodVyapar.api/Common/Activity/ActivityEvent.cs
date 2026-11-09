using JodVyapar.Api.Features.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JodVyapar.Api.Common.Activity;

/// <summary>Event type names stored in ActivityEvents.EventType (max 50 characters).</summary>
public static class ActivityEventTypes
{
    public const string ProfileViewed = "ProfileViewed";
    public const string ProductCreated = "ProductCreated";
    public const string RequirementCreated = "RequirementCreated";
}

/// <summary>What an event is about, stored in ActivityEvents.TargetType (max 50 characters).</summary>
public static class ActivityTargetTypes
{
    public const string User = "User";
    public const string Product = "Product";
    public const string Requirement = "Requirement";
}

/// <summary>
/// One row of the append-only activity log (who did what to which thing, and when). Rows never
/// change, so there is no UpdatedAt or soft delete (same approach as EmailOtps).
/// </summary>
public class ActivityEvent
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }

    /// <summary>The user who acted; null for system or anonymous events.</summary>
    public Guid? ActorUserId { get; set; }

    public required string TargetType { get; set; }
    public Guid TargetId { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("ActivityEvents");

        builder.Property(e => e.EventType).HasMaxLength(50).IsUnicode(false);
        builder.Property(e => e.TargetType).HasMaxLength(50).IsUnicode(false);

        builder.HasIndex(e => new { e.EventType, e.CreatedAt });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
