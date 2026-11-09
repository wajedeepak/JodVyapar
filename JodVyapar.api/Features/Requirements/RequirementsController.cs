using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Features.Requirements;

/// <summary>A buyer's own requirements. Needs the Buyer role; other users' requirements return 404.</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.RequireBuyer)]
[Route(ApiRoutes.Prefix + "/requirements")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RequirementsController(RequirementService requirements) : ListingControllerBase
{
    protected override (string Title, string Detail) NotFoundMessage =>
        ("Requirement not found.", "This requirement doesn't exist or is no longer available.");

    [HttpPost]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(RequirementRequest request, CancellationToken cancellationToken) =>
        ToResponse(
            await requirements.CreateAsync(CurrentUserId, request, cancellationToken),
            dto => CreatedAtAction(nameof(Get), new { id = dto.Id }, dto));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, RequirementRequest request, CancellationToken cancellationToken) =>
        ToResponse(await requirements.UpdateAsync(CurrentUserId, id, request, cancellationToken));

    [HttpGet("mine")]
    [ProducesResponseType<PagedResult<RequirementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMine([FromQuery] MyRequirementsQuery query, CancellationToken cancellationToken) =>
        Ok(await requirements.GetMineAsync(CurrentUserId, query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.GetAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.PauseAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.ActivateAsync(CurrentUserId, id, cancellationToken));

    /// <summary>Extends the requirement by 60 days from now and makes it live.</summary>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Renew(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.RenewAsync(CurrentUserId, id, cancellationToken));

    /// <summary>Marks the requirement as fulfilled, which takes it off the buyer's public profile.</summary>
    [HttpPost("{id:guid}/fulfill")]
    [ProducesResponseType<RequirementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fulfill(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.FulfillAsync(CurrentUserId, id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await requirements.DeleteAsync(CurrentUserId, id, cancellationToken), _ => NoContent());
}
