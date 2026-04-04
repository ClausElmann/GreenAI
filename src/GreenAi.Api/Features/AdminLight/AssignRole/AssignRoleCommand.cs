using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignRole;

public sealed record AssignRoleCommand(int UserId, string RoleName)
    : IRequest<Result<AssignRoleResponse>>, IRequireAuthentication, IRequireProfile;
