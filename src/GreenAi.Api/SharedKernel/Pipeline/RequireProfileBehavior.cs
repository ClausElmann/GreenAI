using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.SharedKernel.Pipeline;

/// <summary>
/// Marker interface — apply to IRequest types that require a resolved profile context.
///
/// Rules:
/// <list type="bullet">
///   <item><description>ProfileId.Value must be &gt; 0 when this marker is present — enforced by <see cref="RequireProfileBehavior{TRequest,TResponse}"/>.</description></item>
///   <item><description>DO NOT apply to auth commands (Login, SelectCustomer, SelectProfile) — those commands establish the profile context, not consume it.</description></item>
///   <item><description>Apply to all business commands and queries that operate on profile-scoped data.</description></item>
/// </list>
/// </summary>
public interface IRequireProfile { }

/// <summary>
/// MediatR pipeline behavior — rejects requests marked <see cref="IRequireProfile"/>
/// when the current user's ProfileId is 0 (unresolved).
///
/// Pipeline order (enforced in Program.cs):
///   1. LoggingBehavior
///   2. AuthorizationBehavior   — confirms IsAuthenticated
///   3. RequireProfileBehavior  — confirms ProfileId &gt; 0 (this behavior)
///   4. ValidationBehavior      — business validation
///
/// This behavior must run AFTER AuthorizationBehavior so the principal is confirmed
/// authenticated before attempting to read ProfileId.
/// </summary>
public sealed class RequireProfileBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;

    public RequireProfileBehavior(ICurrentUser currentUser)
        => _currentUser = currentUser;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequireProfile && _currentUser.ProfileId.Value <= 0)
        {
            // Return Result<T>.Fail when TResponse is Result<T> — consistent with AuthorizationBehavior.
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failMethod = responseType.GetMethod("Fail", [typeof(string), typeof(string)]);
                if (failMethod is not null)
                {
                    var result = failMethod.Invoke(null, ["PROFILE_NOT_SELECTED", "A valid profile must be selected before this operation."]);
                    return (TResponse)result!;
                }
            }

            // Fallback for non-Result<T> responses (defensive — should not occur in this codebase).
            throw new InvalidOperationException("Profile context required. A valid profile must be selected.");
        }

        return await next();
    }
}
