using FluentValidation;

namespace JodVyapar.Api.Features.Reference;

public sealed record StateDto(Guid Id, string Name, string Code, bool IsUnionTerritory);

public sealed record CityDto(Guid Id, Guid StateId, string Name);

public sealed record CategoryDto(Guid Id, string Name, string Slug, IReadOnlyList<CategoryDto> Children);

public sealed record UnitDto(Guid Id, string Name, string Symbol);

public sealed class CityQuery
{
    public Guid? StateId { get; init; }
}

public sealed class CityQueryValidator : AbstractValidator<CityQuery>
{
    public CityQueryValidator()
    {
        RuleFor(q => q.StateId)
            .NotEmpty().WithMessage("Please choose a state.");
    }
}
