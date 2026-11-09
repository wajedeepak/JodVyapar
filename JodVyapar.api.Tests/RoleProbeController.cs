using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Tests;

/// <summary>
/// Test-only endpoints (added to the test host by <see cref="ApiFactory"/>, never shipped) that use
/// the real role policies, so tests can prove the API enforces held roles. The real Buyer/Seller-only
/// endpoints arrive with Catalog and Requirements.
/// </summary>
[ApiController]
[Route("api/v1/test-probe")]
public sealed class RoleProbeController : ControllerBase
{
    [HttpGet("buyer")]
    [Authorize(Policy = AuthPolicies.RequireBuyer)]
    public IActionResult Buyer() => Ok();

    [HttpGet("seller")]
    [Authorize(Policy = AuthPolicies.RequireSeller)]
    public IActionResult Seller() => Ok();

    [HttpGet("admin")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    public IActionResult Admin() => Ok();
}
