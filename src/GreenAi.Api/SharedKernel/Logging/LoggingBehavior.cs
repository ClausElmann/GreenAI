using MediatR;
using System.Diagnostics;

namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// MediatR pipeline behavior — logger alle commands/queries med duration.
/// Registrer som første behavior i PipelineRegistration.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "[MediatR] {RequestName} completed in {Elapsed}ms",
                name, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[MediatR] {RequestName} failed after {Elapsed}ms",
                name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
