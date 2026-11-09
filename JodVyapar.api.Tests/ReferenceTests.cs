using System.Net;
using System.Net.Http.Json;
using JodVyapar.Api.Features.Reference;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ReferenceTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task States_ReturnsAllStatesAndUnionTerritories()
    {
        var states = await _client.GetFromJsonAsync<List<StateDto>>("/api/v1/reference/states");

        Assert.Equal(36, states!.Count);
        Assert.Equal(28, states.Count(s => !s.IsUnionTerritory));
        Assert.Contains(states, s => s.Code == "MH" && s.Name == "Maharashtra");
    }

    [Fact]
    public async Task Cities_ReturnsCitiesOfTheGivenState()
    {
        var states = await _client.GetFromJsonAsync<List<StateDto>>("/api/v1/reference/states");
        var maharashtra = states!.Single(s => s.Code == "MH");

        var cities = await _client.GetFromJsonAsync<List<CityDto>>($"/api/v1/reference/cities?stateId={maharashtra.Id}");

        Assert.Contains(cities!, c => c.Name == "Mumbai");
        Assert.All(cities!, c => Assert.Equal(maharashtra.Id, c.StateId));
    }

    [Fact]
    public async Task Cities_WithoutStateId_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/v1/reference/cities");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("stateId"));
    }

    [Fact]
    public async Task Categories_ReturnsTwoLevelTree()
    {
        var tree = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/v1/reference/categories");

        Assert.Equal(8, tree!.Count);
        Assert.All(tree, top =>
        {
            Assert.InRange(top.Children.Count, 3, 5);
            Assert.All(top.Children, child => Assert.Empty(child.Children));
        });
    }
}
