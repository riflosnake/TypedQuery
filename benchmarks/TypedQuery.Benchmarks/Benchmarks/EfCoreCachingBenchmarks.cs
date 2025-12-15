using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;
using TypedQuery.EntityFrameworkCore;
using TypedQuery.EntityFrameworkCore.Interceptor;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Measures the benefit of EF Core SQL caching in TypedQuery.
/// 
/// Key scenarios:
/// 1. Cold (first call) - EF Core compiles LINQ ? SQL
/// 2. Warm (cached) - Uses cached SQL template, skips EF Core
/// 3. Comparison with direct EF Core execution
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class EfCoreCachingBenchmarks
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _dbContext = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        _dbContext = DatabaseSetup.CreateDbContext(_connection);
        DatabaseSetup.SeedDatabase(_dbContext, DataSize.Medium);
        
        // Clear cache for fair comparison
        TypedQueryInterceptor.ClearAll();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        (_dbContext as IDisposable)?.Dispose();
        _connection?.Dispose();
    }

    /// <summary>
    /// Baseline: Direct EF Core query execution (full pipeline every time)
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<List<ProductDto>> EfCore_Direct()
    {
        return await _dbContext.Products
            .Where(p => p.CategoryId == 1 && p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToListAsync();
    }

    /// <summary>
    /// TypedQuery with EF Core - first execution (cold, compiles SQL)
    /// Note: This will be slower due to compilation overhead
    /// </summary>
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_EfCore_Cold()
    {
        // Clear cache to simulate cold start
        TypedQueryInterceptor.ClearAll();
        
        return await _dbContext
            .ToTypedQuery()
            .Add(new GetProductsByCategoryEfQuery(1))
            .ExecuteAsync();
    }

    /// <summary>
    /// TypedQuery with EF Core - cached execution (warm, uses cached template)
    /// This should be significantly faster than cold start
    /// </summary>
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_EfCore_Warm()
    {
        // Ensure cache is warm (run once if not already)
        if (!TypedQueryInterceptor.HasCompiledTemplate(typeof(GetProductsByCategoryEfQuery)))
        {
            await _dbContext.ToTypedQuery()
                .Add(new GetProductsByCategoryEfQuery(1))
                .ExecuteAsync();
        }
        
        // Now run with cached template
        return await _dbContext
            .ToTypedQuery()
            .Add(new GetProductsByCategoryEfQuery(2)) // Different param to prove cache works
            .ExecuteAsync();
    }

    /// <summary>
    /// TypedQuery with raw SQL (no EF Core overhead at all)
    /// </summary>
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_RawSql()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetProductsByCategoryQuery(1))
            .ExecuteAsync();
    }

    /// <summary>
    /// Multiple EF Core queries - TypedQuery batched vs EF Core sequential
    /// This shows the combined benefit of caching + batching
    /// </summary>
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_EfCore_Batched_5Queries()
    {
        return await _dbContext
            .ToTypedQuery()
            .Add(new GetProductsByCategoryEfQuery(1))
            .Add(new GetProductsByCategoryEfQuery(2))
            .Add(new GetProductsByCategoryEfQuery(3))
            .Add(new GetProductsByCategoryEfQuery(4))
            .Add(new GetProductsByCategoryEfQuery(5))
            .ExecuteAsync();
    }

    /// <summary>
    /// Multiple EF Core queries - sequential execution (baseline for batching comparison)
    /// </summary>
    [Benchmark]
    public async Task<List<List<ProductDto>>> EfCore_Sequential_5Queries()
    {
        var results = new List<List<ProductDto>>();
        
        for (int i = 1; i <= 5; i++)
        {
            var categoryId = i;
            results.Add(await _dbContext.Products
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
                .ToListAsync());
        }
        
        return results;
    }
}
