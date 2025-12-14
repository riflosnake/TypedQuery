using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// THIS IS THE MOST IMPORTANT BENCHMARK - Shows TypedQuery's primary value proposition.
/// Measures performance when batching multiple queries into a single database round-trip.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class BatchQueryBenchmarks
{
    private SqliteConnection _connection = null!;
    private BenchmarkDbContext _dbContext = null!;
    
    [Params(2, 5, 10)]
    public int QueryCount { get; set; }
    
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
    public async Task<List<object>> AdoNet_Sequential()
    {
        var results = new List<object>();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name, Price FROM Products WHERE CategoryId = {categoryId} AND IsActive = 1";
            using var reader = await cmd.ExecuteReaderAsync();
            var categoryResults = new List<ProductDto>();
            while (await reader.ReadAsync())
            {
                categoryResults.Add(new ProductDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = reader.GetDecimal(2)
                });
            }
            results.Add(categoryResults);
        }
        
        return results;
    }
    
    [Benchmark]
    public async Task<List<object>> Dapper_Sequential()
    {
        var results = new List<object>();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            var categoryResults = await _connection.QueryAsync<ProductDto>(
                "SELECT Id, Name, Price FROM Products WHERE CategoryId = @categoryId AND IsActive = 1",
                new { categoryId });
            results.Add(categoryResults.ToList());
        }
        
        return results;
    }
    
    [Benchmark]
    public async Task<object> Dapper_QueryMultiple()
    {
        // Manually construct batched SQL for Dapper
        var sqlParts = new List<string>();
        var parameters = new DynamicParameters();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            sqlParts.Add($"SELECT Id, Name, Price FROM Products WHERE CategoryId = @categoryId{i} AND IsActive = 1");
            parameters.Add($"categoryId{i}", categoryId);
        }
        
        var sql = string.Join(";\n", sqlParts);
        
        using var gridReader = await _connection.QueryMultipleAsync(sql, parameters);
        
        var results = new List<List<ProductDto>>();
        for (int i = 0; i < QueryCount; i++)
        {
            results.Add((await gridReader.ReadAsync<ProductDto>()).ToList());
        }
        
        return results;
    }
    
    [Benchmark]
    public async Task<List<object>> EfCore_Sequential()
    {
        var results = new List<object>();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            var categoryResults = await _dbContext.Products
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
                .ToListAsync();
            results.Add(categoryResults);
        }
        
        return results;
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_RawSql_Batched()
    {
        var executor = _connection.ToTypedQuery();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            executor = executor.Add(new GetProductsByCategoryQuery(categoryId));
        }
        
        return await executor.ExecuteAsync();
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> TypedQuery_EfCore_Batched()
    {
        var executor = _dbContext.ToTypedQuery();
        
        for (int i = 0; i < QueryCount; i++)
        {
            var categoryId = (i % 10) + 1;
            executor = executor.Add(new GetProductsByCategoryEfQuery(categoryId));
        }
        
        return await executor.ExecuteAsync();
    }
}
