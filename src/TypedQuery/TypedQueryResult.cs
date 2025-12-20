using TypedQuery.Abstractions;

namespace TypedQuery;

/// <summary>
/// Contains the results of a TypedQuery batch execution.
/// Results are stored in order and can be retrieved sequentially using Next() or by index.
/// 
/// Usage patterns:
/// 1. Sequential access: Use Next&lt;T&gt;() to get each result set in order
/// 2. By index: Use GetByIndex&lt;T&gt;(index) to get a specific result set
/// 3. By type (first match): Use GetList&lt;T&gt;() to get the first result set of that type
/// </summary>
public sealed class TypedQueryResult
{
    private readonly List<ResultSet> _orderedResults;
    private readonly Dictionary<Type, List<int>> _indexesByResultType;
    private readonly Dictionary<Type, int> _indexByQueryType;
    private int _currentIndex;

    private TypedQueryResult(int capacity)
    {
        _orderedResults = new List<ResultSet>(capacity);
        _indexesByResultType = new Dictionary<Type, List<int>>(capacity);
        _indexByQueryType = new Dictionary<Type, int>(capacity);
        _currentIndex = 0;
    }

    /// <summary>
    /// Creates a TypedQueryResult from pre-computed data.
    /// </summary>
    internal static TypedQueryResult FromData(
        IReadOnlyList<(Type ResultType, Type? QueryType, object? Data)> results)
    {
        var result = new TypedQueryResult(results.Count);

        for (int i = 0; i < results.Count; i++)
        {
            var (resultType, queryType, data) = results[i];
            
            result._orderedResults.Add(new ResultSet(resultType, queryType, data));
            
            // Track indexes by result type (supports multiple queries with same result type)
            if (!result._indexesByResultType.TryGetValue(resultType, out var indexes))
            {
                indexes = new List<int>();
                result._indexesByResultType[resultType] = indexes;
            }
            indexes.Add(i);
            
            // Track by query type (unique)
            if (queryType != null)
            {
                result._indexByQueryType[queryType] = i;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the number of result sets in this batch.
    /// </summary>
    public int Count => _orderedResults.Count;

    /// <summary>
    /// Gets the next result set in sequence.
    /// Call this repeatedly to iterate through all result sets in order.
    /// </summary>
    /// <typeparam name="T">The expected result type</typeparam>
    /// <returns>The next result set as an enumerable</returns>
    /// <exception cref="InvalidOperationException">No more result sets available</exception>
    /// <example>
    /// var result = await conn.ToTypedQuery()
    ///     .Add(new GetMakeByIdRaw(1))
    ///     .Add(new GetMakeByIdRaw(2))
    ///     .Add(new GetCountRaw())
    ///     .ExecuteAsync();
    /// 
    /// var firstMake = result.Next&lt;MakeDto&gt;().FirstOrDefault();
    /// var secondMake = result.Next&lt;MakeDto&gt;().FirstOrDefault();
    /// var count = result.Next&lt;CountResult&gt;().First();
    /// </example>
    public IEnumerable<T> Next<T>()
    {
        if (_currentIndex >= _orderedResults.Count)
        {
            throw new InvalidOperationException(
                $"No more result sets available. Already retrieved {_currentIndex} of {_orderedResults.Count} result sets.");
        }

        var data = _orderedResults[_currentIndex].Data;
        _currentIndex++;
        return ConvertToEnumerable<T>(data);
    }

    /// <summary>
    /// Tries to get the next result set in sequence.
    /// Returns false if no more result sets are available.
    /// </summary>
    /// <typeparam name="T">The expected result type</typeparam>
    /// <param name="results">The next result set, or empty if none available</param>
    /// <returns>True if a result set was retrieved, false if no more available</returns>
    public bool TryNext<T>(out IEnumerable<T> results)
    {
        if (_currentIndex >= _orderedResults.Count)
        {
            results = Enumerable.Empty<T>();
            return false;
        }

        var data = _orderedResults[_currentIndex].Data;
        _currentIndex++;
        results = ConvertToEnumerable<T>(data);
        return true;
    }

    /// <summary>
    /// Resets the sequential iterator to the beginning.
    /// </summary>
    public void Reset()
    {
        _currentIndex = 0;
    }

    /// <summary>
    /// Gets results at a specific index in the batch.
    /// </summary>
    /// <typeparam name="T">The expected result type</typeparam>
    /// <param name="index">The zero-based index of the query in the batch</param>
    /// <returns>The results from the query at that index</returns>
    public IEnumerable<T> GetByIndex<T>(int index)
    {
        if (index < 0 || index >= _orderedResults.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), 
                $"Index {index} is out of range. Batch contains {_orderedResults.Count} result sets.");
        }

        return ConvertToEnumerable<T>(_orderedResults[index].Data);
    }

    /// <summary>
    /// Gets the FIRST result set for the specified result type as a list.
    /// If you have multiple queries returning the same type, use Next() or GetByIndex() instead.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The results from the first matching query</returns>
    public IEnumerable<T> GetList<T>()
    {
        if (_indexesByResultType.TryGetValue(typeof(T), out var indexes) && indexes.Count > 0)
        {
            return ConvertToEnumerable<T>(_orderedResults[indexes[0]].Data);
        }

        return Enumerable.Empty<T>();
    }

    /// <summary>
    /// Gets exactly one result from the first result set of the specified type.
    /// Throws if the result set is empty or contains more than one element.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The single result</returns>
    /// <exception cref="InvalidOperationException">The result set is empty or contains more than one element</exception>
    public T GetSingle<T>()
    {
        if (_indexesByResultType.TryGetValue(typeof(T), out var indexes) && indexes.Count > 0)
        {
            return ConvertToEnumerable<T>(_orderedResults[indexes[0]].Data).Single();
        }

        throw new InvalidOperationException(
            $"No result found for type {typeof(T).Name}. Use GetSingleOrDefault<T>() if the result might be empty.");
    }

    /// <summary>
    /// Gets exactly one result from the first result set of the specified type, or default if empty.
    /// Throws if the result set contains more than one element.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The single result, or default if empty</returns>
    /// <exception cref="InvalidOperationException">The result set contains more than one element</exception>
    public T? GetSingleOrDefault<T>()
    {
        if (_indexesByResultType.TryGetValue(typeof(T), out var indexes) && indexes.Count > 0)
        {
            return ConvertToEnumerable<T>(_orderedResults[indexes[0]].Data).SingleOrDefault();
        }

        return default;
    }

    /// <summary>
    /// Gets the first result from the first result set of the specified type.
    /// Throws if the result set is empty.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The first result</returns>
    /// <exception cref="InvalidOperationException">The result set is empty</exception>
    public T GetFirst<T>()
    {
        if (_indexesByResultType.TryGetValue(typeof(T), out var indexes) && indexes.Count > 0)
        {
            return ConvertToEnumerable<T>(_orderedResults[indexes[0]].Data).First();
        }

        throw new InvalidOperationException(
            $"No result found for type {typeof(T).Name}. Use GetFirstOrDefault<T>() if the result might be empty.");
    }

    /// <summary>
    /// Gets the first result from the first result set of the specified type, or default if empty.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <returns>The first result, or default if empty</returns>
    public T? GetFirstOrDefault<T>()
    {
        if (_indexesByResultType.TryGetValue(typeof(T), out var indexes) && indexes.Count > 0)
        {
            return ConvertToEnumerable<T>(_orderedResults[indexes[0]].Data).FirstOrDefault();
        }

        return default;
    }

    /// <summary>
    /// Gets results by query type. Useful when you have unique query classes.
    /// </summary>
    /// <typeparam name="TQuery">The query type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <returns>The results from the query of that type</returns>
    public IEnumerable<TResult> GetByQuery<TQuery, TResult>() where TQuery : ITypedQuery<TResult>
    {
        if (_indexByQueryType.TryGetValue(typeof(TQuery), out var index))
        {
            return ConvertToEnumerable<TResult>(_orderedResults[index].Data);
        }

        return Enumerable.Empty<TResult>();
    }

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
    /// Internal record for storing result sets.
    /// </summary>
    private sealed record ResultSet(Type ResultType, Type? QueryType, object? Data);
}
