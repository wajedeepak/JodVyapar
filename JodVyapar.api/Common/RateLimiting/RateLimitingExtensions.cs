using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace JodVyapar.Api.Common.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>Per-IP fixed window for every /auth endpoint. Limits come from RateLimiting:Auth.</summary>
    public const string Auth = "auth";
}

public static class RateLimitingExtensions
{
    /// <summary>
    /// Built-in ASP.NET Core rate limiter, partitioned by client IP. Behind a reverse proxy, add
    /// UseForwardedHeaders first or every client shares the proxy's IP.
    /// </summary>
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "Please wait a minute and try again.",
                    },
                });
            };

            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ =>
                    {
                        // Read when a partition is first created, so tests can override the limits.
                        var section = httpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("RateLimiting:Auth");
                        return new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = section.GetValue("PermitLimit", 20),
                            Window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", 60)),
                            QueueLimit = 0,
                        };
                    }));
        });

        return services;
    }
}
