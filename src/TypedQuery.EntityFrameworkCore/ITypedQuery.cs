using TypedQuery.Abstractions;
using TypedQuery.EntityFrameworkCore.Interceptor;
using Microsoft.EntityFrameworkCore;

namespace TypedQuery.EntityFrameworkCore;

/// <summary>
/// Specialized interface for EF Core queries that work with a specific DbContext type.
/// Parameters are passed through the query's constructor.
/// The Build() method is implemented automatically - you only need to implement Query().
/// </summary>
/// <typeparam name="TDbContext">The specific DbContext type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface ITypedQuery<TDbContext, TResult> : ITypedQuery<TResult>
    where TDbContext : DbContext
{
    /// <summary>
    /// Builds an EF Core LINQ query.
    /// </summary>
    /// <param name="db">The typed DbContext to query against</param>
    /// <returns>An IQueryable that will be converted to SQL</returns>
    IQueryable<TResult> Query(TDbContext db);

    /// <summary>
    /// Default implementation that converts the LINQ query to SQL with proper parameters.
    /// Uses TypedQueryInterceptor to capture SQL and parameters without executing the query.
    /// </summary>
    /// <param name="context">The build context</param>
    /// <returns>A QueryDefinition with SQL and parameters</returns>
    /// <exception cref="InvalidOperationException">Thrown if DbContext is not available or wrong type</exception>
    QueryDefinition ITypedQuery<TResult>.Build(QueryBuildContext context)
    {
        if (context.DbContext is not TDbContext dbContext)
        {
            var providedType = context.DbContext?.GetType().Name ?? "null";
            throw new InvalidOperationException(
                $"{GetType().Name} is an EF Core query that requires {typeof(TDbContext).Name} for execution, but got {providedType}. " +
                "Use dbContext.ToTypedQuery() with the correct DbContext type.");
        }

        var queryId = TypedQueryInterceptor.GenerateQueryId();
        var tag = string.Concat(TypedQueryInterceptor.CacheKeyPrefix, queryId);

        var query = Query(dbContext).TagWith(tag);

        try
        {
            _ = query.ToList();
        }
        catch
        {
        }

        var captured = TypedQueryInterceptor.PullCapturedQuery(queryId) ?? throw new InvalidOperationException(
                $"Failed to capture SQL for query {GetType().Name}. " +
                "Make sure TypedQueryInterceptor is registered with UseTypedQuery().");

        return new QueryDefinition(captured.Sql, captured.Parameters);
    }
}
