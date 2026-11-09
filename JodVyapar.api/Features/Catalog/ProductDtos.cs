using FluentValidation;
using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Common.Storage;
using JodVyapar.Api.Common.Text;
using JodVyapar.Api.Common.Validation;

namespace JodVyapar.Api.Features.Catalog;

/// <summary>Body of POST /products and PUT /products/{id}. Text is trimmed and keywords normalized on the way in.</summary>
public sealed class ProductRequest
{
    private readonly string _title = "";
    private readonly string? _description;
    private readonly IReadOnlyList<string> _keywords = [];

    public string Title { get => _title; init => _title = TextInput.Trim(value); }
    public string? Description { get => _description; init => _description = TextInput.NullIfBlank(value); }
    public Guid? CategoryId { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public Guid? UnitId { get; init; }
    public decimal? MinOrderQty { get; init; }

    /// <summary>Send both or neither. Neither: a new product uses the seller's profile location, an edit keeps its current one.</summary>
    public Guid? StateId { get; init; }
    public Guid? CityId { get; init; }

    public IReadOnlyList<string> Keywords { get => _keywords; init => _keywords = KeywordNormalizer.Normalize(value); }
}

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(r => r.Title).ListingTitle();
        RuleFor(r => r.Description).ListingDescription();
        RuleFor(r => r.CategoryId).NotEmpty().WithMessage("Please choose a subcategory.");
        RuleFor(r => r.PriceMin).NotNull().WithMessage("Please enter a price.").PositiveAmount("Price");
        RuleFor(r => r.PriceMax).PositiveAmount("Maximum price");
        RuleFor(r => r.PriceMax)
            .Must((r, max) => max >= r.PriceMin).WithMessage("Maximum price can't be less than the minimum price.")
            .When(r => r.PriceMin is not null && r.PriceMax is not null);
        RuleFor(r => r.UnitId).NotEmpty().WithMessage("Please choose a unit.");
        RuleFor(r => r.MinOrderQty).NotNull().WithMessage("Please enter a minimum order quantity.").PositiveAmount("Minimum order quantity");
        RuleFor(r => r.StateId).NotEmpty().WithMessage("Please choose a state.").When(r => r.CityId is not null);
        RuleFor(r => r.CityId).NotEmpty().WithMessage("Please choose a city.").When(r => r.StateId is not null);
        RuleFor(r => r.Keywords).ListingKeywords();
    }
}

/// <summary>Multipart body of POST /products/{id}/images (one file per call).</summary>
public sealed class UploadProductImageRequest
{
    public IFormFile? File { get; init; }
}

public sealed class UploadProductImageRequestValidator : AbstractValidator<UploadProductImageRequest>
{
    public UploadProductImageRequestValidator()
    {
        RuleFor(r => r.File)
            .NotNull().WithMessage("Please choose an image.")
            .CustomAsync(async (file, context, cancellationToken) =>
            {
                if (file is not null && (await ImageFileInspector.InspectAsync(file, cancellationToken)).Error is { } error)
                {
                    context.AddFailure(error);
                }
            });
    }
}

/// <summary>Body of PUT /products/{id}/images/order: every image id of the product, in the new order.</summary>
public sealed class ReorderProductImagesRequest
{
    public IReadOnlyList<Guid> ImageIds { get; init; } = [];
}

/// <summary>Query string of GET /products/mine.</summary>
public sealed class MyProductsQuery : PageQuery
{
    /// <summary>Optional: Active or Paused.</summary>
    public string? Status { get; init; }

    public ProductStatus? ParsedStatus => Enum.TryParse<ProductStatus>(Status, ignoreCase: true, out var status) ? status : null;
}

public sealed class MyProductsQueryValidator : AbstractValidator<MyProductsQuery>
{
    public MyProductsQueryValidator()
    {
        this.AddPagingRules();
        RuleFor(q => q.Status)
            .Must((q, _) => q.ParsedStatus is ProductStatus.Active or ProductStatus.Paused)
            .WithMessage("Status must be Active or Paused.")
            .When(q => !string.IsNullOrWhiteSpace(q.Status));
    }
}

public sealed record ProductImageDto(Guid Id, string Url, int SortOrder);

/// <summary>The owner's view of a product.</summary>
public sealed record ProductDto(
    Guid Id,
    string Title,
    string? Description,
    Guid ParentCategoryId,
    string ParentCategoryName,
    Guid CategoryId,
    string CategoryName,
    decimal PriceMin,
    decimal? PriceMax,
    Guid UnitId,
    string UnitName,
    string UnitSymbol,
    decimal MinOrderQty,
    Guid StateId,
    string StateName,
    Guid CityId,
    string CityName,
    IReadOnlyList<string> Keywords,
    string Status,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>What other signed-in users see on a public profile. Deliberately has no contact details.</summary>
public sealed record PublicProductDto(
    Guid Id,
    string Title,
    string? Description,
    string ParentCategoryName,
    string CategoryName,
    decimal PriceMin,
    decimal? PriceMax,
    string UnitSymbol,
    decimal MinOrderQty,
    string StateName,
    string CityName,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAt);

/// <summary>Builds the URLs clients use for stored files (see MediaController).</summary>
public static class MediaUrls
{
    public static string ProductImage(Guid imageId) => $"/{ApiRoutes.Prefix}/media/product-images/{imageId}";
}
