using System.Data.Common;

namespace TypedQuery.Abstractions;

/// <summary>
/// Represents a compiled query with SQL and parameters ready for execution.
/// Parameters should be passed as anonymous objects (Dapper-style) for maximum compatibility.
/// </summary>
public sealed class QueryDefinition
{
    /// <summary>
    /// Creates a new QueryDefinition with SQL and optional parameters.
    /// </summary>
    /// <param name="sql">The SQL query text</param>
    /// <param name="parameters">Anonymous object or dictionary containing parameter values (e.g., new { id = 5, name = "John" })</param>
    public QueryDefinition(string sql, object? parameters = null)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters;
    }

    /// <summary>
    /// The SQL query text to execute.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// The parameters for the query. Can be an anonymous object, dictionary, or DynamicParameters.
    /// </summary>
    public object? Parameters { get; }
}
