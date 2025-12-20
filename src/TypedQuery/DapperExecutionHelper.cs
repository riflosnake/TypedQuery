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
    // Cache for ReadAsync<T> method info per result type
    private static readonly ConcurrentDictionary<Type, MethodInfo> ReadAsyncMethodCache = new();
    
    // Generic ReadAsync method from GridReader
    private static readonly MethodInfo GenericReadAsyncMethod = typeof(SqlMapper.GridReader)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(m =>
            m.Name == nameof(SqlMapper.GridReader.ReadAsync) &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(bool));

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
    /// Uses cached MethodInfo for performance.
    /// </summary>
    private static async Task<object> ReadResultSetAsync(
        SqlMapper.GridReader gridReader,
        Type resultType)
    {
        var method = ReadAsyncMethodCache.GetOrAdd(resultType, t => 
            GenericReadAsyncMethod.MakeGenericMethod(t));

        // Invoke ReadAsync<T>(buffered: true)
        var task = (Task)method.Invoke(gridReader, new object[] { true })!;
        await task.ConfigureAwait(false);

        // Get the Result property value (which is IEnumerable<T>)
        var resultProperty = task.GetType().GetProperty("Result")!;
        return resultProperty.GetValue(task)!;
    }
}
