using System.Collections.Concurrent;
using System.Data.Common;

namespace TypedQuery.Internal;

/// <summary>
/// Caches DbProviderFactory instances by connection type to avoid repeated lookups.
/// DbProviderFactories.GetFactory is expensive and should not be called on every query.
/// </summary>
internal static class ProviderFactoryCache
{
    private static readonly ConcurrentDictionary<Type, DbProviderFactory> _cache = new();

    /// <summary>
    /// Gets the DbProviderFactory for the given connection, using cached value if available.
    /// </summary>
    public static DbProviderFactory GetFactory(DbConnection connection)
    {
        var connectionType = connection.GetType();
        
        return _cache.GetOrAdd(connectionType, _ =>
        {
            return DbProviderFactories.GetFactory(connection) 
                ?? throw new NotSupportedException(
                    $"Provider {connectionType.Name} does not expose DbProviderFactory");
        });
    }
}
