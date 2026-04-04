using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignProfile;

public sealed record AssignProfileCommand(int TargetUserId, int ProfileId)
    : IRequest<Result<AssignProfileResponse>>, IRequireAuthentication, IRequireProfile;
