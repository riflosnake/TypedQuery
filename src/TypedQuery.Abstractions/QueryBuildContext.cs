namespace TypedQuery.Abstractions;

/// <summary>
/// Provides context information during query building.
/// Contains information about the execution environment (e.g., DbContext for EF Core queries).
/// </summary>
/// <remarks>
/// Creates a new QueryBuildContext.
/// </remarks>
/// <param name="dbContext">The DbContext instance, or null for raw SQL execution</param>
public sealed class QueryBuildContext(object? dbContext)
{
    /// <summary>
    /// The DbContext instance if executing in an EF Core context, otherwise null.
    /// </summary>
    public object? DbContext { get; } = dbContext;
}
