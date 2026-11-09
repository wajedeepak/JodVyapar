using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Reference;

public sealed class ReferenceService(AppDbContext db)
{
    public async Task<IReadOnlyList<StateDto>> GetStatesAsync(CancellationToken cancellationToken) =>
        await db.Set<State>()
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new StateDto(s.Id, s.Name, s.Code, s.IsUnionTerritory))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CityDto>> GetCitiesAsync(Guid stateId, CancellationToken cancellationToken) =>
        await db.Set<City>()
            .AsNoTracking()
            .Where(c => c.StateId == stateId)
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.StateId, c.Name))
            .ToListAsync(cancellationToken);

    /// <summary>Active top-level categories with their active subcategories.</summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoryTreeAsync(CancellationToken cancellationToken)
    {
        var categories = await db.Set<Category>()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.ParentId, c.Name, c.Slug })
            .ToListAsync(cancellationToken);

        var childrenByParent = categories
            .Where(c => c.ParentId is not null)
            .ToLookup(c => c.ParentId!.Value);

        return categories
            .Where(c => c.ParentId is null)
            .Select(parent => new CategoryDto(
                parent.Id,
                parent.Name,
                parent.Slug,
                childrenByParent[parent.Id].Select(child => new CategoryDto(child.Id, child.Name, child.Slug, [])).ToList()))
            .ToList();
    }
}
