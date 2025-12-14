using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Measures concurrent execution performance and thread-safety.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ConcurrencyBenchmarks
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _dbContext = null!;
    
    [Params(1, 4, 8)]
    public int ThreadCount { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        _dbContext = DatabaseSetup.CreateDbContext(_connection);
        DatabaseSetup.SeedDatabase(_dbContext, DataSize.Small);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }
    
    [Benchmark]
    public async Task ConcurrentBatchExecutions_RawSql()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => ExecuteBatchAsync())
            .ToArray();
        
        await Task.WhenAll(tasks);
    }
    
    private async Task<TypedQueryResult> ExecuteBatchAsync()
    {
        // Create new connection per task for thread safety
        using var connection = DatabaseSetup.CreateConnection();
        
        return await connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .Add(new GetAllCustomersQuery())
            .Add(new GetProductsByCategoryQuery(1))
            .Add(new GetProductByIdQuery(1))
            .ExecuteAsync();
    }
}
