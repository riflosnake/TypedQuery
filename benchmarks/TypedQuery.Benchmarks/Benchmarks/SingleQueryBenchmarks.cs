using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class SingleQueryBenchmarks
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _dbContext = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        _dbContext = DatabaseSetup.CreateDbContext(_connection);
        DatabaseSetup.SeedDatabase(_dbContext, DataSize.Medium);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }
    
    [Benchmark(Baseline = true)]
    public async Task<List<ProductDto>> AdoNet_SingleSelect()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Price FROM Products WHERE IsActive = 1";
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<ProductDto>();
        while (await reader.ReadAsync())
        {
            results.Add(new ProductDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2)
            });
        }
        return results;
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> Dapper_SingleSelect()
    {
        return (await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1")).ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> EfCore_SingleSelect()
    {
        return await _dbContext.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToListAsync();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_RawSql_SingleSelect()
    {
        var result = await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
    
    [Benchmark]
    public async Task<List<ProductDto>> TypedQuery_EfCore_SingleSelect()
    {
        var result = await _dbContext
            .ToTypedQuery()
            .Add(new GetActiveProductsEfQuery())
            .ExecuteAsync();
        return result.GetList<ProductDto>().ToList();
    }
}
