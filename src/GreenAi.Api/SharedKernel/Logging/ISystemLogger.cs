namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// Structured logger that automatically enriches log entries with UserId and CustomerId
/// from the current request context.
///
/// Wraps Serilog via ILogger&lt;T&gt; — all output goes to the same Serilog sinks
/// (SQL [dbo].[Logs] + console) including the UserId and CustomerId columns defined
/// in SerilogColumnOptions.
///
/// Usage: inject ISystemLogger in handlers, middlewares, and services that need
/// user-context-enriched output. For infrastructure/startup code use ILogger&lt;T&gt; directly.
/// </summary>
public interface ISystemLogger
{
    void Information(string messageTemplate, params object?[] args);
    void Warning(string messageTemplate, params object?[] args);
    void Error(string messageTemplate, params object?[] args);
    void Error(Exception exception, string messageTemplate, params object?[] args);
    void Debug(string messageTemplate, params object?[] args);
}
