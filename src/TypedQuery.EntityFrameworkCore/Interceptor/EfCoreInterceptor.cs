using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data.Common;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

public sealed class TypedQueryInterceptor : DbCommandInterceptor
{
    /// <summary>
    /// The prefix used for cache keys in SQL tags.
    /// </summary>
    internal const string CacheKeyPrefix = "TypedQuery|MODE=CACHE_ONLY|ID=";

    private const string TagPrefix = "-- TypedQuery|";
    private const string IdToken = "ID=";
    
    private static readonly char[] LineEndingChars = ['\r', '\n'];
    
    private static readonly ConcurrentDictionary<string, CapturedQuery> _sqlCache = new();
    
    private static readonly ConcurrentDictionary<Type, DbProviderFactory> _factoryCache = new();
    
    private static long _queryIdCounter;

    /// <summary>
    /// Generates a unique query ID using a thread-safe incrementing counter.
    /// Much faster than Guid.NewGuid().ToString().
    /// </summary>
    internal static string GenerateQueryId()
    {
        return Interlocked.Increment(ref _queryIdCounter).ToString();
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!command.CommandText.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
            return result;

        var cacheKey = ExtractQueryId(command.CommandText);
        if (cacheKey is null)
            return result;

        var clonedParams = new DbParameter[command.Parameters.Count];

        var connection = command.Connection ?? throw new InvalidOperationException(
            "DbCommand.Connection is null");
        
        var factory = GetCachedFactory(connection);

        for (int i = 0; i < command.Parameters.Count; i++)
        {
            var source = command.Parameters[i];

            var p = factory.CreateParameter()
                ?? throw new InvalidOperationException("Failed to create DbParameter");

            p.ParameterName = source.ParameterName;
            p.Value = source.Value ?? DBNull.Value;
            p.DbType = source.DbType;
            p.Direction = source.Direction;
            p.Size = source.Size;
            p.Precision = source.Precision;
            p.Scale = source.Scale;
            p.IsNullable = source.IsNullable;

            clonedParams[i] = p;
        }

        _sqlCache[cacheKey] = new CapturedQuery(command.CommandText, clonedParams);

        return InterceptionResult<DbDataReader>.SuppressWithResult(EmptyDataReader.Instance);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
        => new(ReaderExecuting(command, eventData, result));

    /// <summary>
    /// Retrieves cached SQL and parameters by query ID.
    /// </summary>
    internal static CapturedQuery? PullCapturedQuery(string queryId)
    {
        return _sqlCache.TryRemove(queryId, out var captured) ? captured : null;
    }

    /// <summary>
    /// Clears all cached queries.
    /// </summary>
    internal static void ClearCache()
    {
        _sqlCache.Clear();
    }

    private static DbProviderFactory GetCachedFactory(DbConnection connection)
    {
        var connectionType = connection.GetType();
        
        return _factoryCache.GetOrAdd(connectionType, _ =>
        {
            return DbProviderFactories.GetFactory(connection) 
                ?? throw new NotSupportedException(
                    $"Provider {connectionType.Name} does not expose DbProviderFactory");
        });
    }

    private static string? ExtractQueryId(string sql)
    {
        var end = sql.IndexOfAny(LineEndingChars);
        var firstLine = end > 0 ? sql[..end] : sql;

        var idx = firstLine.IndexOf(IdToken, StringComparison.Ordinal);
        if (idx < 0) return null;

        return firstLine[(idx + IdToken.Length)..].Trim();
    }
}
