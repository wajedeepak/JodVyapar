using JodVyapar.Api.Common;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Features.Health;

public sealed record HealthResponse(string Status, string Database, DateTime ServerTimeUtc);

[ApiController]
[Route(ApiRoutes.Prefix + "/health")]
public sealed class HealthController(AppDbContext db) : ControllerBase
{
    /// <summary>Reports whether the API is up and can reach the database.</summary>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var databaseReachable = await db.Database.CanConnectAsync(cancellationToken);

        var response = new HealthResponse(
            databaseReachable ? "Healthy" : "Unhealthy",
            databaseReachable ? "Connected" : "Unreachable",
            DateTime.UtcNow);

        return databaseReachable ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
