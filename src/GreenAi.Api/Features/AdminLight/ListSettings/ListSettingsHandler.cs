using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using GreenAi.Api.SharedKernel.Settings;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.ListSettings;

public sealed class ListSettingsHandler
    : IRequestHandler<ListSettingsQuery, Result<ListSettingsResponse>>
{
    private readonly ICurrentUser               _currentUser;
    private readonly IPermissionService         _permissions;
    private readonly IApplicationSettingService _settings;

    public ListSettingsHandler(
        ICurrentUser               currentUser,
        IPermissionService         permissions,
        IApplicationSettingService settings)
    {
        _currentUser = currentUser;
        _permissions = permissions;
        _settings    = settings;
    }

    public async Task<Result<ListSettingsResponse>> Handle(
        ListSettingsQuery request, CancellationToken ct)
    {
        if (!await _permissions.IsUserSuperAdminAsync(_currentUser.UserId))
            return Result<ListSettingsResponse>.Fail("FORBIDDEN", "SuperAdmin access required.");

        var dtos = new List<SettingDto>();
        foreach (AppSetting setting in Enum.GetValues<AppSetting>())
        {
            var value = await _settings.GetAsync(setting);
            dtos.Add(new SettingDto((int)setting, setting.ToString(), value));
        }

        return Result<ListSettingsResponse>.Ok(new ListSettingsResponse(dtos));
    }
}
