using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

/// <summary>
/// Captured query from EF Core interception (used for immediate retrieval).
/// </summary>
internal sealed record CapturedQuery(string Sql, DbParameter[] Parameters);

/// <summary>
/// Compiled SQL template for Dapper execution mode.
/// 
/// Uses parameter name analysis to map EF Core parameters to query fields.
/// EF Core typically names parameters like @__fieldName_0 where fieldName
/// is derived from the captured variable name in the expression.
/// 
/// This eliminates value-based collision limitations.
/// </summary>
internal sealed class CompiledSqlTemplate
{
    public CompiledSqlTemplate(string sql, ParameterBinding[] bindings)
    {
        Sql = sql;
        ParameterBindings = bindings;
    }

    public string Sql { get; }
    public ParameterBinding[] ParameterBindings { get; }
    
    /// <summary>
    /// Build fresh DbParameters by reading current field values from query instance.
    /// </summary>
    public DbParameter[] BuildParameters(object queryInstance, DbProviderFactory factory)
    {
        var parameters = new DbParameter[ParameterBindings.Length];
        
        for (int i = 0; i < ParameterBindings.Length; i++)
        {
            var binding = ParameterBindings[i];
            var value = binding.SourceField.GetValue(queryInstance);
            
            var p = factory.CreateParameter() 
                ?? throw new InvalidOperationException("Failed to create DbParameter");
            
            p.ParameterName = binding.ParameterName;
            p.Value = value ?? DBNull.Value;
            p.DbType = binding.DbType;
            p.Size = binding.Size;
            p.Precision = binding.Precision;
            p.Scale = binding.Scale;
            
            parameters[i] = p;
        }
        
        return parameters;
    }
}

/// <summary>
/// Binds a SQL parameter to a field on the query class.
/// Uses parameter name analysis (not value matching) for reliable binding.
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
/// </summary>
internal static class ParameterNameAnalyzer
{
    // Pattern: @__<name>_<index> or @__p_<index>
    private static readonly Regex EfCoreParamPattern = new(@"^@?__(?<name>[a-zA-Z_][a-zA-Z0-9_]*)_\d+$", RegexOptions.Compiled);
    
    /// <summary>
    /// Try to extract the field/variable name from an EF Core parameter name.
    /// Returns null if the pattern doesn't match.
    /// </summary>
    public static string? ExtractFieldName(string parameterName)
    {
        var match = EfCoreParamPattern.Match(parameterName);
        if (!match.Success)
            return null;
        
        var name = match.Groups["name"].Value;
        
        // "p" is generic placeholder, not useful
        if (name == "p")
            return null;
        
        return name;
    }
}
