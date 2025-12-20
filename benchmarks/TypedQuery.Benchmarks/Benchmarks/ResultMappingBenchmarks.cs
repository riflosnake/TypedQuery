using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using Microsoft.Data.Sqlite;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Measures result materialization and TypedQueryResult access patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class ResultMappingBenchmarks
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
    
    // Single result access
    
    [Benchmark(Baseline = true)]
    public async Task<ProductDto?> Dapper_GetSingle()
    {
        return await _connection.QueryFirstOrDefaultAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE Id = 1");
    }
    
    [Benchmark]
    public async Task<ProductDto?> TypedQuery_GetSingle()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetProductByIdQuery(1))
            .ExecuteAsync();
        
        return result.GetFirstOrDefault<ProductDto>();
    }
    
    // List result access
    
    [Benchmark]
    public async Task<List<ProductDto>> Dapper_GetList()
    {
        return (await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1")).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_GetList()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .ExecuteAsync();
        
        return result.GetList<ProductDto>().ToList();
    }
    
    // Multiple results access (batched)
    
    [Benchmark]
    public async Task<object> Dapper_MultipleResults()
    {
        var sql = @"
            SELECT Id, Name, Price FROM Products WHERE IsActive = 1;
            SELECT Id, Name, DisplayOrder FROM Categories WHERE IsActive = 1;
            SELECT Id, FirstName, LastName, Email FROM Customers LIMIT 50;
        ";
        
        using var gridReader = await _connection.QueryMultipleAsync(sql);
        
        var products = (await gridReader.ReadAsync<ProductDto>()).ToList();
        var categories = (await gridReader.ReadAsync<CategoryDto>()).ToList();
        var customers = (await gridReader.ReadAsync<CustomerDto>()).ToList();
        
        return new { products, categories, customers };
    }
    
    [Benchmark]
    public async Task<object> TypedQuery_MultipleResults()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .Add(new GetAllCustomersQuery())
            .ExecuteAsync();
        
        var products = result.GetList<ProductDto>();
        var categories = result.GetList<CategoryDto>();
        var customers = result.GetList<CustomerDto>();
        
        return new { products, categories, customers };
    }
}
