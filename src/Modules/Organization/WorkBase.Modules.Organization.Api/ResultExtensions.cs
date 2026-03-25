using Microsoft.AspNetCore.Http;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Api;

internal static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return ToErrorResult(result.Error, result.ValidationErrors);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return ToErrorResult(result.Error, result.ValidationErrors);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, string routeName, Func<T, object> routeValues)
    {
        if (result.IsSuccess)
            return Results.CreatedAtRoute(routeName, routeValues(result.Value), result.Value);

        return ToErrorResult(result.Error, result.ValidationErrors);
    }

    private static IResult ToErrorResult(Error error, IReadOnlyList<ValidationError> validationErrors)
    {
        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
            ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
            ErrorType.Forbidden => Results.Forbid(),
            ErrorType.Validation => Results.BadRequest(new
            {
                error.Code,
                error.Message,
                Errors = validationErrors.Select(e => new { e.PropertyName, e.ErrorMessage })
            }),
            _ => Results.Problem(error.Message, statusCode: 500)
        };
    }
}
