using TypedQuery.Abstractions;

namespace TypedQuery;

/// <summary>
/// Builder for composing multiple TypedQuery queries into a single batch execution.
/// The order of Add() calls determines the order of result sets.
/// </summary>
internal sealed class TypedQueryBuilder
{
    private readonly List<TypedQueryRegistration> _items = [];

    /// <summary>
    /// Adds a query instance to the batch (new simplified API).
    /// </summary>
    /// <typeparam name="TResult">The result type of the query</typeparam>
    /// <param name="query">The constructed query instance</param>
    /// <returns>This builder for method chaining</returns>
    internal TypedQueryBuilder AddInstance<TResult>(ITypedQuery<TResult> query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query), "Query instance cannot be null.");
        }

        _items.Add(new TypedQueryRegistration(
            query.GetType(),
            typeof(TResult),
            query));

        return this;
    }

    /// <summary>
    /// Gets the registered query items.
    /// </summary>
    internal IReadOnlyList<TypedQueryRegistration> Items => _items;
}
