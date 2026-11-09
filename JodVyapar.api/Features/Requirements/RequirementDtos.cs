using FluentValidation;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Common.Text;
using JodVyapar.Api.Common.Validation;

namespace JodVyapar.Api.Features.Requirements;

/// <summary>Body of POST /requirements and PUT /requirements/{id}. Text is trimmed and keywords normalized on the way in.</summary>
public sealed class RequirementRequest
{
    private readonly string _title = "";
    private readonly string? _description;
    private readonly IReadOnlyList<string> _keywords = [];

    public string Title { get => _title; init => _title = TextInput.Trim(value); }
    public string? Description { get => _description; init => _description = TextInput.NullIfBlank(value); }
    public Guid? CategoryId { get; init; }
    public decimal? BudgetMin { get; init; }
    public decimal? BudgetMax { get; init; }
    public decimal? Quantity { get; init; }
    public Guid? UnitId { get; init; }

    /// <summary>Send both or neither. Neither: a new requirement uses the buyer's profile location, an edit keeps its current one.</summary>
    public Guid? StateId { get; init; }
    public Guid? CityId { get; init; }

    public DateOnly? NeededBy { get; init; }
    public IReadOnlyList<string> Keywords { get => _keywords; init => _keywords = KeywordNormalizer.Normalize(value); }
}

public sealed class RequirementRequestValidator : AbstractValidator<RequirementRequest>
{
    public RequirementRequestValidator()
    {
        RuleFor(r => r.Title).ListingTitle();
        RuleFor(r => r.Description).ListingDescription();
        RuleFor(r => r.CategoryId).NotEmpty().WithMessage("Please choose a subcategory.");
        RuleFor(r => r.BudgetMin).PositiveAmount("Minimum budget");
        RuleFor(r => r.BudgetMax).PositiveAmount("Maximum budget");
        RuleFor(r => r.BudgetMax)
            .Must((r, max) => max >= r.BudgetMin).WithMessage("Maximum budget can't be less than the minimum budget.")
            .When(r => r.BudgetMin is not null && r.BudgetMax is not null);
        RuleFor(r => r.Quantity).NotNull().WithMessage("Please enter a quantity.").PositiveAmount("Quantity");
        RuleFor(r => r.UnitId).NotEmpty().WithMessage("Please choose a unit.");
        RuleFor(r => r.StateId).NotEmpty().WithMessage("Please choose a state.").When(r => r.CityId is not null);
        RuleFor(r => r.CityId).NotEmpty().WithMessage("Please choose a city.").When(r => r.StateId is not null);
        RuleFor(r => r.Keywords).ListingKeywords();
    }
}

/// <summary>Query string of GET /requirements/mine.</summary>
public sealed class MyRequirementsQuery : PageQuery
{
    /// <summary>Optional: Active, Paused, Fulfilled or Expired.</summary>
    public string? Status { get; init; }

    public RequirementStatus? ParsedStatus => Enum.TryParse<RequirementStatus>(Status, ignoreCase: true, out var status) ? status : null;
}

public sealed class MyRequirementsQueryValidator : AbstractValidator<MyRequirementsQuery>
{
    public MyRequirementsQueryValidator()
    {
        this.AddPagingRules();
        RuleFor(q => q.Status)
            .Must((q, _) => q.ParsedStatus is RequirementStatus.Active or RequirementStatus.Paused or RequirementStatus.Fulfilled or RequirementStatus.Expired)
            .WithMessage("Status must be Active, Paused, Fulfilled or Expired.")
            .When(q => !string.IsNullOrWhiteSpace(q.Status));
    }
}

/// <summary>The owner's view of a requirement.</summary>
public sealed record RequirementDto(
    Guid Id,
    string Title,
    string? Description,
    Guid ParentCategoryId,
    string ParentCategoryName,
    Guid CategoryId,
    string CategoryName,
    decimal? BudgetMin,
    decimal? BudgetMax,
    decimal Quantity,
    Guid UnitId,
    string UnitName,
    string UnitSymbol,
    Guid StateId,
    string StateName,
    Guid CityId,
    string CityName,
    IReadOnlyList<string> Keywords,
    DateOnly? NeededBy,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>What other signed-in users see on a public profile. Deliberately has no contact details.</summary>
public sealed record PublicRequirementDto(
    Guid Id,
    string Title,
    string? Description,
    string ParentCategoryName,
    string CategoryName,
    decimal? BudgetMin,
    decimal? BudgetMax,
    decimal Quantity,
    string UnitSymbol,
    string StateName,
    string CityName,
    IReadOnlyList<string> Keywords,
    DateOnly? NeededBy,
    DateTime ExpiresAt,
    DateTime CreatedAt);
