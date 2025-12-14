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
    /// Gets a single result (first or default) for the specified result type.
    /// Useful for queries that should return a single item.
    /// </summary>
    /// <typeparam name="T">The result type to retrieve</typeparam>
    /// <returns>The first result of type T, or default if not found or empty</returns>
    public T? Get<T>()
    {
        if (_resultsByResultType.TryGetValue(typeof(T), out var data))
        {
            return GetFirstOrDefault<T>(data);
        }

        return default;
    }

    /// <summary>
    /// Gets a list of results for the specified result type.
    /// Useful for queries that return multiple items.
    /// </summary>
    /// <typeparam name="T">The item type in the list</typeparam>
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
    /// Gets the first item from the stored data, or default.
    /// </summary>
    private static T? GetFirstOrDefault<T>(object? data)
    {
        if (data == null)
            return default;

        if (data is IEnumerable<T> enumerable)
        {
            return enumerable.FirstOrDefault();
        }

        if (data is T single)
        {
            return single;
        }

        return default;
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
