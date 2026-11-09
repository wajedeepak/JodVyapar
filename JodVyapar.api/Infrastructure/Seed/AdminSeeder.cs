using JodVyapar.Api.Common;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Identity;

namespace JodVyapar.Api.Infrastructure.Seed;

/// <summary>
/// Creates the Admin user from configuration (AdminSeed:Email / AdminSeed:Password, set via user-secrets)
/// on startup. Never overwrites an existing user's password.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminSeeder));

        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];
        if (IsUnset(email) || IsUnset(password))
        {
            logger.LogWarning("AdminSeed:Email / AdminSeed:Password not configured; skipping admin user seeding.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email!);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = configuration["AdminSeed:FullName"] ?? "Administrator",
                ConsentAcceptedAt = DateTime.UtcNow,
            };

            var created = await userManager.CreateAsync(user, password!);
            if (!created.Succeeded)
            {
                logger.LogError("Could not create admin user {Email}: {Errors}", email, string.Join(" ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created admin user {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, AppRoles.Admin);
        }
    }

    private static bool IsUnset(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == AppConfigurationExtensions.SecretPlaceholder;
}
