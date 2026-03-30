using DbUp;
using DbUp.Engine;

namespace GreenAi.Api.Database;

public static class DatabaseMigrator
{
    public static void Run(string connectionString, ILogger logger)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                name => name.Contains(".Database.Migrations."))
            .WithTransaction()
            .LogTo(new DbUpLogger(logger))
            .Build();

        if (!upgrader.IsUpgradeRequired())
        {
            logger.LogInformation("[DB] Schema is up to date");
            return;
        }

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new Exception($"Database migration failed: {result.Error.Message}", result.Error);

        logger.LogInformation("[DB] Migration complete — {Count} script(s) applied",
            result.Scripts.Count());
    }

    private sealed class DbUpLogger : DbUp.Engine.Output.IUpgradeLog
    {
        private readonly ILogger _logger;
        public DbUpLogger(ILogger logger) => _logger = logger;
        public void LogTrace(string format, params object[] args) => _logger.LogTrace(format, args);
        public void LogDebug(string format, params object[] args) => _logger.LogDebug(format, args);
        public void LogInformation(string format, params object[] args) => _logger.LogInformation(format, args);
        public void LogWarning(string format, params object[] args) => _logger.LogWarning(format, args);
        public void LogError(Exception ex, string format, params object[] args) => _logger.LogError(ex, format, args);
        public void LogError(string format, params object[] args) => _logger.LogError(format, args);
    }
}
