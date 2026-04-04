using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

public sealed record CreateUserResponse(UserId UserId, string Message);
