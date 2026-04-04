using GreenAi.Api.SharedKernel.Auth;
using Serilog.Context;

namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// Serilog-backed implementation of ISystemLogger.
///
/// On every log call, pushes UserId and CustomerId from ICurrentUser into
/// Serilog's LogContext so they appear in the extra columns defined in
/// SerilogColumnOptions (dbo.Logs.UserId, dbo.Logs.CustomerId).
///
/// Uses ICurrentUser.IsAuthenticated to guard context reads — safe to call
/// from unauthenticated code paths (values will be null in the log row).
/// </summary>
public sealed class DefaultSystemLogger : ISystemLogger
{
    private readonly ILogger<DefaultSystemLogger> _logger;
    private readonly ICurrentUser _currentUser;

    public DefaultSystemLogger(ILogger<DefaultSystemLogger> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public void Information(string messageTemplate, params object?[] args)
    {
        using var _ = PushUserContext();
        _logger.LogInformation(messageTemplate, args);
    }

    public void Warning(string messageTemplate, params object?[] args)
    {
        using var _ = PushUserContext();
        _logger.LogWarning(messageTemplate, args);
    }

    public void Error(string messageTemplate, params object?[] args)
    {
        using var _ = PushUserContext();
        _logger.LogError(messageTemplate, args);
    }

    public void Error(Exception exception, string messageTemplate, params object?[] args)
    {
        using var _ = PushUserContext();
        _logger.LogError(exception, messageTemplate, args);
    }

    public void Debug(string messageTemplate, params object?[] args)
    {
        using var _ = PushUserContext();
        _logger.LogDebug(messageTemplate, args);
    }

    // -------------------------------------------------------------------------

    private IDisposable? PushUserContext()
    {
        if (!_currentUser.IsAuthenticated)
            return null;

        // Stack two properties — both are captured in the same log event.
        // LogContext.PushProperty returns IDisposable; disposing removes the property.
        // We dispose both via the composite disposable at the call site.
        var userId     = LogContext.PushProperty("UserId",     _currentUser.UserId.Value);
        var customerId = LogContext.PushProperty("CustomerId", _currentUser.CustomerId.Value);

        return new CompositeDisposable(userId, customerId);
    }

    private sealed class CompositeDisposable(params IDisposable[] items) : IDisposable
    {
        public void Dispose()
        {
            foreach (var item in items)
                item.Dispose();
        }
    }
}
