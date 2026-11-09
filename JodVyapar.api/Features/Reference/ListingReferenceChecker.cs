using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Reference;

/// <summary>Checks that need the database and are shared by Products and Requirements.</summary>
public sealed class ListingReferenceChecker(AppDbContext db)
{
    /// <summary>Field errors (empty when all is well): active subcategory, city in state, active unit.</summary>
    public async Task<IReadOnlyDictionary<string, string>> CheckAsync(Guid categoryId, Guid unitId, Guid stateId, Guid cityId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();

        var isActiveSubcategory = await db.Set<Category>()
            .AnyAsync(c => c.Id == categoryId && c.IsActive && c.ParentId != null && c.Parent!.IsActive, cancellationToken);
        if (!isActiveSubcategory)
        {
            errors["categoryId"] = "Please choose a subcategory.";
        }

        if (!await db.Set<Unit>().AnyAsync(u => u.Id == unitId && u.IsActive, cancellationToken))
        {
            errors["unitId"] = "Please choose a unit.";
        }

        if (!await db.Set<City>().AnyAsync(c => c.Id == cityId && c.StateId == stateId, cancellationToken))
        {
            errors["cityId"] = "Please choose a city in the selected state.";
        }

        return errors;
    }

    /// <summary>The state and city on the user's company profile, or null if they have none.</summary>
    public async Task<(Guid StateId, Guid CityId)?> GetProfileLocationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var location = await db.Set<CompanyProfile>()
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.StateId, p.CityId })
            .FirstOrDefaultAsync(cancellationToken);

        return location is null ? null : (location.StateId, location.CityId);
    }
}
