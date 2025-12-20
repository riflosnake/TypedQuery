using System.Data;
using System.Data.Common;
using TypedQuery.Abstractions;

namespace TypedQuery;

/// <summary>
/// Extension methods for executing TypedQuery batches on raw DbConnection.
/// Supports raw SQL queries without requiring Entity Framework Core.
/// All execution uses Dapper for high-performance query execution.
/// </summary>
public static class TypedQueryDbConnectionExtensions
{
    /// <summary>
    /// Creates a fluent query executor for composing and executing queries.
    /// This is the entry point for raw SQL query execution.
    /// </summary>
    /// <param name="connection">The database connection</param>
    /// <param name="transaction">Optional database transaction</param>
    /// <returns>A TypedQueryExecutor for building and executing queries</returns>
    public static TypedQueryExecutor<DbConnection> ToTypedQuery(
        this DbConnection connection,
        DbTransaction? transaction = null)
    {
        return new TypedQueryExecutor<DbConnection>(
            connection,
            async (conn, builder, ct) => await ExecuteInternalAsync(conn, builder, transaction, ct));
    }

    /// <summary>
    /// Internal execution logic using Dapper for all query types.
    /// </summary>
    private static async Task<TypedQueryResult> ExecuteInternalAsync(
        DbConnection connection,
        TypedQueryBuilder builder,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction != null && connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                "A DbTransaction was provided but the connection is not open. " +
                "When using transactions, the caller must manage the connection lifetime.");
        }

        var context = new QueryBuildContext(null);
        
        var wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var batch = SqlBatchBuilder.Build(builder, context);

            return await DapperExecutionHelper.ExecuteBatchAsync(
                connection,
                batch,
                transaction,
                commandTimeout: null,
                cancellationToken);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
