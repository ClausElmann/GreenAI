# result-pattern

```yaml
id: result_pattern
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/result-pattern.md
red_thread: result_pattern + error_codes → docs/SSOT/governance/RED_THREAD_REGISTRY.md

contracts:

  - name: Result<T>
    file: src/GreenAi.Api/SharedKernel/Results/Result.cs
    shape:
      IsSuccess: bool
      Value:     T?      # non-null when IsSuccess = true
      Error:     Error?  # non-null when IsSuccess = false
    constructors:
      success: Result<T>.Ok(value)
      failure: Result<T>.Fail("ERROR_CODE", "human readable message")
      failure: Result<T>.Fail(new Error("ERROR_CODE", "message"))

  - name: Error
    shape:
      Code:    string   # from canonical error code table (see below)
      Message: string   # human-readable, may be shown to end users

  - name: ToHttpResult
    file: src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs
    usage: return result.ToHttpResult();
    rule: ALL endpoints MUST use ToHttpResult() — no inline status code logic

error_code_catalog:
  # Code → HTTP status — source: ResultExtensions.cs
  VALIDATION_ERROR:       400
  UNAUTHORIZED:           401
  INVALID_CREDENTIALS:    401
  INVALID_REFRESH_TOKEN:  401
  PROFILE_NOT_SELECTED:   401
  FORBIDDEN:              403
  ACCOUNT_LOCKED:         403
  ACCOUNT_HAS_NO_TENANT:  403
  MEMBERSHIP_NOT_FOUND:   403
  PROFILE_ACCESS_DENIED:  403
  PROFILE_NOT_FOUND:      404
  NOT_FOUND:              404
  EMAIL_TAKEN:            409
  NO_CUSTOMER:            500   # upgrade to 403 when tenant guard is made stricter
  _unmapped:              500   # any code not in the above list defaults to 500

rules:
  MUST:
    - ALL handlers return IRequest<Result<T>>
    - ALL endpoints call result.ToHttpResult()
    - Error.Code comes from error_code_catalog ONLY
    - New error codes added to ResultExtensions.cs before use
    - New codes documented in error_code_catalog above
  MUST_NOT:
    - Throw exceptions for expected business outcomes
    - Return null instead of Result<T>.Fail(...)
    - Hardcode HTTP status codes in endpoint Map() methods
    - Invent error codes not in ResultExtensions.cs

flow:

  - step: handler_returns
    action: return Result<T>.Ok(response)  OR  Result<T>.Fail("CODE", "msg")

  - step: endpoint_maps
    action: return result.ToHttpResult()
    next: HTTP response per error_code_catalog

  - step: blazor_handles
    action:
      if result.IsSuccess: use result.Value
      else: display result.Error!.Message to user

patterns:

  - id: success_pattern
    code: |
      var data = await db.Connection.QuerySingleAsync<MyResponse>(query, parameters);
      return Result<MyResponse>.Ok(data);

  - id: guard_pattern
    code: |
      if (!user.CustomerId.HasValue)
          return Result<MyResponse>.Fail("NO_CUSTOMER", "No customer selected.");

  - id: endpoint_pattern
    code: |
      var result = await mediator.Send(command, ct);
      return result.ToHttpResult();

anti_patterns:

  - detect: handler throws NotFoundException or ArgumentException
    why_wrong: exceptions for expected states bypass Result pipeline and kill logging
    fix: return Result<T>.Fail("PROFILE_NOT_FOUND", "...") and let ToHttpResult map it

  - detect: endpoint returns Results.NotFound() directly
    why_wrong: bypasses ToHttpResult mapping — error code catalog not applied
    fix: handler returns Result.Fail("PROFILE_NOT_FOUND"), endpoint calls result.ToHttpResult()

  - detect: custom error code not in ResultExtensions.cs
    why_wrong: maps to 500 silently — wrong HTTP status, invisible bug
    fix: add code to ResultExtensions.cs with correct HTTP status first

enforcement:

  - where: Features/**/Handler.cs
    how: return type must be Task<Result<T>>

  - where: Features/**/Endpoint.cs
    how: must contain exactly one call to .ToHttpResult()

  - where: ResultExtensions.cs
    how: canonical switch expression — add new codes here
```
