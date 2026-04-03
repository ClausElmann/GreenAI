using Dapper;
using Microsoft.Data.SqlClient;

namespace GreenAi.Api.SharedKernel.Db;

/// <summary>
/// Scoped DB session backed by a lazily-opened SqlConnection.
///
/// The connection is NOT opened in the constructor — it is opened on the first
/// actual database call (Query/Execute). This means:
///   - Requests that are rejected by pipeline behaviors (auth, validation) before
///     any handler runs never open a connection at all.
///   - Connection pool pressure is reduced for short-circuited request paths.
/// </summary>
public sealed class DbSession : IDbSession
{
    private readonly string _connectionString;
    private SqlConnection? _connection;
    private System.Data.IDbTransaction? _activeTransaction;

    public DbSession(string connectionString)
    {
        _connectionString = connectionString;
    }

    private SqlConnection Connection
    {
        get
        {
            if (_connection is null)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
        => Connection.QueryAsync<T>(sql, param);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
        => Connection.QuerySingleOrDefaultAsync<T>(sql, param);

    public Task<int> ExecuteAsync(string sql, object? param = null)
        => Connection.ExecuteAsync(sql, param, transaction: _activeTransaction);

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(Func<Task> work)
    {
        _activeTransaction = Connection.BeginTransaction();
        try
        {
            await work();
            _activeTransaction.Commit();
        }
        catch
        {
            _activeTransaction.Rollback();
            throw;
        }
        finally
        {
            _activeTransaction.Dispose();
            _activeTransaction = null;
        }
    }

    public void Dispose()
    {
        _activeTransaction?.Dispose();
        _connection?.Dispose();
    }
}

