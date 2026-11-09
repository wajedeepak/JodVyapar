using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JodVyapar.Api.Common.Validation;

public static class ModelStateExtensions
{
    /// <summary>
    /// Adds an error under exactly this field name. Keys are case-insensitive and model binding has already
    /// created PascalCase entries for form and query values ("File"), which would otherwise decide the name
    /// the client sees. On the first error those error-free entries are dropped, so every key stays camelCase.
    /// Clear is used because Remove keeps the original casing. Binding failures never reach this code:
    /// [ApiController] has already returned a 400 for them.
    /// </summary>
    public static void AddFieldError(this ModelStateDictionary modelState, string field, string message)
    {
        if (modelState.ErrorCount == 0)
        {
            modelState.Clear();
        }

        modelState.AddModelError(field, message);
    }
}
