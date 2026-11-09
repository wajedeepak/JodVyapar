using FluentValidation;
using JodVyapar.Api.Common.Text;

namespace JodVyapar.Api.Common.Validation;

/// <summary>Validation rules shared by Products and Requirements.</summary>
public static class ListingRules
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 2000;

    public static IRuleBuilderOptions<T, string> ListingTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Please enter a title.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must be {TitleMaxLength} characters or fewer.");

    public static IRuleBuilderOptions<T, string?> ListingDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(DescriptionMaxLength).WithMessage($"Description must be {DescriptionMaxLength} characters or fewer.");

    /// <summary>Must be above zero and fit decimal(12,2). A null value passes (add NotNull where it is required).</summary>
    public static IRuleBuilderOptions<T, decimal?> PositiveAmount<T>(this IRuleBuilder<T, decimal?> rule, string label) =>
        rule.GreaterThan(0).WithMessage($"{label} must be more than 0.")
            .PrecisionScale(12, 2, ignoreTrailingZeros: true).WithMessage($"{label} can have up to 10 digits and 2 decimal places.");

    public static IRuleBuilderOptions<T, IReadOnlyList<string>> ListingKeywords<T>(this IRuleBuilder<T, IReadOnlyList<string>> rule) =>
        rule.Must(k => k.Count <= KeywordNormalizer.MaxKeywords).WithMessage($"Please use at most {KeywordNormalizer.MaxKeywords} keywords.")
            .Must(k => k.All(w => w.Length <= KeywordNormalizer.MaxKeywordLength)).WithMessage($"Each keyword must be {KeywordNormalizer.MaxKeywordLength} characters or fewer.");
}
