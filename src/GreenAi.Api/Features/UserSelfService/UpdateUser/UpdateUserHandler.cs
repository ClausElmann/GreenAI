using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.UpdateUser;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
{
    private readonly IDbSession _db;
    private readonly ICurrentUser _currentUser;

    public UpdateUserHandler(IDbSession db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UpdateUserResponse>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        if (command.DisplayName is not null)
            await _db.ExecuteAsync(
                SqlLoader.Load<UpdateUserHandler>("UpdateUserDisplayName.sql"),
                new { DisplayName = command.DisplayName, ProfileId = _currentUser.ProfileId.Value });

        if (command.LanguageId is not null)
            await _db.ExecuteAsync(
                SqlLoader.Load<UpdateUserHandler>("UpdateUserLanguage.sql"),
                new { LanguageId = command.LanguageId.Value, UserId = _currentUser.UserId.Value, CustomerId = _currentUser.CustomerId.Value });

        return Result<UpdateUserResponse>.Ok(new UpdateUserResponse("Profile updated."));
    }
}
