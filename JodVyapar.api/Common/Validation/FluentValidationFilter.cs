using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace JodVyapar.Api.Common.Validation;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument and returns a
/// ValidationProblemDetails (400) keyed by camelCase field name, so the client can map errors to form fields.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var error in result.Errors)
            {
                context.ModelState.AddFieldError(ToCamelCasePath(error.PropertyName), error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var apiBehavior = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            context.Result = apiBehavior.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }

    private static string ToCamelCasePath(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
