using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using TypedQuery.Abstractions;

namespace TypedQuery.Internal;

/// <summary>
/// Caches compiled delegates for invoking Build methods on query types.
/// Uses expression trees to create fast delegates instead of reflection invoke.
/// </summary>
internal static class BuildMethodCache
{
    private static readonly ConcurrentDictionary<Type, Func<object, QueryBuildContext, QueryDefinition>> _cache = new();

    /// <summary>
    /// Invokes the Build method on a query instance using a cached compiled delegate.
    /// Much faster than reflection-based Method.Invoke().
    /// </summary>
    public static QueryDefinition InvokeBuild(object query, QueryBuildContext context)
    {
        var queryType = query.GetType();

        var invoker = _cache.GetOrAdd(queryType, CreateInvoker);

        return invoker(query, context);
    }

    private static Func<object, QueryBuildContext, QueryDefinition> CreateInvoker(Type queryType)
    {
        var typedQueryInterface = queryType.GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(ITypedQuery<>));

        if (typedQueryInterface == null)
        {
            throw new InvalidOperationException(
                $"Query type {queryType.Name} does not implement ITypedQuery<TResult>.");
        }

        var buildMethod = typedQueryInterface.GetMethod("Build")
            ?? throw new InvalidOperationException(
                $"Could not find Build method on {typedQueryInterface.Name}.");

        // Create expression tree for fast invocation:
        // (object query, QueryBuildContext context) => ((ITypedQuery<T>)query).Build(context)
        
        var queryParam = Expression.Parameter(typeof(object), "query");
        var contextParam = Expression.Parameter(typeof(QueryBuildContext), "context");

        var castQuery = Expression.Convert(queryParam, typedQueryInterface);
        var callBuild = Expression.Call(castQuery, buildMethod, contextParam);

        var lambda = Expression.Lambda<Func<object, QueryBuildContext, QueryDefinition>>(
            callBuild, queryParam, contextParam);

        return lambda.Compile();
    }
}
