using TypedQuery.Abstractions;
using TypedQuery.Internal;
using Dapper;
using System.Text;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypedQuery;

/// <summary>
/// Builds a SQL batch from multiple query definitions.
/// Handles parameter uniqueness across batched queries.
/// 
/// 
/// Supported parameter types:
/// - Anonymous objects: new { id = 1, name = "test" }
/// - DynamicParameters: Dapper's dynamic parameter bag
/// - Dictionary&lt;string, object?&gt;: key-value pairs
/// - IDictionary: legacy dictionary implementations
/// - Values containing SqlTypes (SqlInt64, etc.) are automatically unwrapped
/// - Values containing DbParameter/IDbDataParameter are automatically unwrapped
/// 
/// NOT supported (will throw NotSupportedException):
/// - Tuples: (int id, string name) or Tuple&lt;int, string&gt;
/// - Arrays/Lists as parameter containers
/// - Single primitive values (int, string, etc.)
/// - IEnumerable (except dictionaries)
/// </summary>
internal static class SqlBatchBuilder
{
    // Cache for anonymous type property accessors (eliminates repeated reflection)
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>
    /// Builds a SQL batch from the registered queries in the builder.
    /// </summary>
    public static SqlBatch Build(TypedQueryBuilder builder, QueryBuildContext context)
    {
        var itemCount = builder.Items.Count;
        var sqlBuilder = new StringBuilder(itemCount * 256);
        var mergedParameters = new DynamicParameters();
        var resultTypes = new List<Type>(itemCount);
        var queryTypes = new List<Type>(itemCount);

        for (int queryIndex = 0; queryIndex < itemCount; queryIndex++)
        {
            var item = builder.Items[queryIndex];
            var definition = BuildMethodCache.InvokeBuild(item.QueryInstance, context);

            // Process this query's SQL and parameters
            ProcessQuery(
                definition.Sql,
                definition.Parameters,
                queryIndex,
                sqlBuilder,
                mergedParameters);

            sqlBuilder.Append(";\n");

            resultTypes.Add(item.ResultType);
            queryTypes.Add(item.QueryType);
        }

        return new SqlBatch(sqlBuilder.ToString(), mergedParameters, resultTypes, queryTypes);
    }

    /// <summary>
    /// Processes a single query: rewrites parameter names to be unique and adds to merged parameters.
    /// Uses efficient char-by-char scanning instead of regex.
    /// </summary>
    private static void ProcessQuery(
        string sql,
        object? parameters,
        int queryIndex,
        StringBuilder output,
        DynamicParameters mergedParameters)
    {
        if (parameters == null)
        {
            output.Append(sql);
            return;
        }

        // Extract parameter names and values from the object
        var paramDict = ExtractParameters(parameters);

        if (paramDict.Count == 0)
        {
            output.Append(sql);
            return;
        }

        // Build prefix once
        var prefix = $"p{queryIndex}_";

        // Add all parameters with prefixed names, unwrapping special types
        foreach (var kvp in paramDict)
        {
            var value = UnwrapParameterValue(kvp.Value);
            mergedParameters.Add(prefix + kvp.Key, value);
        }

        // Rewrite SQL parameter references using efficient char scanning
        RewriteParameterReferences(sql, paramDict, prefix, output);
    }

    /// <summary>
    /// Unwraps special parameter types like SqlTypes, DbParameter, etc. to their underlying values.
    /// This ensures Dapper can handle the values correctly.
    /// </summary>
    private static object? UnwrapParameterValue(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var type = value.GetType();

        // Handle System.Data.SqlTypes (SqlInt64, SqlString, SqlInt32, etc.)
        if (type.Namespace == "System.Data.SqlTypes")
        {
            // Check for IsNull property first
            var isNullProp = type.GetProperty("IsNull");
            if (isNullProp != null)
            {
                var isNull = (bool?)isNullProp.GetValue(value);
                if (isNull == true)
                    return null;
            }

            // Get the Value property to extract the underlying value
            var valueProp = type.GetProperty("Value");
            if (valueProp != null)
            {
                return valueProp.GetValue(value);
            }
        }

        // Handle DbParameter - extract the Value
        if (value is DbParameter dbParam)
        {
            return dbParam.Value == DBNull.Value ? null : dbParam.Value;
        }

        // Handle IDbDataParameter
        if (value is IDbDataParameter dataParam)
        {
            return dataParam.Value == DBNull.Value ? null : dataParam.Value;
        }

        return value;
    }

    /// <summary>
    /// Rewrites @paramName references in SQL to @p{index}_paramName.
    /// Uses efficient char-by-char scanning - no regex.
    /// </summary>
    private static void RewriteParameterReferences(
        string sql,
        Dictionary<string, object?> paramDict,
        string prefix,
        StringBuilder output)
    {
        var length = sql.Length;
        var i = 0;

        while (i < length)
        {
            var c = sql[i];

            if (c == '@')
            {
                // Found potential parameter, extract the name
                var start = i + 1;
                var end = start;

                // Scan for valid identifier characters: letters, digits, underscore
                while (end < length && IsIdentifierChar(sql[end]))
                {
                    end++;
                }

                if (end > start)
                {
                    var paramName = sql.Substring(start, end - start);

                    // Check if this is one of our parameters (case-insensitive)
                    if (paramDict.ContainsKey(paramName))
                    {
                        // Write the rewritten parameter
                        output.Append('@');
                        output.Append(prefix);
                        output.Append(paramName);
                        i = end;
                        continue;
                    }
                }

                // Not our parameter, write as-is
                output.Append(c);
                i++;
            }
            else
            {
                output.Append(c);
                i++;
            }
        }
    }

    /// <summary>
    /// Checks if a character is valid in a SQL parameter identifier.
    /// </summary>
    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    /// Extracts parameters from an anonymous object, dictionary, or DynamicParameters.
    /// Uses cached PropertyInfo[] for anonymous types to minimize reflection overhead.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the parameter type is not supported (tuples, arrays, primitives, etc.)
    /// </exception>
    private static Dictionary<string, object?> ExtractParameters(object parameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var type = parameters.GetType();

        // Handle DynamicParameters first (most common for Dapper users)
        if (parameters is DynamicParameters dp)
        {
            foreach (var name in dp.ParameterNames)
            {
                result[name] = dp.Get<object?>(name);
            }
            return result;
        }

        // Handle generic dictionary
        if (parameters is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        // Handle legacy dictionary
        if (parameters is System.Collections.IDictionary legacyDict)
        {
            foreach (System.Collections.DictionaryEntry entry in legacyDict)
            {
                if (entry.Key is string key)
                {
                    result[key] = entry.Value;
                }
            }
            return result;
        }

        // Reject unsupported types with helpful error messages
        ValidateParameterType(type, parameters);

        // Anonymous object or regular class - use cached properties
        var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties());
        
        foreach (var prop in properties)
        {
            result[prop.Name] = prop.GetValue(parameters);
        }

        return result;
    }

    /// <summary>
    /// Validates that the parameter type is supported and throws helpful exceptions if not.
    /// </summary>
    private static void ValidateParameterType(Type type, object parameters)
    {
        // Check for tuples (ValueTuple and Tuple)
        if (type.FullName?.StartsWith("System.ValueTuple") == true ||
            type.FullName?.StartsWith("System.Tuple") == true)
        {
            throw new NotSupportedException(
                $"Tuples are not supported as query parameters. " +
                $"Use an anonymous object instead: new {{ id = tuple.Item1, name = tuple.Item2 }}");
        }

        // Check for primitive types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || type == typeof(Guid))
        {
            throw new NotSupportedException(
                $"Single primitive values ({type.Name}) are not supported as query parameters. " +
                $"Use an anonymous object instead: new {{ paramName = value }}");
        }

        // Check for arrays and collections (except dictionaries, handled above)
        if (type.IsArray)
        {
            throw new NotSupportedException(
                $"Arrays are not supported as query parameter containers. " +
                $"Use an anonymous object instead: new {{ id = values[0], name = values[1] }}");
        }

        // Check for IEnumerable (lists, collections, etc.) - but not string which is IEnumerable<char>
        if (type != typeof(string) && 
            typeof(System.Collections.IEnumerable).IsAssignableFrom(type) &&
            !typeof(System.Collections.IDictionary).IsAssignableFrom(type))
        {
            throw new NotSupportedException(
                $"Collections ({type.Name}) are not supported as query parameter containers. " +
                $"Use an anonymous object or Dictionary<string, object?> instead.");
        }

        // Warn about non-anonymous classes (they'll work, but might be unintended)
        // Anonymous types are compiler-generated and have specific naming patterns
        bool isAnonymousType = Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) &&
                              type.Name.Contains("AnonymousType");
        
        if (!isAnonymousType && type.IsClass && !type.IsAbstract)
        {
            // This is a regular class - it will work but log a debug note
            // We allow it because it's valid usage, just less common
        }
    }

    /// <summary>
    /// Clears the property cache. Useful for testing.
    /// </summary>
    internal static void ClearCache()
    {
        PropertyCache.Clear();
    }
}
