using FluentValidation;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JodVyapar.Api.Features.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        // Report only the first problem per field.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.FullName)
            .NotEmpty().WithMessage("Please enter your full name.")
            .MaximumLength(100).WithMessage("Full name must be 100 characters or fewer.");

        RuleFor(r => r.CompanyName)
            .NotEmpty().WithMessage("Please enter your company name.")
            .MaximumLength(150).WithMessage("Company name must be 150 characters or fewer.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Please enter your email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MustAsync(async (email, _) => await userManager.FindByEmailAsync(email.Trim()) is null)
            .WithMessage("An account with this email already exists.");

        RuleFor(r => r.Mobile).IndianMobile();

        RuleFor(r => r.Password).StrongPassword();

        RuleFor(r => r.StateId)
            .NotEmpty().WithMessage("Please choose a state.")
            .MustAsync((stateId, ct) => db.Set<State>().AnyAsync(s => s.Id == stateId, ct))
            .WithMessage("Please choose a valid state.");

        RuleFor(r => r.CityId)
            .NotEmpty().WithMessage("Please choose a city.")
            .MustAsync((request, cityId, ct) =>
                db.Set<City>().AnyAsync(c => c.Id == cityId && c.StateId == request.StateId, ct))
            .WithMessage("Please choose a city in the selected state.");

        RuleFor(r => r.Roles)
            .NotEmpty().WithMessage("Please choose whether you want to buy, sell or both.")
            .Must(roles => roles!.All(role => AppRoles.FindSelfAssignable(role) is not null))
            .WithMessage("Please choose Buyer, Seller or both.");

        RuleFor(r => r.ConsentAccepted)
            .Equal(true).WithMessage("Please accept the terms and privacy policy to continue.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Please enter your email address.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Please enter your password.");
    }
}

/// <summary>Mirrors the Identity password policy configured in IdentityServiceCollectionExtensions.</summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Please enter a password.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must be 100 characters or fewer.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
}

public static class MobileRules
{
    /// <summary>A 10-digit Indian mobile number starting 6-9, without +91 or spaces.</summary>
    public static IRuleBuilderOptions<T, string> IndianMobile<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Please enter your mobile number.")
            .Matches(@"^[6-9]\d{9}$").WithMessage("Please enter a valid 10-digit Indian mobile number.");
}

public sealed class EmailRequestValidator : AbstractValidator<EmailRequest>
{
    public EmailRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.Email).ValidLoginEmail();
    }
}

public sealed class VerifyCodeRequestValidator : AbstractValidator<VerifyCodeRequest>
{
    public VerifyCodeRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.Email).ValidLoginEmail();

        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("Please enter the 6-digit code.")
            .Matches("^[0-9]{6}$").WithMessage("The code is 6 digits.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(r => r.Email).ValidLoginEmail();

        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("Please enter the 6-digit code.")
            .Matches("^[0-9]{6}$").WithMessage("The code is 6 digits.");

        RuleFor(r => r.NewPassword).StrongPassword();
    }
}

public static class EmailRules
{
    public static IRuleBuilderOptions<T, string> ValidLoginEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Please enter your email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.")
            .EmailAddress().WithMessage("Please enter a valid email address.");
}
