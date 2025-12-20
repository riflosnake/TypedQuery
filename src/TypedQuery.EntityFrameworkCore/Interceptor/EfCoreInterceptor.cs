using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

/// <summary>
/// Interceptor that captures EF Core generated SQL for compilation.
/// 
/// This interceptor serves one purpose: capture SQL + parameters from EF Core
/// during the "compilation" phase, so subsequent executions can bypass EF Core
/// entirely and use Dapper directly.
/// 
/// Execution Modes:
/// - First call: EF Core compiles LINQ → interceptor captures SQL → cache template
/// - Subsequent calls: Skip EF Core → use cached SQL → execute via Dapper
/// 
/// Parameter Binding Strategy (No Collision Limitation):
/// 1. First, try to match by parameter NAME (e.g., @__categoryId_0 → field 'categoryId')
/// 2. If name matching fails, fall back to value matching
/// 3. This hybrid approach handles both named and anonymous parameters
/// </summary>
public sealed class TypedQueryInterceptor : DbCommandInterceptor
{
    internal const string CacheKeyPrefix = "TypedQuery|MODE=CACHE_ONLY|ID=";
    private const string TagPrefix = "-- TypedQuery|";
    private const string IdToken = "ID=";
    private static readonly char[] LineEndingChars = ['\r', '\n'];

    // Immediate captures: queryId → captured (short-lived, for passing to Build)
    private static readonly ConcurrentDictionary<string, CapturedQuery> _captures = new();
    
    // Compiled templates: QueryType → template (long-lived, null = not compilable)
    private static readonly ConcurrentDictionary<Type, CompiledSqlTemplate?> _compiledTemplates = new();
    
    // Pending compilation: queryId → query instance (for field-value matching)
    private static readonly ConcurrentDictionary<string, object> _pendingCompilations = new();
    
    private static long _queryIdCounter;

    #region Public API

    internal static string GenerateQueryId() => 
        Interlocked.Increment(ref _queryIdCounter).ToString();

    /// <summary>
    /// Register query instance for compilation (first-time value matching).
    /// </summary>
    internal static void RegisterForCompilation(string queryId, object queryInstance) =>
        _pendingCompilations[queryId] = queryInstance;

    /// <summary>
    /// Check if a compiled template exists for this query type.
    /// </summary>
    public static bool HasCompiledTemplate(Type queryType) =>
        _compiledTemplates.ContainsKey(queryType);

    /// <summary>
    /// Check if query type is cacheable (has a non-null template).
    /// </summary>
    public static bool IsCacheable(Type queryType) =>
        _compiledTemplates.TryGetValue(queryType, out var t) && t != null;

    /// <summary>
    /// Get compiled template if available (null = not compilable or not yet compiled).
    /// </summary>
    internal static CompiledSqlTemplate? GetCompiledTemplate(Type queryType) =>
        _compiledTemplates.TryGetValue(queryType, out var t) ? t : null;

    /// <summary>
    /// Pull captured query from immediate cache.
    /// </summary>
    internal static CapturedQuery? PullCapture(string queryId) =>
        _captures.TryRemove(queryId, out var c) ? c : null;

    /// <summary>
    /// Clear all caches. Useful for testing.
    /// </summary>
    public static void ClearAll()
    {
        _captures.Clear();
        _compiledTemplates.Clear();
        _pendingCompilations.Clear();
    }

    /// <summary>
    /// Get cache statistics for diagnostics and testing.
    /// </summary>
    public static (int CaptureCount, int TemplateCount) GetCacheStats() =>
        (_captures.Count, _compiledTemplates.Count);

    #endregion

    #region Interception

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!command.CommandText.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
            return result;

        var queryId = ExtractQueryId(command.CommandText);
        if (queryId is null)
            return result;

        // Clone parameters
        var clonedParams = CloneParameters(command.Parameters);
        var captured = new CapturedQuery(command.CommandText, clonedParams);
        
        // Store for immediate retrieval
        _captures[queryId] = captured;

        // Try to compile template if this is a first-time capture
        if (_pendingCompilations.TryRemove(queryId, out var queryInstance))
        {
            var queryType = queryInstance.GetType();
            if (!_compiledTemplates.ContainsKey(queryType))
            {
                var template = TryCompileTemplate(captured, queryInstance);
                _compiledTemplates.TryAdd(queryType, template);
            }
        }

        // Suppress actual execution
        return InterceptionResult<DbDataReader>.SuppressWithResult(EmptyDataReader.Instance);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken ct = default) => new(ReaderExecuting(command, eventData, result));

    #endregion

    #region Compilation

    /// <summary>
    /// Try to compile a reusable template using hybrid matching:
    /// 1. First, try parameter name → field name matching
    /// 2. Fall back to value matching for unnamed parameters
    /// 
    /// Returns null only if neither strategy can resolve all parameters.
    /// </summary>
    private static CompiledSqlTemplate? TryCompileTemplate(CapturedQuery captured, object queryInstance)
    {
        if (captured.Parameters.Length == 0)
        {
            return new CompiledSqlTemplate(captured.Sql, []);
        }

        var queryType = queryInstance.GetType();
        var fields = queryType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var fieldsByName = fields.ToDictionary(f => f.Name.ToLowerInvariant(), f => f);
        
        var bindings = new ParameterBinding[captured.Parameters.Length];
        var usedFields = new HashSet<FieldInfo>();

        for (int i = 0; i < captured.Parameters.Length; i++)
        {
            var param = captured.Parameters[i];
            FieldInfo? matchedField = null;

            // Strategy 1: Try to match by parameter name
            var extractedName = ParameterNameAnalyzer.ExtractFieldName(param.ParameterName);
            if (extractedName != null)
            {
                // Try exact match first
                if (fieldsByName.TryGetValue(extractedName.ToLowerInvariant(), out var field) && 
                    !usedFields.Contains(field))
                {
                    matchedField = field;
                }
                
                // Try with common prefixes removed (e.g., primary constructor fields like <id>k__BackingField)
                if (matchedField == null)
                {
                    foreach (var kvp in fieldsByName)
                    {
                        if (usedFields.Contains(kvp.Value)) continue;
                        
                        // Check if field name contains the extracted name
                        if (kvp.Key.Contains(extractedName.ToLowerInvariant()))
                        {
                            matchedField = kvp.Value;
                            break;
                        }
                    }
                }
            }

            // Strategy 2: Fall back to value matching if name matching failed
            if (matchedField == null)
            {
                var paramValue = param.Value;
                
                // Can't match null/DBNull reliably
                if (paramValue is null or DBNull)
                    return null;

                int matchCount = 0;
                foreach (var field in fields)
                {
                    if (usedFields.Contains(field)) continue;
                    
                    var fieldValue = field.GetValue(queryInstance);
                    if (fieldValue != null && AreValuesEqual(paramValue, fieldValue))
                    {
                        matchedField = field;
                        matchCount++;
                    }
                }

                // Value matching: must match exactly one (collision = fail)
                if (matchCount != 1)
                    matchedField = null;
            }

            // If still no match, template is not cacheable
            if (matchedField == null)
                return null;

            usedFields.Add(matchedField);
            bindings[i] = new ParameterBinding(
                param.ParameterName,
                matchedField,
                param.DbType,
                param.Size,
                param.Precision,
                param.Scale
            );
        }

        return new CompiledSqlTemplate(captured.Sql, bindings);
    }

    private static bool AreValuesEqual(object paramValue, object fieldValue)
    {
        if (paramValue.GetType() == fieldValue.GetType())
            return paramValue.Equals(fieldValue);

        try
        {
            var converted = Convert.ChangeType(fieldValue, paramValue.GetType());
            return paramValue.Equals(converted);
        }
        catch { return false; }
    }

    #endregion

    #region Helpers

    private static DbParameter[] CloneParameters(DbParameterCollection source)
    {
        var cloned = new DbParameter[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            var s = source[i];
            // We need to create a generic parameter since we don't have the factory
            // Just store the values we need
            cloned[i] = new ClonedParameter
            {
                ParameterName = s.ParameterName,
                Value = s.Value ?? DBNull.Value,
                DbType = s.DbType,
                Direction = s.Direction,
                Size = s.Size,
                Precision = s.Precision,
                Scale = s.Scale,
                IsNullable = s.IsNullable
            };
        }
        return cloned;
    }

    private static string? ExtractQueryId(string sql)
    {
        var end = sql.IndexOfAny(LineEndingChars);
        var firstLine = end > 0 ? sql[..end] : sql;
        var idx = firstLine.IndexOf(IdToken, StringComparison.Ordinal);
        return idx < 0 ? null : firstLine[(idx + IdToken.Length)..].Trim();
    }

    #endregion
}

/// <summary>
/// Simple DbParameter implementation for storing cloned parameter values.
/// </summary>
internal sealed class ClonedParameter : DbParameter
{
    private DbType _dbType;
    private ParameterDirection _direction;

    public override DbType DbType 
    { 
        get => _dbType; 
        set => _dbType = value; 
    }
    
    public override ParameterDirection Direction 
    { 
        get => _direction; 
        set => _direction = value; 
    }
    
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = "";
    public override int Size { get; set; }
    public override string SourceColumn { get; set; } = "";
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    
    public override void ResetDbType() 
    { 
        _dbType = DbType.String; 
    }
}
