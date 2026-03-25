using Microsoft.AspNetCore.Http;
using WorkBase.Shared.Domain;

namespace WorkBase.Shared.Api;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : MapError(result);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : MapError(result);

    public static IResult ToCreatedResult<T>(this Result<T> result, string uri) =>
        result.IsSuccess ? Results.Created(uri, result.Value) : MapError(result);

    private static IResult MapError(Result result)
    {
        if (result.ValidationErrors.Count > 0)
        {
            var grouped = result.ValidationErrors
                .GroupBy(v => v.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(grouped);
        }

        var (statusCode, title) = result.Error.Type switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.Validation => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Error.Message,
            extensions: new Dictionary<string, object?> { ["errorCode"] = result.Error.Code });
    }
}
