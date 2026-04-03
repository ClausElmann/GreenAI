using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public interface ISelectCustomerRepository
{
    Task<MembershipRecord?> FindMembershipAsync(UserId userId, CustomerId customerId);
    Task<IReadOnlyCollection<ProfileRecord>> GetProfilesAsync(UserId userId, CustomerId customerId);
}
