using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GreenAi.Tests.SharedKernel.Logging;

/// <summary>
/// Unit tests for DefaultSystemLogger.
///
/// Validates that each log-level method delegates to ILogger&lt;T&gt;.
/// Serilog LogContext enrichment is a side effect of the real Serilog pipeline
/// and is verified by the end-to-end SQL log query in Slice006Tests.
/// </summary>
public sealed class DefaultSystemLoggerTests
{
    private static (DefaultSystemLogger Logger, ILogger<DefaultSystemLogger> Inner) Create(
        bool isAuthenticated = true,
        int userId = 42,
        int customerId = 7)
    {
        var inner = Substitute.For<ILogger<DefaultSystemLogger>>();
        inner.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(isAuthenticated);
        currentUser.UserId.Returns(new UserId(userId));
        currentUser.CustomerId.Returns(new CustomerId(customerId));

        return (new DefaultSystemLogger(inner, currentUser), inner);
    }

    [Fact]
    public void Information_CallsLoggerAtInformationLevel()
    {
        var (log, inner) = Create();

        log.Information("Hello {Name}", "world");

        inner.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Warning_CallsLoggerAtWarningLevel()
    {
        var (log, inner) = Create();

        log.Warning("Something odd {Value}", 99);

        inner.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Error_WithException_CallsLoggerAtErrorLevel()
    {
        var (log, inner) = Create();
        var ex = new InvalidOperationException("boom");

        log.Error(ex, "Failed {Op}", "save");

        inner.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            ex,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Error_WithoutException_CallsLoggerAtErrorLevel()
    {
        var (log, inner) = Create();

        log.Error("Oops {Code}", 500);

        inner.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Debug_CallsLoggerAtDebugLevel()
    {
        var (log, inner) = Create();

        log.Debug("Trace point {X}", 1);

        inner.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void AnyLevel_UnauthenticatedUser_DoesNotThrow()
    {
        // When IsAuthenticated=false, PushUserContext returns null — must handle gracefully
        var (log, _) = Create(isAuthenticated: false);

        var ex = Record.Exception(() => log.Information("Unauthenticated ping"));

        Assert.Null(ex);
    }
}
