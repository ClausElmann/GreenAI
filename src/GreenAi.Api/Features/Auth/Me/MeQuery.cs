using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Me;

/// <summary>Returns the resolved identity context for the authenticated user.</summary>
public sealed record MeQuery : IRequest<Result<MeResponse>>;
