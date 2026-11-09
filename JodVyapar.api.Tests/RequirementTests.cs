using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Requirements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static JodVyapar.Api.Tests.ListingTestData;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class RequirementTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<AuthResponse> RegisterAsync(params string[] roles) =>
        TestAccounts.RegisterVerifiedAsync(_client, factory, TestAccounts.UniqueEmail(), roles);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) =>
        TestAccounts.SendAsync(_client, method, url, token, body);

    private async Task<RequirementDto> ReadAsync(HttpMethod method, string url, string token)
    {
        var response = await SendAsync(method, url, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RequirementDto>())!;
    }

    /// <summary>Moves a requirement's expiry into the past, instead of waiting 60 days.</summary>
    private Task AgeAsync(Guid id) =>
        factory.WithDbAsync(db => db.Set<Requirement>().Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresAt, DateTime.UtcNow.AddMinutes(-5))));

    private async Task ExpireDueAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RequirementExpiryService>().ExpireDueAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Create_SetsSixtyDayExpiry_AndLogsEvent()
    {
        var buyer = await RegisterAsync("Buyer");

        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken, Requirement(keywords: ["Cotton, COTTON", " Yarn "]));

        Assert.Equal("Active", requirement.Status);
        Assert.Equal(["cotton", "yarn"], requirement.Keywords);
        Assert.InRange(requirement.ExpiresAt, DateTime.UtcNow.AddDays(59), DateTime.UtcNow.AddDays(61));

        var logged = false;
        await factory.WithDbAsync(async db => logged = await db.Set<ActivityEvent>()
            .AnyAsync(e => e.EventType == ActivityEventTypes.RequirementCreated && e.TargetId == requirement.Id));
        Assert.True(logged);
    }

    [Fact]
    public async Task Create_WithoutBudgetOrLocation_UsesProfileLocation()
    {
        var buyer = await RegisterAsync("Buyer");

        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken, Requirement(budgetMin: null, budgetMax: null, state: null, city: null));

        Assert.Null(requirement.BudgetMin);
        Assert.Equal("Mumbai", requirement.CityName);
    }

    [Fact]
    public async Task SellerOnly_CannotUseRequirements()
    {
        var seller = await RegisterAsync("Seller");

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, Requirements, seller.AccessToken, Requirement())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"{Requirements}/mine", seller.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task OtherBuyer_Gets404_EverywhereOnSomeoneElsesRequirement()
    {
        var owner = await RegisterAsync("Buyer");
        var other = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, owner.AccessToken);
        var url = $"{Requirements}/{requirement.Id}";

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, url, other.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Put, url, other.AccessToken, Requirement(title: "Hijacked"))).StatusCode);
        foreach (var action in new[] { "pause", "activate", "renew", "fulfill" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, $"{url}/{action}", other.AccessToken)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, url, other.AccessToken)).StatusCode);
    }

    public static TheoryData<object, string> InvalidRequirements => new()
    {
        { Requirement(category: "category:textiles"), "categoryId" },
        { Requirement(budgetMin: 200, budgetMax: 100), "budgetMax" },
        { Requirement(quantity: 0), "quantity" },
        { Requirement(quantity: null), "quantity" },
        { Requirement(title: ""), "title" },
        { Requirement(keywords: ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"]), "keywords" },
        { Requirement(neededBy: "2020-01-01"), "neededBy" },
    };

    [Theory]
    [MemberData(nameof(InvalidRequirements))]
    public async Task Create_Invalid_ReturnsFieldError(object body, string field)
    {
        var buyer = await RegisterAsync("Buyer");

        await AssertFieldErrorAsync(await SendAsync(HttpMethod.Post, Requirements, buyer.AccessToken, body), field);
    }

    [Fact]
    public async Task Update_KeepsAPastNeededByDate_WhenUnchanged()
    {
        var buyer = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken);
        await factory.WithDbAsync(db => db.Set<Requirement>().Where(r => r.Id == requirement.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.NeededBy, new DateOnly(2020, 1, 1))));

        var response = await SendAsync(HttpMethod.Put, $"{Requirements}/{requirement.Id}", buyer.AccessToken, Requirement(title: "Edited", neededBy: "2020-01-01"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PauseActivate_ChangeStatus()
    {
        var buyer = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken);
        var url = $"{Requirements}/{requirement.Id}";

        Assert.Equal("Paused", (await ReadAsync(HttpMethod.Post, $"{url}/pause", buyer.AccessToken)).Status);
        Assert.Equal("Active", (await ReadAsync(HttpMethod.Post, $"{url}/activate", buyer.AccessToken)).Status);
    }

    [Fact]
    public async Task Fulfill_IsFinal_ForActivateRenewAndPause()
    {
        var buyer = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken);
        var url = $"{Requirements}/{requirement.Id}";

        Assert.Equal("Fulfilled", (await ReadAsync(HttpMethod.Post, $"{url}/fulfill", buyer.AccessToken)).Status);

        foreach (var action in new[] { "activate", "renew", "pause" })
        {
            await AssertFieldErrorAsync(await SendAsync(HttpMethod.Post, $"{url}/{action}", buyer.AccessToken), "status");
        }
    }

    [Fact]
    public async Task Delete_IsSoft()
    {
        var buyer = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{Requirements}/{requirement.Id}", buyer.AccessToken)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"{Requirements}/{requirement.Id}", buyer.AccessToken)).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            var row = await db.Set<Requirement>().IgnoreQueryFilters().SingleAsync(r => r.Id == requirement.Id);
            Assert.True(row.IsDeleted);
            Assert.Equal(RequirementStatus.Removed, row.Status);
        });
    }

    // Expiry

    [Fact]
    public async Task ExpiryJob_MarksOverdueAsExpired_AndLeavesOthersAlone()
    {
        var buyer = await RegisterAsync("Buyer");
        var overdue = await CreateRequirementAsync(_client, buyer.AccessToken);
        var overduePaused = await CreateRequirementAsync(_client, buyer.AccessToken);
        var overdueFulfilled = await CreateRequirementAsync(_client, buyer.AccessToken);
        var current = await CreateRequirementAsync(_client, buyer.AccessToken);
        await SendAsync(HttpMethod.Post, $"{Requirements}/{overduePaused.Id}/pause", buyer.AccessToken);
        await SendAsync(HttpMethod.Post, $"{Requirements}/{overdueFulfilled.Id}/fulfill", buyer.AccessToken);
        await AgeAsync(overdue.Id);
        await AgeAsync(overduePaused.Id);
        await AgeAsync(overdueFulfilled.Id);

        await ExpireDueAsync();

        Assert.Equal("Expired", (await ReadAsync(HttpMethod.Get, $"{Requirements}/{overdue.Id}", buyer.AccessToken)).Status);
        Assert.Equal("Expired", (await ReadAsync(HttpMethod.Get, $"{Requirements}/{overduePaused.Id}", buyer.AccessToken)).Status);
        Assert.Equal("Fulfilled", (await ReadAsync(HttpMethod.Get, $"{Requirements}/{overdueFulfilled.Id}", buyer.AccessToken)).Status);
        Assert.Equal("Active", (await ReadAsync(HttpMethod.Get, $"{Requirements}/{current.Id}", buyer.AccessToken)).Status);

        var expired = await (await SendAsync(HttpMethod.Get, $"{Requirements}/mine?status=Expired", buyer.AccessToken)).Content.ReadFromJsonAsync<PagedResult<RequirementDto>>();
        Assert.Equal(2, expired!.TotalCount);
    }

    [Theory]
    [InlineData("renew")]
    [InlineData("activate")]
    public async Task RenewOrActivate_BringsAnExpiredRequirementBack_ForSixtyDays(string action)
    {
        var buyer = await RegisterAsync("Buyer");
        var requirement = await CreateRequirementAsync(_client, buyer.AccessToken);
        await AgeAsync(requirement.Id);
        await ExpireDueAsync();

        var renewed = await ReadAsync(HttpMethod.Post, $"{Requirements}/{requirement.Id}/{action}", buyer.AccessToken);

        Assert.Equal("Active", renewed.Status);
        Assert.InRange(renewed.ExpiresAt, DateTime.UtcNow.AddDays(59), DateTime.UtcNow.AddDays(61));
    }
}
