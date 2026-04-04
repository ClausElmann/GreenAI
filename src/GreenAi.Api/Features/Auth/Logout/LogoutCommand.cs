using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Logout;

/// <summary>
/// Invalidates all refresh tokens for the authenticated user.
/// UserId is taken from ICurrentUser (JWT claim) — no client payload needed.
/// </summary>
public sealed record LogoutCommand : IRequest<Result<LogoutResponse>>;
