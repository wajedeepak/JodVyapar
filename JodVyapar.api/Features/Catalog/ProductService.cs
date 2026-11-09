using System.Linq.Expressions;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Common.Results;
using JodVyapar.Api.Common.Storage;
using JodVyapar.Api.Common.Text;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Catalog;

/// <summary>A seller's own products, and the public list shown on their profile. Other users' products are "not found".</summary>
public sealed class ProductService(
    AppDbContext db,
    ListingReferenceChecker referenceChecker,
    IFileStorage fileStorage,
    IActivityLogger activityLogger,
    ILogger<ProductService> logger)
{
    public const int MaxImages = 3;

    private static readonly Expression<Func<Product, ProductDto>> ToDto = p => new ProductDto(
        p.Id,
        p.Title,
        p.Description,
        p.Category.ParentId!.Value,
        p.Category.Parent!.Name,
        p.CategoryId,
        p.Category.Name,
        p.PriceMin,
        p.PriceMax,
        p.UnitId,
        p.Unit.Name,
        p.Unit.Symbol,
        p.MinOrderQty,
        p.StateId,
        p.State.Name,
        p.CityId,
        p.City.Name,
        KeywordNormalizer.FromStored(p.Keywords),
        p.Status.ToString(),
        p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, MediaUrls.ProductImage(i.Id), i.SortOrder)).ToList(),
        p.CreatedAt,
        p.UpdatedAt);

    private static readonly Expression<Func<Product, PublicProductDto>> ToPublicDto = p => new PublicProductDto(
        p.Id,
        p.Title,
        p.Description,
        p.Category.Parent!.Name,
        p.Category.Name,
        p.PriceMin,
        p.PriceMax,
        p.Unit.Symbol,
        p.MinOrderQty,
        p.State.Name,
        p.City.Name,
        KeywordNormalizer.FromStored(p.Keywords),
        p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, MediaUrls.ProductImage(i.Id), i.SortOrder)).ToList(),
        p.CreatedAt);

    public async Task<ServiceResult<ProductDto>> CreateAsync(Guid sellerId, ProductRequest request, CancellationToken cancellationToken)
    {
        var location = request.StateId is null
            ? await referenceChecker.GetProfileLocationAsync(sellerId, cancellationToken)
            : (request.StateId.Value, request.CityId!.Value);
        if (location is not var (stateId, cityId))
        {
            return ServiceResult<ProductDto>.Invalid("stateId", "Please choose a state.");
        }

        var errors = await referenceChecker.CheckAsync(request.CategoryId!.Value, request.UnitId!.Value, stateId, cityId, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<ProductDto>.Invalid(errors);
        }

        var product = new Product { SellerUserId = sellerId, Title = request.Title, Status = ProductStatus.Active };
        Apply(product, request, stateId, cityId);
        db.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        await activityLogger.LogAsync(ActivityEventTypes.ProductCreated, sellerId, ActivityTargetTypes.Product, product.Id, cancellationToken);
        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(Guid sellerId, Guid productId, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.NotFound();
        }

        var stateId = request.StateId ?? product.StateId;
        var cityId = request.CityId ?? product.CityId;
        var errors = await referenceChecker.CheckAsync(request.CategoryId!.Value, request.UnitId!.Value, stateId, cityId, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<ProductDto>.Invalid(errors);
        }

        Apply(product, request, stateId, cityId);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    public async Task<PagedResult<ProductDto>> GetMineAsync(Guid sellerId, MyProductsQuery query, CancellationToken cancellationToken)
    {
        var products = Owned(sellerId).AsNoTracking();
        if (query.ParsedStatus is { } status)
        {
            products = products.Where(p => p.Status == status);
        }

        var total = await products.CountAsync(cancellationToken);
        var items = await products
            .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<ServiceResult<ProductDto>> GetAsync(Guid sellerId, Guid productId, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).AsNoTracking().Where(p => p.Id == productId).Select(ToDto).FirstOrDefaultAsync(cancellationToken);
        return product is null ? ServiceResult<ProductDto>.NotFound() : ServiceResult<ProductDto>.Ok(product);
    }

    public Task<ServiceResult<ProductDto>> PauseAsync(Guid sellerId, Guid productId, CancellationToken cancellationToken) =>
        SetStatusAsync(sellerId, productId, ProductStatus.Paused, cancellationToken);

    public Task<ServiceResult<ProductDto>> ActivateAsync(Guid sellerId, Guid productId, CancellationToken cancellationToken) =>
        SetStatusAsync(sellerId, productId, ProductStatus.Active, cancellationToken);

    /// <summary>Soft delete. The images stay on disk but are no longer served.</summary>
    public async Task<ServiceResult<NoValue>> DeleteAsync(Guid sellerId, Guid productId, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<NoValue>.NotFound();
        }

        product.Status = ProductStatus.Removed;
        db.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<NoValue>.Ok(default);
    }

    public async Task<ServiceResult<ProductDto>> AddImageAsync(Guid sellerId, Guid productId, IFormFile file, CancellationToken cancellationToken)
    {
        var (format, error) = await ImageFileInspector.InspectAsync(file, cancellationToken);
        if (format is null)
        {
            return ServiceResult<ProductDto>.Invalid("file", error!);
        }

        // The row lock makes concurrent uploads queue up, so they can't get past the limit together.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var product = await db.Set<Product>()
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {productId}")
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.SellerUserId == sellerId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.NotFound();
        }

        if (product.Images.Count >= MaxImages)
        {
            return ServiceResult<ProductDto>.Invalid("file", $"A product can have up to {MaxImages} images.");
        }

        var storagePath = $"products/{product.Id:N}/{Guid.NewGuid():N}.{format.Extension}";
        await using (var content = file.OpenReadStream())
        {
            await fileStorage.SaveAsync(storagePath, content, cancellationToken);
        }

        try
        {
            db.Add(new ProductImage
            {
                ProductId = product.Id,
                StoragePath = storagePath,
                ContentType = format.ContentType,
                SortOrder = product.Images.Count,
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteFileAsync(storagePath);
            throw;
        }

        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    public async Task<ServiceResult<ProductDto>> RemoveImageAsync(Guid sellerId, Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        var image = product?.Images.FirstOrDefault(i => i.Id == imageId);
        if (product is null || image is null)
        {
            return ServiceResult<ProductDto>.NotFound();
        }

        db.Remove(image);
        Renumber(product.Images.Where(i => i != image).OrderBy(i => i.SortOrder));
        await db.SaveChangesAsync(cancellationToken);

        await TryDeleteFileAsync(image.StoragePath);
        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    public async Task<ServiceResult<ProductDto>> ReorderImagesAsync(Guid sellerId, Guid productId, ReorderProductImagesRequest request, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.NotFound();
        }

        if (!request.ImageIds.Order().SequenceEqual(product.Images.Select(i => i.Id).Order()))
        {
            return ServiceResult<ProductDto>.Invalid("imageIds", "Please send every image of this product exactly once.");
        }

        Renumber(request.ImageIds.Select(id => product.Images.First(i => i.Id == id)));
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    /// <summary>Active products only, newest first. The caller has checked that the profile is public.</summary>
    public async Task<PagedResult<PublicProductDto>> GetPublicAsync(Guid sellerId, PageQuery query, CancellationToken cancellationToken)
    {
        var products = db.Set<Product>().AsNoTracking().Where(p => p.SellerUserId == sellerId && p.Status == ProductStatus.Active);

        var total = await products.CountAsync(cancellationToken);
        var items = await products
            .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(ToPublicDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicProductDto>(items, query.Page, query.PageSize, total);
    }

    private IQueryable<Product> Owned(Guid sellerId) => db.Set<Product>().Where(p => p.SellerUserId == sellerId);

    private Task<ProductDto> GetDtoAsync(Guid sellerId, Guid productId, CancellationToken cancellationToken) =>
        Owned(sellerId).AsNoTracking().Where(p => p.Id == productId).Select(ToDto).FirstAsync(cancellationToken);

    private async Task<ServiceResult<ProductDto>> SetStatusAsync(Guid sellerId, Guid productId, ProductStatus status, CancellationToken cancellationToken)
    {
        var product = await Owned(sellerId).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.NotFound();
        }

        product.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProductDto>.Ok(await GetDtoAsync(sellerId, product.Id, cancellationToken));
    }

    private static void Apply(Product product, ProductRequest request, Guid stateId, Guid cityId)
    {
        product.Title = request.Title;
        product.Description = request.Description;
        product.CategoryId = request.CategoryId!.Value;
        product.PriceMin = request.PriceMin!.Value;
        product.PriceMax = request.PriceMax;
        product.UnitId = request.UnitId!.Value;
        product.MinOrderQty = request.MinOrderQty!.Value;
        product.StateId = stateId;
        product.CityId = cityId;
        product.Keywords = KeywordNormalizer.ToStored(request.Keywords);
    }

    private static void Renumber(IEnumerable<ProductImage> imagesInOrder)
    {
        var position = 0;
        foreach (var image in imagesInOrder)
        {
            image.SortOrder = position++;
        }
    }

    /// <summary>A leftover file is harmless, so a failed delete is logged and never fails the request.</summary>
    private async Task TryDeleteFileAsync(string storagePath)
    {
        try
        {
            await fileStorage.DeleteAsync(storagePath, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete stored file {StoragePath}", storagePath);
        }
    }
}
