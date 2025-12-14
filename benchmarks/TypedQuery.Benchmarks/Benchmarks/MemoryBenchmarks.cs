using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Data.Sqlite;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Measures memory allocation patterns in TypedQuery execution.
/// Focus on Gen0/Gen1/Gen2 collections and bytes allocated.
/// </summary>
[MemoryDiagnoser]
[GcForce]
[SimpleJob(RuntimeMoniker.Net80)]
public class MemoryBenchmarks
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _dbContext = null!;
    
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
    public async Task<TypedQueryResult> SingleQuery_NoParams()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> SingleQuery_WithParam()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetProductByIdQuery(1))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> BatchQuery_5Queries_NoParams()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .Add(new GetAllCustomersQuery())
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> BatchQuery_5Queries_WithParams()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetProductsByCategoryQuery(1))
            .Add(new GetProductsByCategoryQuery(2))
            .Add(new GetProductsByCategoryQuery(3))
            .Add(new GetProductsByCategoryQuery(4))
            .Add(new GetProductsByCategoryQuery(5))
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> BatchQuery_10Queries_Mixed()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetProductByIdQuery(1))
            .Add(new GetAllCategoriesQuery())
            .Add(new GetProductsByCategoryQuery(1))
            .Add(new GetCustomerByIdQuery(1))
            .Add(new GetProductByIdQuery(2))
            .Add(new GetAllCustomersQuery())
            .Add(new GetProductsByCategoryQuery(2))
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> EfCoreQuery_Batch_5Queries()
    {
        return await _dbContext
            .ToTypedQuery()
            .Add(new GetActiveProductsEfQuery())
            .Add(new GetAllCategoriesEfQuery())
            .Add(new GetAllCustomersEfQuery())
            .Add(new GetActiveProductsEfQuery())
            .Add(new GetAllCategoriesEfQuery())
            .ExecuteAsync();
    }
}
