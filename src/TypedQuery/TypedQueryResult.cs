using TypedQuery.Abstractions;

namespace TypedQuery;

/// <summary>
/// Contains the results of a TypedQuery batch execution.
/// Results can be retrieved by their result type or query type.
/// </summary>
public sealed class TypedQueryResult
{
    private readonly Dictionary<Type, object?> _resultsByResultType;
    private readonly Dictionary<Type, object?> _resultsByQueryType;

    private TypedQueryResult(int capacity)
    {
        _resultsByResultType = new Dictionary<Type, object?>(capacity);
        _resultsByQueryType = new Dictionary<Type, object?>(capacity);
    }

    /// <summary>
    /// Creates a TypedQueryResult from pre-computed data (for Dapper and custom scenarios).
    /// </summary>
    internal static TypedQueryResult FromData(
        IReadOnlyList<(Type ResultType, Type? QueryType, object? Data)> results)
    {
        var result = new TypedQueryResult(results.Count);

        foreach (var (resultType, queryType, data) in results)
        {
            result._resultsByResultType[resultType] = data;
            
            if (queryType != null)
            {
                result._resultsByQueryType[queryType] = data;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all results as an enumerable sequence for the specified result type.
    /// This is the recommended method for queries that return multiple items.
    /// Results are buffered by Dapper but not materialized to a list.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>An enumerable sequence of results</returns>
    public IEnumerable<T> GetAll<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToEnumerable<T>(data);
        }

        return Enumerable.Empty<T>();
    }

    /// <summary>
    /// Gets all results as a materialized list for the specified result type.
    /// Use this when you need a list specifically, otherwise prefer GetAll().
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>A read-only list of results</returns>
    public IReadOnlyList<T> GetList<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToList<T>(data);
        }

        return [];
    }

    /// <summary>
    /// Gets exactly one result. Throws if the result set is empty or contains more than one element.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The single result</returns>
    /// <exception cref="InvalidOperationException">The result set is empty or contains more than one element</exception>
    public T GetSingle<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToEnumerable<T>(data).Single();
        }

        throw new InvalidOperationException(
            $"No result found for type {typeof(T).Name}. Use GetSingleOrDefault<T>() if the result might be empty.");
    }

    /// <summary>
    /// Gets exactly one result, or default if the result set is empty.
    /// Throws if the result set contains more than one element.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The single result, or default if empty</returns>
    /// <exception cref="InvalidOperationException">The result set contains more than one element</exception>
    public T? GetSingleOrDefault<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToEnumerable<T>(data).SingleOrDefault();
        }

        return default;
    }

    /// <summary>
    /// Gets the first result. Throws if the result set is empty.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The first result</returns>
    /// <exception cref="InvalidOperationException">The result set is empty</exception>
    public T GetFirst<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToEnumerable<T>(data).First();
        }

        throw new InvalidOperationException(
            $"No result found for type {typeof(T).Name}. Use GetFirstOrDefault<T>() if the result might be empty.");
    }

    /// <summary>
    /// Gets the first result, or default if the result set is empty.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The first result, or default if empty</returns>
    public T? GetFirstOrDefault<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return ConvertToEnumerable<T>(data).FirstOrDefault();
        }

        return default;
    }

    /// <summary>
    /// Gets a single result (first or default) for the specified result type.
    /// </summary>
    /// <typeparam name="T">The result type to retrieve</typeparam>
    /// <returns>The first result of type T, or default if not found or empty</returns>
    [Obsolete("Use GetFirstOrDefault<T>() for explicit semantics. This method will be removed in a future version.")]
    public T? Get<T>() => GetFirstOrDefault<T>();

    /// <summary>
    /// Converts stored data to an enumerable sequence.
    /// </summary>
    private static IEnumerable<T> ConvertToEnumerable<T>(object? data)
    {
        if (data == null)
            return Enumerable.Empty<T>();

        if (data is IEnumerable<T> enumerable)
        {
            return enumerable;
        }

        if (data is T single)
        {
            return [single];
        }

        return Enumerable.Empty<T>();
    }

    /// <summary>
    /// Converts the stored data to a list.
    /// </summary>
    private static IReadOnlyList<T> ConvertToList<T>(object? data)
    {
        if (data == null)
            return [];

        if (data is IReadOnlyList<T> readOnlyList)
        {
            return readOnlyList;
        }

        if (data is List<T> list)
        {
            return list;
        }

        if (data is IEnumerable<T> enumerable)
        {
            return enumerable.ToList();
        }

        if (data is T single)
        {
            return [single];
        }

        return [];
    }

    /// <summary>
    /// Gets the result for the specified query type (advanced API).
    /// </summary>
    /// <typeparam name="TQuery">The query type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <returns>The result of type TResult</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no result exists for the specified type</exception>
    public TResult? GetByQuery<TQuery, TResult>() where TQuery : notnull, ITypedQuery<TResult>
    {
        if (_resultsByQueryType.TryGetValue(typeof(TQuery), out var queryResult))
        {
            return (TResult?)queryResult;
        }
        
        throw new KeyNotFoundException(
            $"No result found for type {typeof(TQuery).Name}. " +
            "Ensure you added a query for this result type.");
    }
}
