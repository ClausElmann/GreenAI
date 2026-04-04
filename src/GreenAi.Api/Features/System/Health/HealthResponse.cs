namespace GreenAi.Api.Features.System.Health;

public record HealthResponse(
    string Status,
    string Version,
    bool DatabaseOk,
    bool ConfigOk,
    DateTimeOffset Timestamp);
