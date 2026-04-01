using Microsoft.AspNetCore.Components.Server.Circuits;

namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// Logger Blazor Server circuit events (connect, disconnect, resume).
/// </summary>
public sealed class LoggingCircuitHandler : CircuitHandler
{
    private readonly ILogger<LoggingCircuitHandler> _logger;

    public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger)
        => _logger = logger;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation("[Circuit] Opened — {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation("[Circuit] Closed — {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogDebug("[Circuit] Reconnected — {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogWarning("[Circuit] Disconnected — {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
