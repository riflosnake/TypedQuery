using TypedQuery.Abstractions;
using TypedQuery.EntityFrameworkCore.Interceptor;
using Microsoft.EntityFrameworkCore;

namespace TypedQuery.EntityFrameworkCore;

/// <summary>
/// EF Core query interface with dual execution modes:
/// 
/// Mode A (Compilation): First execution uses EF Core to compile LINQ → SQL
/// Mode B (Dapper): Subsequent executions skip EF Core, execute via Dapper
/// 
/// WARNING: Mode B bypasses EF Core entirely. No tracking, no identity resolution,
/// no navigation fixup, no concurrency semantics. Results are plain DTOs.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type</typeparam>
/// <typeparam name="TResult">The result DTO type</typeparam>
public interface ITypedQuery<TDbContext, TResult> : ITypedQuery<TResult>
    where TDbContext : DbContext
{
    /// <summary>
    /// Build the LINQ query (used for SQL compilation).
    /// </summary>
    IQueryable<TResult> Query(TDbContext db);

    /// <summary>
    /// Builds QueryDefinition using dual-mode execution:
    /// - If compiled template exists: use cached SQL + fresh parameters (Dapper mode)
    /// - Otherwise: compile via EF Core, cache template for next time
    /// </summary>
    QueryDefinition ITypedQuery<TResult>.Build(QueryBuildContext context)
    {
        if (context.DbContext is not TDbContext dbContext)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} requires {typeof(TDbContext).Name}, " +
                $"got {context.DbContext?.GetType().Name ?? "null"}.");
        }

        var queryType = GetType();

        // === MODE B: Dapper Execution (compiled template exists) ===
        if (TypedQueryInterceptor.HasCompiledTemplate(queryType))
        {
            var template = TypedQueryInterceptor.GetCompiledTemplate(queryType);
            if (template != null)
            {
                // Skip EF Core entirely - build parameters from fields, use cached SQL
                var connection = dbContext.Database.GetDbConnection();
                var factory = TypedQueryInterceptor.GetFactory(connection);
                var parameters = template.BuildParameters(this, factory);
                return new QueryDefinition(template.Sql, parameters);
            }
            // template is null = not compilable, fall through to Mode A
        }

        // === MODE A: EF Core Compilation ===
        var queryId = TypedQueryInterceptor.GenerateQueryId();
        TypedQueryInterceptor.RegisterForCompilation(queryId, this);

        var tag = string.Concat(TypedQueryInterceptor.CacheKeyPrefix, queryId);
        var query = Query(dbContext).TagWith(tag);

        // Trigger EF Core pipeline - interceptor captures SQL and suppresses execution
        try { _ = query.ToList(); } catch { }

        var captured = TypedQueryInterceptor.PullCapture(queryId)
            ?? throw new InvalidOperationException(
                $"SQL capture failed for {queryType.Name}. " +
                "Ensure UseTypedQuery() is configured.");

        return new QueryDefinition(captured.Sql, captured.Parameters);
    }
}
