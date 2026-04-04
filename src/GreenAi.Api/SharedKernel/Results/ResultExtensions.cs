using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace GreenAi.Api.SharedKernel.Results;

/// <summary>
/// Maps Result&lt;T&gt; to the appropriate IResult (HTTP response) based on the error code.
/// Use this in all Minimal API endpoints instead of inline status code logic.
/// 500 responses include an ErrorId that is also written to the Logs table via Serilog
/// through the MediatR LoggingBehavior.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return HttpResults.Ok(result.Value);

        return result.Error!.Code switch
        {
            // 400 — input rejected before reaching business logic
            "VALIDATION_ERROR"       => HttpResults.Problem(result.Error.Message, statusCode: 400),

            // 401 — caller is not authenticated or credentials are wrong
            "UNAUTHORIZED"           => HttpResults.Problem(result.Error.Message, statusCode: 401),
            "INVALID_CREDENTIALS"    => HttpResults.Problem(result.Error.Message, statusCode: 401),
            "INVALID_REFRESH_TOKEN"  => HttpResults.Problem(result.Error.Message, statusCode: 401),
            "PROFILE_NOT_SELECTED"   => HttpResults.Problem(result.Error.Message, statusCode: 401),

            // 403 — authenticated but not permitted
            "FORBIDDEN"              => HttpResults.Problem(result.Error.Message, statusCode: 403),
            "ACCOUNT_LOCKED"         => HttpResults.Problem(result.Error.Message, statusCode: 403),
            "ACCOUNT_HAS_NO_TENANT"  => HttpResults.Problem(result.Error.Message, statusCode: 403),
            "MEMBERSHIP_NOT_FOUND"   => HttpResults.Problem(result.Error.Message, statusCode: 403),
            "PROFILE_ACCESS_DENIED"  => HttpResults.Problem(result.Error.Message, statusCode: 403),

            // 404 — resource not found
            "PROFILE_NOT_FOUND"      => HttpResults.Problem(result.Error.Message, statusCode: 404),
            "NOT_FOUND"              => HttpResults.Problem(result.Error.Message, statusCode: 404),

            // 400 — token is invalid or expired
            "INVALID_TOKEN"          => HttpResults.Problem(result.Error.Message, statusCode: 400),

            // 409 — conflict: resource already exists or state prevents the action
            "EMAIL_TAKEN"            => HttpResults.Problem(result.Error.Message, statusCode: 409),

            // 500 — unexpected error (error code not mapped above)
            // ErrorId links this response to the Logs table entry created by LoggingBehavior.
            _ => HttpResults.Problem(
                detail: result.Error.Message,
                statusCode: 500,
                extensions: new Dictionary<string, object?> { ["errorId"] = Guid.NewGuid() }),
        };
    }
}
