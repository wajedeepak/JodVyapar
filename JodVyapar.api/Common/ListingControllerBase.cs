using JodVyapar.Api.Common.Results;
using JodVyapar.Api.Common.Validation;
using JodVyapar.Api.Features.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JodVyapar.Api.Common;

/// <summary>Turns a <see cref="ServiceResult{T}"/> into the API's standard responses (ProblemDetails for 404, ValidationProblemDetails for 400).</summary>
public abstract class ListingControllerBase : ControllerBase
{
    /// <summary>Title and detail of the 404 response, e.g. "Product not found."</summary>
    protected abstract (string Title, string Detail) NotFoundMessage { get; }

    /// <summary>The signed-in user. These controllers all require a role policy, so a valid token is guaranteed.</summary>
    protected Guid CurrentUserId => User.GetUserId() ?? throw new InvalidOperationException("The access token has no user id.");

    protected IActionResult ToResponse<T>(ServiceResult<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsNotFound)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: NotFoundMessage.Title, detail: NotFoundMessage.Detail);
        }

        if (result.Errors is not null)
        {
            foreach (var (field, message) in result.Errors)
            {
                ModelState.AddFieldError(field, message);
            }

            return ValidationProblem(ModelState);
        }

        return onSuccess is null ? Ok(result.Value) : onSuccess(result.Value!);
    }
}
