using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Identity.ChangeUserEmail;

public sealed record ChangeUserEmailCommand(
    string NewEmail,
    string ConfirmNewEmail) : IRequest<Result<ChangeUserEmailResponse>>, IRequireAuthentication, IRequireProfile;
