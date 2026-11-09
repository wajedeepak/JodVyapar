using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Common.Activity;

/// <summary>Records user actions in the ActivityEvents table (for analytics and, later, admin reports).</summary>
public interface IActivityLogger
{
    /// <summary>Saves one event straight away. Never throws: a failed log write must not break the user's request.</summary>
    Task LogAsync(string eventType, Guid? actorUserId, string targetType, Guid targetId, CancellationToken cancellationToken);
}

public sealed class ActivityLogger(AppDbContext db, ILogger<ActivityLogger> logger) : IActivityLogger
{
    public async Task LogAsync(string eventType, Guid? actorUserId, string targetType, Guid targetId, CancellationToken cancellationToken)
    {
        var activity = new ActivityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorUserId = actorUserId,
            TargetType = targetType,
            TargetId = targetId,
            CreatedAt = DateTime.UtcNow,
        };

        try
        {
            db.Add(activity);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Detach so a later SaveChanges in this request doesn't retry the failed insert.
            db.Entry(activity).State = EntityState.Detached;
            logger.LogWarning(ex, "Could not log {EventType} by {ActorUserId} on {TargetType} {TargetId}", eventType, actorUserId, targetType, targetId);
        }
    }
}
