using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Tenant;

/// <summary>
/// Tenant context derived from the authenticated ICurrentUser.
/// Provides CustomerId for all DB queries.
/// Register as Scoped.
/// </summary>
public sealed class CurrentUserTenantContext : ITenantContext
{
    private readonly ICurrentUser _currentUser;

    public CurrentUserTenantContext(ICurrentUser currentUser)
        => _currentUser = currentUser;

    public CustomerId CustomerId => _currentUser.CustomerId;
}
