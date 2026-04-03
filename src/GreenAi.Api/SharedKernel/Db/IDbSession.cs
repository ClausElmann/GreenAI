namespace GreenAi.Api.SharedKernel.Db;

public interface IDbSession : IDisposable
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);

    /// <summary>
    /// Executes <paramref name="work"/> inside a single SQL transaction on the shared connection.
    /// Commits on success, rolls back on any exception (which is re-thrown).
    /// All ExecuteAsync calls made during work() participate in the transaction automatically.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> work);
}
