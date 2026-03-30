using Dapper;
using Microsoft.Data.SqlClient;

namespace GreenAi.Api.SharedKernel.Db;

public sealed class DbSession : IDbSession
{
    private readonly SqlConnection _connection;

    public DbSession(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        _connection.Open();
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
        => _connection.QueryAsync<T>(sql, param);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
        => _connection.QuerySingleOrDefaultAsync<T>(sql, param);

    public Task<int> ExecuteAsync(string sql, object? param = null)
        => _connection.ExecuteAsync(sql, param);

    public System.Data.IDbTransaction BeginTransaction()
        => _connection.BeginTransaction();

    public void Dispose() => _connection.Dispose();
}
