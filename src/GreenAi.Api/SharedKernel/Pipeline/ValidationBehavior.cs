using MediatR;
using FluentValidation;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.SharedKernel.Pipeline;

/// <summary>
/// Pipeline behavior — runs FluentValidation validators registered for TRequest.
///
/// Pipeline order (registered in Program.cs):
///   1. LoggingBehavior
///   2. AuthorizationBehavior
///   3. RequireProfileBehavior
///   4. ValidationBehavior  (this behavior)
///
/// When TResponse is Result&lt;T&gt;, validation failures are returned as Result.Fail("VALIDATION_ERROR", ...)
/// rather than throwing — consistent with the Result&lt;T&gt; error-handling contract.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failMethod = responseType.GetMethod("Fail", [typeof(string), typeof(string)]);
                if (failMethod is not null)
                {
                    var message = string.Join("; ", failures.Select(f => f.ErrorMessage));
                    var result = failMethod.Invoke(null, ["VALIDATION_ERROR", message]);
                    return (TResponse)result!;
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
