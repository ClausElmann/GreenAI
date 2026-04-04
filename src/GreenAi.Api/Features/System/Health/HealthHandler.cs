using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using GreenAi.Api.SharedKernel.Settings;
using MediatR;
using System.Reflection;

namespace GreenAi.Api.Features.System.Health;

public sealed class HealthHandler : IRequestHandler<HealthQuery, Result<HealthResponse>>
{
    private readonly IDbSession _db;
    private readonly IApplicationSettingService _settings;

    public HealthHandler(IDbSession db, IApplicationSettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<Result<HealthResponse>> Handle(HealthQuery request, CancellationToken cancellationToken)
    {
        var dbOk = false;
        var configOk = false;

        try
        {
            await _db.QuerySingleOrDefaultAsync<int>(SqlLoader.Load<HealthHandler>("DbPing.sql"));
            dbOk = true;
        }
        catch { /* DB unreachable — rapporteres via dbOk = false */ }

        try
        {
            // Bekræft at config kan læses (RequestLogLevel er et grøn-ai nøgle der altid er til stede)
            await _settings.GetAsync(AppSetting.RequestLogLevel, defaultValue: "off");
            configOk = true;
        }
        catch { /* Config unreachable — rapporteres via configOk = false */ }

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        var status = dbOk && configOk ? "healthy" : "degraded";

        return Result<HealthResponse>.Ok(new HealthResponse(
            Status:      status,
            Version:     version,
            DatabaseOk:  dbOk,
            ConfigOk:    configOk,
            Timestamp:   DateTimeOffset.UtcNow));
    }
}
