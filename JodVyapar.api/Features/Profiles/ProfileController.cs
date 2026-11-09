using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Catalog;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Features.Profiles;

/// <summary>The signed-in user's profile and roles, and the public profiles of other users. All endpoints need a token.</summary>
[ApiController]
[Authorize]
[Route(ApiRoutes.Prefix + "/profile")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class ProfileController(
    ProfileService profileService,
    ProductService productService,
    RequirementService requirementService) : ControllerBase
{
    /// <summary>The signed-in user's full profile: roles, email, mobile, GSTIN, about and verification flags.</summary>
    [HttpGet("me")]
    [ProducesResponseType<MyProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var profile = await profileService.GetMyProfileAsync(userId, cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    /// <summary>Updates name and company details. Email and roles cannot be changed here.</summary>
    [HttpPut("me")]
    [ProducesResponseType<MyProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var profile = await profileService.UpdateMyProfileAsync(userId, request, cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    /// <summary>
    /// Adds the Buyer or Seller role and returns a new token carrying it (use it from now on).
    /// Adding a role the user already holds is harmless. Admin cannot be added; roles cannot be removed.
    /// </summary>
    [HttpPost("me/roles")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddRole(AddRoleRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await profileService.AddRoleAsync(userId, request, cancellationToken);
        switch (result.Status)
        {
            case AddRoleStatus.UserNotFound:
                return Unauthorized();

            case AddRoleStatus.CompanyProfileRequired:
                ModelState.AddModelError("role", "Please add your company details to your profile first.");
                var problem = ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState);
                problem.Extensions["code"] = ProfileErrorCodes.CompanyProfileRequired;
                return BadRequest(problem);

            default:
                return Ok(result.Response);
        }
    }

    /// <summary>
    /// Another user's public profile. Never includes email, mobile or the GSTIN number. 404 for unknown,
    /// disabled or unverified users.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<PublicProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublic(Guid userId, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } viewerId)
        {
            return Unauthorized();
        }

        var profile = await profileService.GetPublicProfileAsync(viewerId, userId, cancellationToken);
        return profile is null ? ProfileNotFound() : Ok(profile);
    }

    /// <summary>A user's Active products, newest first. No contact details. 404 when the profile isn't public.</summary>
    [HttpGet("{userId:guid}/products")]
    [ProducesResponseType<PagedResult<PublicProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProducts(Guid userId, [FromQuery] PublicListQuery query, CancellationToken cancellationToken) =>
        await profileService.IsPublicAsync(userId, cancellationToken)
            ? Ok(await productService.GetPublicAsync(userId, query, cancellationToken))
            : ProfileNotFound();

    /// <summary>A user's Active, unexpired requirements, newest first. No contact details. 404 when the profile isn't public.</summary>
    [HttpGet("{userId:guid}/requirements")]
    [ProducesResponseType<PagedResult<PublicRequirementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicRequirements(Guid userId, [FromQuery] PublicListQuery query, CancellationToken cancellationToken) =>
        await profileService.IsPublicAsync(userId, cancellationToken)
            ? Ok(await requirementService.GetPublicAsync(userId, query, cancellationToken))
            : ProfileNotFound();

    private ObjectResult ProfileNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Profile not found.", detail: "This profile doesn't exist or is no longer available.");
}
