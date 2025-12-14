using TypedQuery.Abstractions;

namespace TypedQuery;

/// <summary>
/// Fluent executor for composing and executing TypedQuery batches.
/// Provides the simplified API: .Add(new Query(...)).ExecuteAsync()
/// </summary>
/// <typeparam name="TContext">The execution context type (DbContext or DbConnection)</typeparam>
public sealed class TypedQueryExecutor<TContext>
{
    private readonly TContext _context;
    private readonly Func<TContext, TypedQueryBuilder, CancellationToken, Task<TypedQueryResult>> _executor;
    private readonly TypedQueryBuilder _builder = new();

    /// <summary>
    /// Creates a new TypedQueryExecutor.
    /// </summary>
    /// <param name="context">The execution context (DbContext or DbConnection)</param>
    /// <param name="executor">The execution function that runs the batch</param>
    internal TypedQueryExecutor(
        TContext context,
        Func<TContext, TypedQueryBuilder, CancellationToken, Task<TypedQueryResult>> executor)
    {
        _context = context;
        _executor = executor;
    }

    /// <summary>
    /// Adds a query to the execution batch.
    /// The query instance should be fully constructed with all parameters.
    /// </summary>
    /// <typeparam name="TResult">The result type (inferred from the query)</typeparam>
    /// <param name="query">The constructed query instance</param>
    /// <returns>This executor for method chaining</returns>
    public TypedQueryExecutor<TContext> Add<TResult>(ITypedQuery<TResult> query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query), "Query instance cannot be null.");
        }

        _builder.AddInstance(query);
        return this;
    }

    /// <summary>
    /// Executes all registered queries and returns the results.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A TypedQueryResult containing all query results</returns>
    public Task<TypedQueryResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _executor(_context, _builder, cancellationToken);
    }
}
