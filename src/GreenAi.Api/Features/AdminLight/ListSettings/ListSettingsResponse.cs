namespace GreenAi.Api.Features.AdminLight.ListSettings;

public sealed record ListSettingsResponse(IReadOnlyList<SettingDto> Settings);

public sealed record SettingDto(int Key, string Name, string? Value);
