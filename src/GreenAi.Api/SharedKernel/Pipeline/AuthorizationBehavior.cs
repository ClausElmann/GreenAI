using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.SharedKernel.Pipeline;

/// <summary>
/// Marker interface — apply to IRequest types that require authentication.
/// </summary>
public interface IRequireAuthentication { }

/// <summary>
/// MediatR pipeline behavior — rejects unauthenticated requests that implement IRequireAuthentication.
/// Register after LoggingBehavior, before ValidationBehavior.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationBehavior(ICurrentUser currentUser)
        => _currentUser = currentUser;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequireAuthentication && !_currentUser.IsAuthenticated)
        {
            // Return Result<T>.Fail if TResponse is Result<T>, otherwise throw
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failMethod = responseType.GetMethod("Fail", [typeof(string), typeof(string)]);
                if (failMethod is not null)
                {
                    var result = failMethod.Invoke(null, ["UNAUTHORIZED", "Authentication required"]);
                    return (TResponse)result!;
                }
            }

            throw new UnauthorizedAccessException("Authentication required");
        }

        return await next();
    }
}
