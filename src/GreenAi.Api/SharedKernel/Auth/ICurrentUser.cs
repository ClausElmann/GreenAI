using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

public interface ICurrentUser
{
    UserId UserId { get; }
    CustomerId CustomerId { get; }
    ProfileId ProfileId { get; }
    bool IsImpersonating { get; }
    UserId? OriginalUserId { get; }
    bool IsAuthenticated { get; }
}
