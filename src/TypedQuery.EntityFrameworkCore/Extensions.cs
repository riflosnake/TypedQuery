using TypedQuery.Abstractions;
using TypedQuery.EntityFrameworkCore.Interceptor;
using TypedQuery.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace TypedQuery.EntityFrameworkCore;

/// <summary>
/// Extension methods for executing TypedQuery batches on DbContext.
/// Enables EF Core queries to be mixed with raw SQL queries.
/// All execution uses Dapper via DapperExecutionHelper for consistency and performance.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Creates a fluent query executor for composing and executing queries.
    /// This is the entry point for the new simplified API.
    /// </summary>
    /// <typeparam name="TDbContext">The specific DbContext type</typeparam>
    /// <param name="dbContext">The DbContext to execute queries against</param>
    /// <returns>A TypedQueryExecutor for building and executing queries</returns>
    public static TypedQueryExecutor<TDbContext> ToTypedQuery<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext
    {
        return new TypedQueryExecutor<TDbContext>(
            dbContext,
            ExecuteInternalAsync);
    }

    /// <summary>
    /// Internal execution logic using Dapper for all query types.
    /// </summary>
    private static async Task<TypedQueryResult> ExecuteInternalAsync(
        DbContext dbContext,
        TypedQueryBuilder builder,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();

        var context = new QueryBuildContext(dbContext);

        var wasClosed = connection.State == ConnectionState.Closed;
        
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var factory = ProviderFactoryCache.GetFactory(connection);

            var batch = SqlBatchBuilder.Build(builder, context, factory);

            var currentTransaction = dbContext.Database.CurrentTransaction;
            var transaction = currentTransaction?.GetDbTransaction();

            return await DapperExecutionHelper.ExecuteBatchAsync(
                connection,
                batch,
                transaction,
                commandTimeout: null,
                cancellationToken);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static DbContextOptionsBuilder<TContext> UseTypedQuery<TContext>(
        this DbContextOptionsBuilder<TContext> options)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);

        var extension =
            options.Options.FindExtension<TypedQueryOptionsExtension>()
            ?? new TypedQueryOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)options)
            .AddOrUpdateExtension(extension);

        return options;
    }

    public static DbContextOptionsBuilder UseTypedQuery(
        this DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var extension =
            options.Options.FindExtension<TypedQueryOptionsExtension>()
            ?? new TypedQueryOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)options)
            .AddOrUpdateExtension(extension);

        return options;
    }
}

internal sealed class TypedQueryOptionsExtension
    : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);
    private ExtensionInfo? _info;

    public void ApplyServices(IServiceCollection services)
    {
        services.AddScoped<IInterceptor, TypedQueryInterceptor>();
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "using TypedQuery ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(
            IDictionary<string, string> debugInfo)
        { }
    }
}
