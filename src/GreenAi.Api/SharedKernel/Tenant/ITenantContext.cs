using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Tenant;

public interface ITenantContext
{
    CustomerId CustomerId { get; }
}
