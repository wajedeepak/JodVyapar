using System.Linq.Expressions;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Common.Paging;
using JodVyapar.Api.Common.Results;
using JodVyapar.Api.Common.Text;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Requirements;

/// <summary>A buyer's own requirements, and the public list shown on their profile. Other users' requirements are "not found".</summary>
public sealed class RequirementService(
    AppDbContext db,
    ListingReferenceChecker referenceChecker,
    IActivityLogger activityLogger)
{
    public const int LifetimeDays = 60;

    /// <summary>India has one time zone; "today" for a needed-by date means today there.</summary>
    private static readonly TimeSpan IndiaOffset = TimeSpan.FromHours(5.5);

    private static readonly Expression<Func<Requirement, RequirementDto>> ToDto = r => new RequirementDto(
        r.Id,
        r.Title,
        r.Description,
        r.Category.ParentId!.Value,
        r.Category.Parent!.Name,
        r.CategoryId,
        r.Category.Name,
        r.BudgetMin,
        r.BudgetMax,
        r.Quantity,
        r.UnitId,
        r.Unit.Name,
        r.Unit.Symbol,
        r.StateId,
        r.State.Name,
        r.CityId,
        r.City.Name,
        KeywordNormalizer.FromStored(r.Keywords),
        r.NeededBy,
        r.Status.ToString(),
        r.ExpiresAt,
        r.CreatedAt,
        r.UpdatedAt);

    private static readonly Expression<Func<Requirement, PublicRequirementDto>> ToPublicDto = r => new PublicRequirementDto(
        r.Id,
        r.Title,
        r.Description,
        r.Category.Parent!.Name,
        r.Category.Name,
        r.BudgetMin,
        r.BudgetMax,
        r.Quantity,
        r.Unit.Symbol,
        r.State.Name,
        r.City.Name,
        KeywordNormalizer.FromStored(r.Keywords),
        r.NeededBy,
        r.ExpiresAt,
        r.CreatedAt);

    public async Task<ServiceResult<RequirementDto>> CreateAsync(Guid buyerId, RequirementRequest request, CancellationToken cancellationToken)
    {
        var location = request.StateId is null
            ? await referenceChecker.GetProfileLocationAsync(buyerId, cancellationToken)
            : (request.StateId.Value, request.CityId!.Value);
        if (location is not var (stateId, cityId))
        {
            return ServiceResult<RequirementDto>.Invalid("stateId", "Please choose a state.");
        }

        var errors = await CheckAsync(request, stateId, cityId, previousNeededBy: null, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<RequirementDto>.Invalid(errors);
        }

        var requirement = new Requirement
        {
            BuyerUserId = buyerId,
            Title = request.Title,
            Status = RequirementStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(LifetimeDays),
        };
        Apply(requirement, request, stateId, cityId);
        db.Add(requirement);
        await db.SaveChangesAsync(cancellationToken);

        await activityLogger.LogAsync(ActivityEventTypes.RequirementCreated, buyerId, ActivityTargetTypes.Requirement, requirement.Id, cancellationToken);
        return ServiceResult<RequirementDto>.Ok(await GetDtoAsync(buyerId, requirement.Id, cancellationToken));
    }

    public async Task<ServiceResult<RequirementDto>> UpdateAsync(Guid buyerId, Guid requirementId, RequirementRequest request, CancellationToken cancellationToken)
    {
        var requirement = await Owned(buyerId).FirstOrDefaultAsync(r => r.Id == requirementId, cancellationToken);
        if (requirement is null)
        {
            return ServiceResult<RequirementDto>.NotFound();
        }

        var stateId = request.StateId ?? requirement.StateId;
        var cityId = request.CityId ?? requirement.CityId;
        var errors = await CheckAsync(request, stateId, cityId, requirement.NeededBy, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<RequirementDto>.Invalid(errors);
        }

        Apply(requirement, request, stateId, cityId);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<RequirementDto>.Ok(await GetDtoAsync(buyerId, requirement.Id, cancellationToken));
    }

    public async Task<PagedResult<RequirementDto>> GetMineAsync(Guid buyerId, MyRequirementsQuery query, CancellationToken cancellationToken)
    {
        var requirements = Owned(buyerId).AsNoTracking();
        if (query.ParsedStatus is { } status)
        {
            requirements = requirements.Where(r => r.Status == status);
        }

        var total = await requirements.CountAsync(cancellationToken);
        var items = await requirements
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<RequirementDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<ServiceResult<RequirementDto>> GetAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken)
    {
        var requirement = await Owned(buyerId).AsNoTracking().Where(r => r.Id == requirementId).Select(ToDto).FirstOrDefaultAsync(cancellationToken);
        return requirement is null ? ServiceResult<RequirementDto>.NotFound() : ServiceResult<RequirementDto>.Ok(requirement);
    }

    public Task<ServiceResult<RequirementDto>> PauseAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken) =>
        ChangeAsync(buyerId, requirementId, requirement =>
        {
            if (requirement.Status is RequirementStatus.Fulfilled or RequirementStatus.Expired)
            {
                return "Only an active requirement can be paused.";
            }

            requirement.Status = RequirementStatus.Paused;
            return null;
        }, cancellationToken);

    /// <summary>Puts a paused or expired requirement live again. If its time has run out this counts as a renewal.</summary>
    public Task<ServiceResult<RequirementDto>> ActivateAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken) =>
        ChangeAsync(buyerId, requirementId, requirement =>
        {
            if (requirement.Status == RequirementStatus.Fulfilled)
            {
                return "A fulfilled requirement can't be reactivated. Please post a new one.";
            }

            if (requirement.ExpiresAt <= DateTime.UtcNow)
            {
                requirement.ExpiresAt = DateTime.UtcNow.AddDays(LifetimeDays);
            }

            requirement.Status = RequirementStatus.Active;
            return null;
        }, cancellationToken);

    /// <summary>Gives the requirement another 60 days from now and makes it live.</summary>
    public Task<ServiceResult<RequirementDto>> RenewAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken) =>
        ChangeAsync(buyerId, requirementId, requirement =>
        {
            if (requirement.Status == RequirementStatus.Fulfilled)
            {
                return "A fulfilled requirement can't be renewed. Please post a new one.";
            }

            requirement.ExpiresAt = DateTime.UtcNow.AddDays(LifetimeDays);
            requirement.Status = RequirementStatus.Active;
            return null;
        }, cancellationToken);

    public Task<ServiceResult<RequirementDto>> FulfillAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken) =>
        ChangeAsync(buyerId, requirementId, requirement =>
        {
            requirement.Status = RequirementStatus.Fulfilled;
            return null;
        }, cancellationToken);

    /// <summary>Soft delete.</summary>
    public async Task<ServiceResult<NoValue>> DeleteAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken)
    {
        var requirement = await Owned(buyerId).FirstOrDefaultAsync(r => r.Id == requirementId, cancellationToken);
        if (requirement is null)
        {
            return ServiceResult<NoValue>.NotFound();
        }

        requirement.Status = RequirementStatus.Removed;
        db.Remove(requirement);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<NoValue>.Ok(default);
    }

    /// <summary>Active and unexpired only, newest first (the check on ExpiresAt covers the gap before the hourly job runs). The caller has checked that the profile is public.</summary>
    public async Task<PagedResult<PublicRequirementDto>> GetPublicAsync(Guid buyerId, PageQuery query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var requirements = db.Set<Requirement>().AsNoTracking()
            .Where(r => r.BuyerUserId == buyerId && r.Status == RequirementStatus.Active && r.ExpiresAt > now);

        var total = await requirements.CountAsync(cancellationToken);
        var items = await requirements
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(ToPublicDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicRequirementDto>(items, query.Page, query.PageSize, total);
    }

    private IQueryable<Requirement> Owned(Guid buyerId) => db.Set<Requirement>().Where(r => r.BuyerUserId == buyerId);

    private Task<RequirementDto> GetDtoAsync(Guid buyerId, Guid requirementId, CancellationToken cancellationToken) =>
        Owned(buyerId).AsNoTracking().Where(r => r.Id == requirementId).Select(ToDto).FirstAsync(cancellationToken);

    /// <summary>Applies a status change. <paramref name="change"/> returns an error message when the change isn't allowed.</summary>
    private async Task<ServiceResult<RequirementDto>> ChangeAsync(Guid buyerId, Guid requirementId, Func<Requirement, string?> change, CancellationToken cancellationToken)
    {
        var requirement = await Owned(buyerId).FirstOrDefaultAsync(r => r.Id == requirementId, cancellationToken);
        if (requirement is null)
        {
            return ServiceResult<RequirementDto>.NotFound();
        }

        if (change(requirement) is { } error)
        {
            return ServiceResult<RequirementDto>.Invalid("status", error);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<RequirementDto>.Ok(await GetDtoAsync(buyerId, requirement.Id, cancellationToken));
    }

    private async Task<IReadOnlyDictionary<string, string>> CheckAsync(RequirementRequest request, Guid stateId, Guid cityId, DateOnly? previousNeededBy, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>(
            await referenceChecker.CheckAsync(request.CategoryId!.Value, request.UnitId!.Value, stateId, cityId, cancellationToken));

        // Only a newly chosen date is checked, so an older requirement can still be edited after its date passes.
        var today = DateOnly.FromDateTime(DateTime.UtcNow + IndiaOffset);
        if (request.NeededBy is { } neededBy && neededBy != previousNeededBy && neededBy < today)
        {
            errors["neededBy"] = "Needed-by date can't be in the past.";
        }

        return errors;
    }

    private static void Apply(Requirement requirement, RequirementRequest request, Guid stateId, Guid cityId)
    {
        requirement.Title = request.Title;
        requirement.Description = request.Description;
        requirement.CategoryId = request.CategoryId!.Value;
        requirement.BudgetMin = request.BudgetMin;
        requirement.BudgetMax = request.BudgetMax;
        requirement.Quantity = request.Quantity!.Value;
        requirement.UnitId = request.UnitId!.Value;
        requirement.StateId = stateId;
        requirement.CityId = cityId;
        requirement.NeededBy = request.NeededBy;
        requirement.Keywords = KeywordNormalizer.ToStored(request.Keywords);
    }
}
