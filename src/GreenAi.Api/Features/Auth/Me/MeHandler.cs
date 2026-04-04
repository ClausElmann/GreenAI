using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Me;

public sealed class MeHandler : IRequestHandler<MeQuery, Result<MeResponse>>
{
    private readonly ICurrentUser _currentUser;

    public MeHandler(ICurrentUser currentUser) => _currentUser = currentUser;

    public Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken ct)
    {
        var response = new MeResponse(
            UserId:          _currentUser.UserId.Value,
            CustomerId:      _currentUser.CustomerId.Value,
            ProfileId:       _currentUser.ProfileId.Value,
            LanguageId:      _currentUser.LanguageId,
            Email:           _currentUser.Email,
            IsImpersonating: _currentUser.IsImpersonating,
            OriginalUserId:  _currentUser.OriginalUserId?.Value);

        return Task.FromResult(Result<MeResponse>.Ok(response));
    }
}
