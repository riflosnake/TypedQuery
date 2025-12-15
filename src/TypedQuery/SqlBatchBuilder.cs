using TypedQuery.Abstractions;
using TypedQuery.Internal;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace TypedQuery;

/// <summary>
/// Builds a SQL batch from multiple query registrations.
/// </summary>
internal static class SqlBatchBuilder
{
    /// <summary>
    /// Builds a SQL batch from the registered queries in the builder.
    /// </summary>
    /// <param name="builder">The query builder containing registered queries</param>
    /// <param name="context">The build context</param>
    /// <returns>A SQL batch ready for execution</returns>
    public static SqlBatch Build(
        TypedQueryBuilder builder,
        QueryBuildContext context,
        DbProviderFactory factory)
    {
        var itemCount = builder.Items.Count;

        var sql = new StringBuilder(itemCount * 256);
        var allParameters = new List<DbParameter>(itemCount * 4);
        var resultTypes = new List<Type>(itemCount);
        var queryTypes = new List<Type>(itemCount);

        int queryIndex = 0;
        foreach (var item in builder.Items)
        {
            var definition = BuildMethodCache.InvokeBuild(item.QueryInstance, context);

            if (definition.AnonymousParameters != null)
            {
                RewriteAnonymousParametersInPlace(
                    sql, definition.Sql, definition.AnonymousParameters!, queryIndex,
                    allParameters, factory);
            }
            else
            {
                RewriteParametersInPlace(
                    sql, definition.Sql, definition.Parameters, queryIndex,
                    allParameters, factory);
            }

            sql.Append(";\n");

            resultTypes.Add(item.ResultType);
            queryTypes.Add(item.QueryType);

            queryIndex++;
        }

        return new SqlBatch(sql.ToString(), allParameters, resultTypes, queryTypes);
    }

    /// <summary>
    /// Rewrites parameter names for anonymous object parameters.
    /// Converts anonymous object properties to provider-specific DbParameters with unique names.
    /// </summary>
    private static void RewriteAnonymousParametersInPlace(
        StringBuilder sql,
        string querySql,
        object anonymousParams,
        int queryIndex,
        List<DbParameter> allParameters,
        DbProviderFactory factory)
    {
        var paramDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var prop in anonymousParams.GetType().GetProperties())
        {
            paramDict[prop.Name] = prop.GetValue(anonymousParams);
        }

        if (paramDict.Count == 0)
        {
            sql.Append(querySql);
            return;
        }

        var modifiedSql = querySql;

        foreach (var kvp in paramDict)
        {
            var originalName = kvp.Key;
            var value = kvp.Value;

            var paramNameInSql = originalName.StartsWith('@') ? originalName : $"@{originalName}";
            var cleanName = originalName.TrimStart('@', ':', '?');
            var newName = $"@tql{queryIndex}_{cleanName}";

            modifiedSql = modifiedSql.Replace(paramNameInSql, newName);

            var p = factory.CreateParameter()
                ?? throw new InvalidOperationException("Failed to create DbParameter");

            p.ParameterName = newName;
            p.Value = value ?? DBNull.Value;

            allParameters.Add(p);
        }

        sql.Append(modifiedSql);
    }

    /// <summary>
    /// Rewrites parameter names to make them unique across multiple queries.
    /// Appends directly to the shared StringBuilder to reduce allocations.
    /// </summary>
    private static void RewriteParametersInPlace(
        StringBuilder sql,
        string querySql,
        IReadOnlyList<DbParameter> parameters,
        int queryIndex,
        List<DbParameter> allParameters,
        DbProviderFactory factory)
    {
        if (parameters.Count == 0)
        {
            sql.Append(querySql);
            return;
        }

        var modifiedSql = querySql;
        
        foreach (var source in parameters)
        {
            var originalName = source.ParameterName;
            
            var startIndex = 0;
            while (startIndex < originalName.Length && IsParameterPrefix(originalName[startIndex]))
            {
                startIndex++;
            }
            
            var cleanName = startIndex > 0 ? originalName.Substring(startIndex) : originalName;
            
            var newName = string.Concat("@tql", queryIndex.ToString(), "_", cleanName);

            modifiedSql = modifiedSql.Replace(originalName, newName);

            var p = factory.CreateParameter()
                ?? throw new InvalidOperationException("Failed to create DbParameter");

            p.ParameterName = newName;
            p.Value = source.Value ?? DBNull.Value;
            p.DbType = source.DbType;
            p.Direction = source.Direction;
            p.Size = source.Size;
            p.Precision = source.Precision;
            p.Scale = source.Scale;
            p.IsNullable = source.IsNullable;

            allParameters.Add(p);
        }

        sql.Append(modifiedSql);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsParameterPrefix(char c) => c == '@' || c == ':' || c == '?';
}
