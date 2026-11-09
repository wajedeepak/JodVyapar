using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Storage;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Catalog;

/// <summary>
/// Serves stored product images. Anonymous because an img tag can't send the JWT. Looks images up by
/// their database id, so a client can never name a path, and images of deleted products are gone.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route(ApiRoutes.Prefix + "/media")]
public sealed class MediaController(AppDbContext db, IFileStorage fileStorage) : ControllerBase
{
    [HttpGet("product-images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductImage(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await db.Set<ProductImage>()
            .AsNoTracking()
            .Where(i => i.Id == imageId)
            .Select(i => new { i.StoragePath, i.ContentType })
            .FirstOrDefaultAsync(cancellationToken);

        var content = image is null ? null : await fileStorage.OpenReadAsync(image.StoragePath, cancellationToken);
        if (content is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(content, image!.ContentType);
    }
}
