using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Hosting;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class AdminSeedTests(ApiFactory factory)
{
    [Fact]
    public async Task Startup_WithAdminSeedConfig_CreatesAdminWhoCanLogIn()
    {
        // A second host on the already-migrated test database, started with AdminSeed settings.
        await using var seeded = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AdminSeed:Email", "admin@jodvyapar.test");
            builder.UseSetting("AdminSeed:Password", "Admin12345");
        });
        var client = seeded.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@jodvyapar.test", password = "Admin12345" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(["Admin"], auth.User.Roles);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var me = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<MeResponse>();
        Assert.Null(me!.Company);
    }
}
