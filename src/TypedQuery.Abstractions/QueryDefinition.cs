using System.Data.Common;

namespace TypedQuery.Abstractions;

/// <summary>
/// Represents a compiled query with SQL and parameters ready for execution.
/// </summary>
public sealed class QueryDefinition
{
    /// <summary>
    /// Creates a new QueryDefinition.
    /// </summary>
    public QueryDefinition(string sql, IReadOnlyList<DbParameter> parameters)
    {
        Sql = sql ?? throw new ArgumentNullException(nameof(sql));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>
    /// The SQL query text to execute.
    /// </summary>
    public string Sql { get; init; }

    /// <summary>
    /// The database parameters for the query.
    /// </summary>
    public IReadOnlyList<DbParameter> Parameters { get; init; }
}
