using FluentValidation;

namespace JodVyapar.Api.Common.Paging;

/// <summary>One page of a list, with the total so the client can draw a pager.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Query-string paging (?page=1&amp;pageSize=12). Validated by <see cref="PageQueryRules"/>.</summary>
public class PageQuery
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}

public static class PageQueryRules
{
    public static void AddPagingRules<T>(this AbstractValidator<T> validator)
        where T : PageQuery
    {
        validator.RuleFor(q => q.Page)
            .InclusiveBetween(1, 1_000_000).WithMessage("Page must be 1 or more.");
        validator.RuleFor(q => q.PageSize)
            .InclusiveBetween(1, PageQuery.MaxPageSize).WithMessage($"Page size must be between 1 and {PageQuery.MaxPageSize}.");
    }
}

/// <summary>Paging only (public profile lists).</summary>
public sealed class PublicListQuery : PageQuery;

public sealed class PublicListQueryValidator : AbstractValidator<PublicListQuery>
{
    public PublicListQueryValidator() => this.AddPagingRules();
}
