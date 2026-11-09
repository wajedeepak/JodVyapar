using JodVyapar.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Features.Reference;

/// <summary>Read-only lookup data used by forms (anonymous).</summary>
[ApiController]
[AllowAnonymous]
[Route(ApiRoutes.Prefix + "/reference")]
public sealed class ReferenceController(ReferenceService referenceService) : ControllerBase
{
    /// <summary>All Indian states and union territories, alphabetically.</summary>
    [HttpGet("states")]
    public async Task<ActionResult<IReadOnlyList<StateDto>>> GetStates(CancellationToken cancellationToken) =>
        Ok(await referenceService.GetStatesAsync(cancellationToken));

    /// <summary>Major cities of one state.</summary>
    [HttpGet("cities")]
    [ProducesResponseType<IReadOnlyList<CityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> GetCities([FromQuery] CityQuery query, CancellationToken cancellationToken) =>
        Ok(await referenceService.GetCitiesAsync(query.StateId!.Value, cancellationToken));

    /// <summary>Active category tree: top-level categories with their subcategories.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await referenceService.GetCategoryTreeAsync(cancellationToken));
}
