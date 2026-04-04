using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

/// <summary>
/// Creates a new user and links them to the caller's customer.
/// Requires IRequireAuthentication + IRequireProfile.
/// Caller must hold UserRole.ManageUsers (checked in handler).
/// </summary>
public sealed record CreateUserCommand(
    string Email,
    string InitialPassword,
    int    LanguageId = 1)
    : IRequest<Result<CreateUserResponse>>, IRequireAuthentication, IRequireProfile;
