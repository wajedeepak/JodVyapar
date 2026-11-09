using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Features.Catalog;

/// <summary>A seller's own products. Needs the Seller role; other users' products return 404.</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.RequireSeller)]
[Route(ApiRoutes.Prefix + "/products")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ProductsController(ProductService products) : ListingControllerBase
{
    /// <summary>Above the 2 MB image limit, so a slightly large image gets the friendly field error.</summary>
    private const long MaxUploadRequestBytes = 5 * 1024 * 1024;

    protected override (string Title, string Detail) NotFoundMessage =>
        ("Product not found.", "This product doesn't exist or is no longer available.");

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken cancellationToken) =>
        ToResponse(
            await products.CreateAsync(CurrentUserId, request, cancellationToken),
            dto => CreatedAtAction(nameof(Get), new { id = dto.Id }, dto));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken cancellationToken) =>
        ToResponse(await products.UpdateAsync(CurrentUserId, id, request, cancellationToken));

    [HttpGet("mine")]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMine([FromQuery] MyProductsQuery query, CancellationToken cancellationToken) =>
        Ok(await products.GetMineAsync(CurrentUserId, query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await products.GetAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await products.PauseAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await products.ActivateAsync(CurrentUserId, id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await products.DeleteAsync(CurrentUserId, id, cancellationToken), _ => NoContent());

    /// <summary>Adds one image (form field "file"): jpg, png or webp, max 2 MB, max 3 per product.</summary>
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImage(Guid id, [FromForm] UploadProductImageRequest request, CancellationToken cancellationToken) =>
        ToResponse(await products.AddImageAsync(CurrentUserId, id, request.File!, cancellationToken));

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(Guid id, Guid imageId, CancellationToken cancellationToken) =>
        ToResponse(await products.RemoveImageAsync(CurrentUserId, id, imageId, cancellationToken));

    [HttpPut("{id:guid}/images/order")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderImages(Guid id, ReorderProductImagesRequest request, CancellationToken cancellationToken) =>
        ToResponse(await products.ReorderImagesAsync(CurrentUserId, id, request, cancellationToken));
}
