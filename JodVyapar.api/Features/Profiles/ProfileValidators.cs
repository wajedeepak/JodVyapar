using FluentValidation;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Profiles;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    /// <summary>
    /// GSTIN format only (not a checksum or government lookup): 2-digit state code, 10-character PAN,
    /// entity number (1-9 or A-Z), the letter Z, and a check character.
    /// </summary>
    public const string GstinPattern = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

    public const int AboutMaxLength = 500;

    public UpdateProfileRequestValidator(AppDbContext db)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("Please enter your full name.")
            .MaximumLength(100).WithMessage("Full name must be 100 characters or fewer.");

        RuleFor(r => r.CompanyName)
            .NotEmpty().WithMessage("Please enter your company name.")
            .MaximumLength(150).WithMessage("Company name must be 150 characters or fewer.");

        RuleFor(r => r.Mobile).IndianMobile();

        RuleFor(r => r.StateId)
            .NotEmpty().WithMessage("Please choose a state.")
            .MustAsync((stateId, ct) => db.Set<State>().AnyAsync(s => s.Id == stateId, ct))
            .WithMessage("Please choose a valid state.");

        RuleFor(r => r.CityId)
            .NotEmpty().WithMessage("Please choose a city.")
            .MustAsync((request, cityId, ct) =>
                db.Set<City>().AnyAsync(c => c.Id == cityId && c.StateId == request.StateId, ct))
            .WithMessage("Please choose a city in the selected state.");

        RuleFor(r => r.Gstin)
            .Matches(GstinPattern).WithMessage("Please enter a valid 15-character GSTIN, for example 27ABCDE1234F1Z5.")
            .When(r => r.Gstin is not null);

        RuleFor(r => r.About)
            .MaximumLength(AboutMaxLength).WithMessage($"About must be {AboutMaxLength} characters or fewer.");
    }
}

public sealed class AddRoleRequestValidator : AbstractValidator<AddRoleRequest>
{
    public AddRoleRequestValidator()
    {
        RuleFor(r => r.Role)
            .Must(role => AppRoles.FindSelfAssignable(role) is not null)
            .WithMessage("Please choose Buyer or Seller.");
    }
}
