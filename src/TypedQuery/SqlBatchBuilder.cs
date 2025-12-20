using TypedQuery.Abstractions;
using TypedQuery.Internal;
using Dapper;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;

namespace TypedQuery;

/// <summary>
/// Builds a SQL batch from multiple query definitions.
/// Handles parameter uniqueness across batched queries.
/// 
/// Performance optimizations:
/// - No regex - uses efficient char-by-char scanning
/// - Caches PropertyInfo[] per anonymous type for parameter extraction
/// - Pre-allocates StringBuilder capacity
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

        // Add all parameters with prefixed names
        foreach (var kvp in paramDict)
        {
            mergedParameters.Add(prefix + kvp.Key, kvp.Value);
        }

        // Rewrite SQL parameter references using efficient char scanning
        RewriteParameterReferences(sql, paramDict, prefix, output);
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
    private static Dictionary<string, object?> ExtractParameters(object parameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (parameters is DynamicParameters dp)
        {
            foreach (var name in dp.ParameterNames)
            {
                result[name] = dp.Get<object?>(name);
            }
        }
        else if (parameters is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        else if (parameters is System.Collections.IDictionary legacyDict)
        {
            foreach (System.Collections.DictionaryEntry entry in legacyDict)
            {
                if (entry.Key is string key)
                {
                    result[key] = entry.Value;
                }
            }
        }
        else
        {
            // Anonymous object - use cached properties
            var type = parameters.GetType();
            var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties());
            
            foreach (var prop in properties)
            {
                result[prop.Name] = prop.GetValue(parameters);
            }
        }

        return result;
    }

    /// <summary>
    /// Clears the property cache. Useful for testing.
    /// </summary>
    internal static void ClearCache()
    {
        PropertyCache.Clear();
    }
}
