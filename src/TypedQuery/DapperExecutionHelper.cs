using Dapper;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace TypedQuery;

/// <summary>
/// Shared Dapper execution utilities for all TypedQuery execution paths.
/// Provides consistent, high-performance query execution using Dapper.
/// </summary>
internal static class DapperExecutionHelper
{
    // Cache for strongly-typed read delegates per result type
    private static readonly ConcurrentDictionary<Type, Func<SqlMapper.GridReader, Task<object>>> ReadDelegateCache = new();

    /// <summary>
    /// Executes a SQL batch using Dapper's QueryMultipleAsync.
    /// </summary>
    public static async Task<TypedQueryResult> ExecuteBatchAsync(
        DbConnection connection,
        SqlBatch batch,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        using var gridReader = await connection.QueryMultipleAsync(
            new CommandDefinition(
                batch.Sql,
                batch.Parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                cancellationToken: cancellationToken));

        return await ReadResultsAsync(
            gridReader, 
            batch.ResultTypes, 
            batch.QueryTypes,
            cancellationToken);
    }

    /// <summary>
    /// Reads all result sets from the GridReader.
    /// </summary>
    private static async Task<TypedQueryResult> ReadResultsAsync(
        SqlMapper.GridReader gridReader,
        IReadOnlyList<Type> resultTypes,
        IReadOnlyList<Type>? queryTypes,
        CancellationToken ct = default)
    {
        var resultCount = resultTypes.Count;
        var results = new List<(Type ResultType, Type? QueryType, object? Data)>(resultCount);

        for (int i = 0; i < resultCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var resultType = resultTypes[i];
            var queryType = queryTypes != null && i < queryTypes.Count ? queryTypes[i] : null;

            var data = await ReadResultSetAsync(gridReader, resultType);
            results.Add((resultType, queryType, data));
        }

        return TypedQueryResult.FromData(results);
    }

    /// <summary>
    /// Reads a single result set from the grid reader.
    /// Uses cached delegate for performance and to avoid reflection issues in production.
    /// </summary>
    private static Task<object> ReadResultSetAsync(
        SqlMapper.GridReader gridReader,
        Type resultType)
    {
        var readDelegate = ReadDelegateCache.GetOrAdd(resultType, CreateReadDelegate);
        return readDelegate(gridReader);
    }

    /// <summary>
    /// Creates a delegate that reads a result set of a specific type.
    /// Uses a simple async wrapper to avoid DynamicMethod issues.
    /// </summary>
    private static Func<SqlMapper.GridReader, Task<object>> CreateReadDelegate(Type resultType)
    {
        // Create a generic method call via a helper that avoids DynamicMethod
        var helperMethod = typeof(DapperExecutionHelper)
            .GetMethod(nameof(ReadAsyncHelper), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType);

        // Create a delegate from the method
        return (Func<SqlMapper.GridReader, Task<object>>)Delegate.CreateDelegate(
            typeof(Func<SqlMapper.GridReader, Task<object>>),
            helperMethod);
    }

    /// <summary>
    /// Helper method that performs the actual read with proper generic typing.
    /// This avoids the "Invalid type owner for DynamicMethod" error by using
    /// a statically-defined method instead of expression compilation.
    /// </summary>
    private static async Task<object> ReadAsyncHelper<T>(SqlMapper.GridReader gridReader)
    {
        var result = await gridReader.ReadAsync<T>(buffered: true);
        return result;
    }
}
