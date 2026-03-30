namespace GreenAi.Api.SharedKernel.Db;

public interface IDbSession : IDisposable
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);
    System.Data.IDbTransaction BeginTransaction();
}
