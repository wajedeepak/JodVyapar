using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Requirements;

/// <summary>Marks requirements Expired once their ExpiresAt has passed. Fulfilled and deleted ones are left alone.</summary>
public sealed class RequirementExpiryService(AppDbContext db, ILogger<RequirementExpiryService> logger)
{
    /// <summary>Returns how many requirements were updated.</summary>
    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // A bulk update skips SaveChanges, so UpdatedAt is set here.
        var updated = await db.Set<Requirement>()
            .Where(r => r.ExpiresAt <= now && (r.Status == RequirementStatus.Active || r.Status == RequirementStatus.Paused))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, RequirementStatus.Expired)
                    .SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        logger.LogInformation("Requirement expiry: {Count} requirement(s) marked as Expired", updated);
        return updated;
    }
}

/// <summary>Runs <see cref="RequirementExpiryService"/> shortly after startup and then every hour.</summary>
public sealed class RequirementExpiryJob(IServiceScopeFactory scopeFactory, ILogger<RequirementExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <summary>A failed run is logged and retried at the next interval; it must not stop the API.</summary>
    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<RequirementExpiryService>().ExpireDueAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Requirement expiry run failed");
        }
    }
}
