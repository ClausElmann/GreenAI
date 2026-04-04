using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.UpdateUser;

/// <summary>
/// Updates the current user's profile display name and/or language preference.
/// Both fields are optional — omit to leave unchanged.
/// </summary>
public sealed record UpdateUserCommand(
    string? DisplayName,
    int?    LanguageId)
    : IRequest<Result<UpdateUserResponse>>, IRequireAuthentication, IRequireProfile;
