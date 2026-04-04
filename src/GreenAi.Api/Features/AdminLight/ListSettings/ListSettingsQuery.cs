using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.ListSettings;

/// <summary>
/// Returns all ApplicationSettings key-value pairs.
/// SuperAdmin only — checked in handler.
/// </summary>
public sealed record ListSettingsQuery
    : IRequest<Result<ListSettingsResponse>>, IRequireAuthentication, IRequireProfile;
