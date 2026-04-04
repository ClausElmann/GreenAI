using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.SaveSetting;

/// <summary>
/// Updates a single ApplicationSetting by its enum int key.
/// SuperAdmin only — checked in handler.
/// </summary>
public sealed record SaveSettingCommand(int Key, string? Value)
    : IRequest<Result<SaveSettingResponse>>, IRequireAuthentication, IRequireProfile;
