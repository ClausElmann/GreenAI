using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using GreenAi.Api.SharedKernel.Settings;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.SaveSetting;

public sealed class SaveSettingHandler
    : IRequestHandler<SaveSettingCommand, Result<SaveSettingResponse>>
{
    private readonly ICurrentUser               _currentUser;
    private readonly IPermissionService         _permissions;
    private readonly IApplicationSettingService _settings;

    public SaveSettingHandler(
        ICurrentUser               currentUser,
        IPermissionService         permissions,
        IApplicationSettingService settings)
    {
        _currentUser = currentUser;
        _permissions = permissions;
        _settings    = settings;
    }

    public async Task<Result<SaveSettingResponse>> Handle(
        SaveSettingCommand command, CancellationToken ct)
    {
        if (!await _permissions.IsUserSuperAdminAsync(_currentUser.UserId))
            return Result<SaveSettingResponse>.Fail("FORBIDDEN", "SuperAdmin access required.");

        if (!Enum.IsDefined(typeof(AppSetting), command.Key))
            return Result<SaveSettingResponse>.Fail("NOT_FOUND", $"No setting with key {command.Key}.");

        var setting = (AppSetting)command.Key;
        await _settings.SaveAsync(setting, command.Value);

        return Result<SaveSettingResponse>.Ok(
            new SaveSettingResponse($"Setting '{setting}' saved."));
    }
}
