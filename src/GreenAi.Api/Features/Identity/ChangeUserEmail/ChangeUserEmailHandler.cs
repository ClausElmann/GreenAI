using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Identity.ChangeUserEmail;

public sealed class ChangeUserEmailHandler : IRequestHandler<ChangeUserEmailCommand, Result<ChangeUserEmailResponse>>
{
    private readonly IChangeUserEmailRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ChangeUserEmailHandler(IChangeUserEmailRepository repository, ICurrentUser currentUser)
    {
        _repository  = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ChangeUserEmailResponse>> Handle(ChangeUserEmailCommand command, CancellationToken ct)
    {
        var available = await _repository.IsEmailAvailableAsync(command.NewEmail, _currentUser.UserId);

        if (!available)
            return Result<ChangeUserEmailResponse>.Fail("EMAIL_TAKEN", "That email address is already in use.");

        await _repository.UpdateEmailAndAuditAsync(_currentUser.UserId, _currentUser.CustomerId, command.NewEmail);

        return Result<ChangeUserEmailResponse>.Ok(new ChangeUserEmailResponse("Email updated successfully."));
    }
}
