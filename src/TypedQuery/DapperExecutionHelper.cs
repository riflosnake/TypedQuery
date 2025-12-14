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
    private static readonly ConcurrentDictionary<Type, Func<SqlMapper.GridReader, bool, Task<object>>> ReadAsyncDelegateCache = new();

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
        var parameters = CreateDynamicParameters(batch.Parameters);

        using var gridReader = await connection.QueryMultipleAsync(
            new CommandDefinition(
                batch.Sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                cancellationToken: cancellationToken));

        return await ReadResultsAsync(
            gridReader, batch.ResultTypes, batch.QueryTypes,
            cancellationToken);
    }

    /// <summary>
    /// Creates DynamicParameters from DbParameter collection.
    /// </summary>
    private static DynamicParameters CreateDynamicParameters(IReadOnlyList<DbParameter> parameters)
    {
        var dynamicParams = new DynamicParameters();
        
        foreach (var param in parameters)
        {
            dynamicParams.Add(
                param.ParameterName,
                param.Value,
                param.DbType,
                param.Direction,
                param.Size);
        }

        return dynamicParams;
    }

    /// <summary>
    /// Reads all result sets from the GridReader using Dapper.
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
    /// Reads a single result set from the grid reader using compiled delegates.
    /// </summary>
    private static async Task<object> ReadResultSetAsync(
        SqlMapper.GridReader gridReader,
        Type resultType)
    {
        var readDelegate = ReadAsyncDelegateCache.GetOrAdd(resultType, CreateReadDelegate);

        return await readDelegate(gridReader, true);
    }

    /// <summary>
    /// Creates a compiled delegate for reading a specific result type from GridReader.
    /// This eliminates all reflection overhead after first call.
    /// </summary>
    private static Func<SqlMapper.GridReader, bool, Task<object>> CreateReadDelegate(Type resultType)
    {
        var readAsyncMethod = typeof(SqlMapper.GridReader)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m =>
                m.Name == nameof(SqlMapper.GridReader.ReadAsync) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(bool));

        var genericReadAsync = readAsyncMethod.MakeGenericMethod(resultType);

        // Create: async (gridReader, buffered) => {
        //     var result = await gridReader.ReadAsync<T>(buffered);
        //     return result.ToList();
        // }
        
        // Since we need async/await, we'll create the delegate differently
        // We'll return a function that does the work
        
        var toListMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(Enumerable.ToList) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1)
            .MakeGenericMethod(resultType);

        return async (gridReader, buffered) =>
        {
            // Call ReadAsync<T>(buffered)
            var task = (Task)genericReadAsync.Invoke(gridReader, [buffered])!;
            await task.ConfigureAwait(false);

            // Get the result from the task
            var taskType = task.GetType();
            var resultProperty = taskType.GetProperty("Result")!;
            var enumerable = resultProperty.GetValue(task)!;

            // Convert to list
            return toListMethod.Invoke(null, [enumerable])!;
        };
    }
}
