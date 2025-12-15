using System.Data.Common;

namespace TypedQuery.Abstractions;

/// <summary>
/// Represents a compiled query with SQL and parameters ready for execution.
/// </summary>
public sealed class QueryDefinition
{
    /// <summary>
    /// Creates a new QueryDefinition with DbParameter instances.
    /// </summary>
    public QueryDefinition(string sql, IReadOnlyList<DbParameter> parameters)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        AnonymousParameters = null;
    }

    /// <summary>
    /// Creates a new QueryDefinition with anonymous object parameters (Dapper-style).
    /// This is the recommended constructor for most use cases as it doesn't require provider-specific parameter types.
    /// </summary>
    /// <param name="sql">The SQL query text</param>
    /// <param name="parameters">Anonymous object containing parameter values (e.g., new { id = 5, name = "John" })</param>
    public QueryDefinition(string sql, object? parameters = null)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        AnonymousParameters = parameters;
        Parameters = Array.Empty<DbParameter>();
    }

    /// <summary>
    /// The SQL query text to execute.
    /// </summary>
    public string Sql { get; init; }

    /// <summary>
    /// The database parameters for the query (legacy, for backward compatibility).
    /// Use AnonymousParameters for new code.
    /// </summary>
    public IReadOnlyList<DbParameter> Parameters { get; init; }

    /// <summary>
    /// Anonymous object containing parameter values (Dapper-style).
    /// If this is set, Parameters collection is ignored.
    /// </summary>
    public object? AnonymousParameters { get; init; }
}
