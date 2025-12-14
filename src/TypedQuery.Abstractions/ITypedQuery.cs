namespace TypedQuery.Abstractions;

/// <summary>
/// Core interface for all TypedQuery queries.
/// Every query is represented by a type that declares its result shape.
/// Parameters are passed through the query's constructor.
/// </summary>
/// <typeparam name="TResult">The type of the query result</typeparam>
public interface ITypedQuery<TResult>
{
    /// <summary>
    /// Builds the query definition with SQL and parameters.
    /// </summary>
    /// <param name="context">The query build context containing execution environment information</param>
    /// <returns>A query definition containing SQL and parameters</returns>
    QueryDefinition Build(QueryBuildContext context);
}
