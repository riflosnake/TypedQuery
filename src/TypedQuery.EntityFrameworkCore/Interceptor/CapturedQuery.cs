using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

/// <summary>
/// Captured query from EF Core interception (used for immediate retrieval).
/// </summary>
internal sealed class CapturedQuery
{
    public CapturedQuery(string sql, DbParameter[] parameters)
    {
        Sql = sql;
        Parameters = parameters;
    }
    
    public string Sql { get; }
    public DbParameter[] Parameters { get; }
    
    /// <summary>
    /// Converts captured DbParameters to DynamicParameters for Dapper execution.
    /// </summary>
    public DynamicParameters ToDynamicParameters()
    {
        var dp = new DynamicParameters();
        
        foreach (var param in Parameters)
        {
            dp.Add(
                param.ParameterName,
                param.Value == DBNull.Value ? null : param.Value,
                param.DbType,
                param.Direction,
                param.Size);
        }
        
        return dp;
    }
}

/// <summary>
/// Compiled SQL template for Dapper execution mode.
/// 
/// Uses compiled delegates for field access - no reflection on warm path.
/// First execution compiles the template, subsequent executions are pure delegate calls.
/// </summary>
internal sealed class CompiledSqlTemplate
{
    private readonly CompiledParameterAccessor[] _accessors;

    public CompiledSqlTemplate(string sql, ParameterBinding[] bindings)
    {
        Sql = sql;
        ParameterBindings = bindings;
        
        // Compile field accessors to delegates for fast warm-path execution
        _accessors = new CompiledParameterAccessor[bindings.Length];
        for (int i = 0; i < bindings.Length; i++)
        {
            _accessors[i] = new CompiledParameterAccessor(bindings[i]);
        }
    }

    public string Sql { get; }
    public ParameterBinding[] ParameterBindings { get; }
    
    /// <summary>
    /// Build DynamicParameters using compiled delegates - no reflection.
    /// </summary>
    public DynamicParameters BuildParameters(object queryInstance)
    {
        var dp = new DynamicParameters();
        
        foreach (var accessor in _accessors)
        {
            var value = accessor.GetValue(queryInstance);
            dp.Add(
                accessor.ParameterName,
                value,
                accessor.DbType,
                System.Data.ParameterDirection.Input,
                accessor.Size);
        }
        
        return dp;
    }
}

/// <summary>
/// Compiled accessor for a single parameter - uses delegate instead of reflection.
/// </summary>
internal sealed class CompiledParameterAccessor
{
    private readonly Func<object, object?> _getter;

    public CompiledParameterAccessor(ParameterBinding binding)
    {
        ParameterName = binding.ParameterName;
        DbType = binding.DbType;
        Size = binding.Size;
        
        // Compile a delegate: (object instance) => ((QueryType)instance).field
        _getter = CompileGetter(binding.SourceField);
    }

    public string ParameterName { get; }
    public System.Data.DbType DbType { get; }
    public int Size { get; }

    public object? GetValue(object instance) => _getter(instance);

    private static Func<object, object?> CompileGetter(FieldInfo field)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var castInstance = Expression.Convert(instanceParam, field.DeclaringType!);
        var fieldAccess = Expression.Field(castInstance, field);
        var boxedResult = Expression.Convert(fieldAccess, typeof(object));
        
        return Expression.Lambda<Func<object, object?>>(boxedResult, instanceParam).Compile();
    }
}

/// <summary>
/// Binds a SQL parameter to a field on the query class.
/// </summary>
internal sealed class ParameterBinding
{
    public ParameterBinding(
        string parameterName,
        FieldInfo sourceField,
        System.Data.DbType dbType,
        int size,
        byte precision,
        byte scale)
    {
        ParameterName = parameterName;
        SourceField = sourceField;
        DbType = dbType;
        Size = size;
        Precision = precision;
        Scale = scale;
    }

    public string ParameterName { get; }
    public FieldInfo SourceField { get; }
    public System.Data.DbType DbType { get; }
    public int Size { get; }
    public byte Precision { get; }
    public byte Scale { get; }
}

/// <summary>
/// Utility for extracting field names from EF Core parameter names.
/// EF Core typically names parameters like: @__fieldName_0, @__p_0, @__customerId_1
/// 
/// Uses efficient char scanning - no regex.
/// </summary>
internal static class ParameterNameAnalyzer
{
    /// <summary>
    /// Try to extract the field/variable name from an EF Core parameter name.
    /// Returns null if the pattern doesn't match.
    /// 
    /// Pattern: @?__<name>_<digits>
    /// Examples: @__id_0 → "id", @__categoryId_1 → "categoryId", __p_0 → null (generic)
    /// </summary>
    public static string? ExtractFieldName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return null;

        var span = parameterName.AsSpan();
        var pos = 0;

        // Skip optional @ prefix
        if (pos < span.Length && span[pos] == '@')
            pos++;

        // Must start with __
        if (pos + 2 > span.Length || span[pos] != '_' || span[pos + 1] != '_')
            return null;
        pos += 2;

        // Find the name part (ends at last underscore before digits)
        var nameStart = pos;
        var lastUnderscoreBeforeDigits = -1;

        // Scan to find the pattern: name_digits
        for (int i = pos; i < span.Length; i++)
        {
            if (span[i] == '_')
            {
                // Check if everything after this underscore is digits
                var allDigitsAfter = true;
                for (int j = i + 1; j < span.Length; j++)
                {
                    if (!char.IsDigit(span[j]))
                    {
                        allDigitsAfter = false;
                        break;
                    }
                }
                
                if (allDigitsAfter && i + 1 < span.Length)
                {
                    lastUnderscoreBeforeDigits = i;
                    break;
                }
            }
        }

        if (lastUnderscoreBeforeDigits <= nameStart)
            return null;

        var name = parameterName.Substring(nameStart, lastUnderscoreBeforeDigits - nameStart);

        // "p" is a generic placeholder, not useful for matching
        if (name == "p")
            return null;

        return name;
    }
}
