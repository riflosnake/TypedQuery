using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using Microsoft.Data.Sqlite;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Measures parameter handling overhead in TypedQuery's parameter rewriting mechanism.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class ParameterBenchmarks
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
    
    // No parameters
    
    [Benchmark(Baseline = true)]
    public async Task<List<ProductDto>> Dapper_NoParameters()
    {
        return (await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1")).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_NoParameters()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
    
    // Single parameter
    
    [Benchmark]
    public async Task<List<ProductDto>> Dapper_SingleParameter()
    {
        return (await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE Id = @id",
            new { id = 1 })).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_SingleParameter()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetProductByIdQuery(1))
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
    
    // Five parameters
    
    [Benchmark]
    public async Task<List<ProductDto>> Dapper_FiveParameters()
    {
        return (await _connection.QueryAsync<ProductDto>(
            @"SELECT Id, Name, Price 
              FROM Products 
              WHERE CategoryId = @categoryId 
                AND Price BETWEEN @minPrice AND @maxPrice 
                AND IsActive = @isActive
                AND StockQuantity >= @minStock",
            new { categoryId = 1, minPrice = 10m, maxPrice = 500m, isActive = true, minStock = 5 })).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_FiveParameters()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetProductsMultiFilterQuery(1, 10m, 500m, true, 5))
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
    
    // Ten parameters
    
    [Benchmark]
    public async Task<List<ProductDto>> Dapper_TenParameters()
    {
        return (await _connection.QueryAsync<ProductDto>(
            @"SELECT Id, Name, Price 
              FROM Products 
              WHERE CategoryId = @categoryId 
                AND Price BETWEEN @minPrice AND @maxPrice 
                AND IsActive = @isActive
                AND StockQuantity >= @minStock
                AND Name LIKE @namePattern
                AND CreatedAt BETWEEN @fromDate AND @toDate
              ORDER BY Id
              LIMIT @limit OFFSET @offset",
            new 
            { 
                categoryId = 1, 
                minPrice = 10m, 
                maxPrice = 500m, 
                isActive = true, 
                minStock = 5,
                namePattern = "%Product%",
                fromDate = DateTime.UtcNow.AddYears(-1),
                toDate = DateTime.UtcNow,
                offset = 0,
                limit = 50
            })).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_TenParameters()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetProductsWith10ParamsQuery(
                1, 10m, 500m, true, 5,
                "%Product%", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, 0, 50))
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
    
    // Batched queries with parameters to test rewriting overhead
    
    [Benchmark]
    public async Task<object> Dapper_Batch_5QueriesWithParams()
    {
        var sql = @"
            SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat1 AND IsActive = 1;
            SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat2 AND IsActive = 1;
            SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat3 AND IsActive = 1;
            SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat4 AND IsActive = 1;
            SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat5 AND IsActive = 1;
        ";
        
        using var gridReader = await _connection.QueryMultipleAsync(sql, 
            new { cat1 = 1, cat2 = 2, cat3 = 3, cat4 = 4, cat5 = 5 });
        
        var results = new List<List<ProductDto>>();
        for (int i = 0; i < 5; i++)
        {
            results.Add((await gridReader.ReadAsync<ProductDto>()).ToList());
        }
        return results;
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_Batch_5QueriesWithParams()
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
}
