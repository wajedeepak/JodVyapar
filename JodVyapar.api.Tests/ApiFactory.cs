using JodVyapar.Api.Common.Email;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JodVyapar.Api.Tests;

/// <summary>
/// Hosts the API in-memory against a throwaway LocalDB database that is built from the
/// numbered scripts in /Database (exactly as a real environment is) when the test run starts,
/// and dropped when it ends. Emails go to <see cref="Emails"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string ConnectionString { get; } =
        $"Server=(localdb)\\MSSQLLocalDB;Database=JodVyapar_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-not-a-real-secret-0123456789");
        builder.UseSetting("Otp:HmacKey", "integration-tests-otp-hmac-key-not-a-real-secret-0123456789");
        builder.UseSetting("Email:Provider", "Smtp");
        builder.UseSetting("Email:Smtp:Host", "localhost");

        // Every test client shares one IP; keep the per-IP limiter out of the way.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IEmailSender>(Emails));

            // Test-only controllers (RoleProbeController) that exercise the real authorization policies.
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Creates the empty database only; the tables come from the scripts.
        await db.GetService<IRelationalDatabaseCreator>().CreateAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        foreach (var script in DatabaseScripts.SchemaAndSeed())
        {
            foreach (var batch in DatabaseScripts.Batches(script))
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>Runs a direct database change, e.g. to age a code instead of waiting.</summary>
    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
