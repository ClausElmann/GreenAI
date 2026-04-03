using GreenAi.Api.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace GreenAi.Tests.SharedKernel.Results;

/// <summary>
/// Unit tests for ResultExtensions.ToHttpResult() — verifies the error code → HTTP status mapping.
///
/// PURPOSE: These tests prevent silent HTTP 500 responses from unregistered error codes.
/// When a handler emits Result.Fail("NEW_CODE", ...) without adding it to ResultExtensions.cs,
/// the switch default maps it to 500. This test suite enforces every code maps to its intended status.
///
/// MAINTENANCE: When adding a new error code to ResultExtensions.cs, add a corresponding test here.
///
/// Current catalog:
///   VALIDATION_ERROR       → 400
///   UNAUTHORIZED           → 401
///   INVALID_CREDENTIALS    → 401
///   INVALID_REFRESH_TOKEN  → 401
///   PROFILE_NOT_SELECTED   → 401
///   FORBIDDEN              → 403
///   ACCOUNT_LOCKED         → 403
///   ACCOUNT_HAS_NO_TENANT  → 403
///   MEMBERSHIP_NOT_FOUND   → 403
///   PROFILE_ACCESS_DENIED  → 403
///   PROFILE_NOT_FOUND      → 404
///   NOT_FOUND              → 404
///   EMAIL_TAKEN            → 409
/// </summary>
public sealed class ResultExtensionsTests
{
    private static int StatusFor(string errorCode)
    {
        var result = Result<string>.Fail(errorCode, "test message");
        var httpResult = result.ToHttpResult();

        // Extract status code via reflection on IResult — IStatusCodeHttpResult is the standard interface
        if (httpResult is IStatusCodeHttpResult statusResult)
            return statusResult.StatusCode ?? 200;

        // ProblemHttpResult implements IStatusCodeHttpResult; if cast fails, something changed fundamentally
        throw new InvalidOperationException(
            $"IResult for code '{errorCode}' does not implement IStatusCodeHttpResult — inspect ToHttpResult() return type.");
    }

    // ===================================================================
    // 400 — Validation
    // ===================================================================

    [Fact] public void VALIDATION_ERROR_returns_400() => Assert.Equal(400, StatusFor("VALIDATION_ERROR"));

    // ===================================================================
    // 401 — Authentication
    // ===================================================================

    [Fact] public void UNAUTHORIZED_returns_401()           => Assert.Equal(401, StatusFor("UNAUTHORIZED"));
    [Fact] public void INVALID_CREDENTIALS_returns_401()    => Assert.Equal(401, StatusFor("INVALID_CREDENTIALS"));
    [Fact] public void INVALID_REFRESH_TOKEN_returns_401()  => Assert.Equal(401, StatusFor("INVALID_REFRESH_TOKEN"));
    [Fact] public void PROFILE_NOT_SELECTED_returns_401()   => Assert.Equal(401, StatusFor("PROFILE_NOT_SELECTED"));

    // ===================================================================
    // 403 — Authorization
    // ===================================================================

    [Fact] public void FORBIDDEN_returns_403()             => Assert.Equal(403, StatusFor("FORBIDDEN"));
    [Fact] public void ACCOUNT_LOCKED_returns_403()        => Assert.Equal(403, StatusFor("ACCOUNT_LOCKED"));
    [Fact] public void ACCOUNT_HAS_NO_TENANT_returns_403() => Assert.Equal(403, StatusFor("ACCOUNT_HAS_NO_TENANT"));
    [Fact] public void MEMBERSHIP_NOT_FOUND_returns_403()  => Assert.Equal(403, StatusFor("MEMBERSHIP_NOT_FOUND"));
    [Fact] public void PROFILE_ACCESS_DENIED_returns_403() => Assert.Equal(403, StatusFor("PROFILE_ACCESS_DENIED"));

    // ===================================================================
    // 404 — Not Found
    // ===================================================================

    [Fact] public void PROFILE_NOT_FOUND_returns_404() => Assert.Equal(404, StatusFor("PROFILE_NOT_FOUND"));
    [Fact] public void NOT_FOUND_returns_404()          => Assert.Equal(404, StatusFor("NOT_FOUND"));

    // ===================================================================
    // 409 — Conflict
    // ===================================================================

    [Fact] public void EMAIL_TAKEN_returns_409() => Assert.Equal(409, StatusFor("EMAIL_TAKEN"));

    // ===================================================================
    // 500 — Catch-all (unmapped codes must NOT silently be used in handlers)
    // This test documents the default behavior but does NOT validate a real code.
    // RESULT-001 in Validate-GreenAiCompliance.ps1 catches unmapped codes statically.
    // ===================================================================

    [Fact]
    public void UnknownCode_returns_500()
    {
        // An unregistered code falls through to the default case
        // If this test fails, the default was changed — update this test and investigate why
        Assert.Equal(500, StatusFor("THIS_CODE_DOES_NOT_EXIST_AND_NEVER_SHOULD"));
    }
}
