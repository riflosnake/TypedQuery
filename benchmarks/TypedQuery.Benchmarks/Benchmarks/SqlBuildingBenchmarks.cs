using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TypedQuery.Abstractions;
using TypedQuery.Benchmarks.Queries;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Isolates SQL batch building overhead without database execution.
/// Measures the cost of SqlBatchBuilder.Build, parameter rewriting, and EF Core capture.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class SqlBuildingBenchmarks
{
    private TypedQueryBuilder _builder1 = null!;
    private TypedQueryBuilder _builder5 = null!;
    private TypedQueryBuilder _builder10 = null!;
    private TypedQueryBuilder _builder20 = null!;
    private QueryBuildContext _context = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _context = new QueryBuildContext(null);
        
        // Pre-build query builders with different query counts
        _builder1 = new TypedQueryBuilder();
        _builder1.AddInstance(new GetProductByIdQuery(1));
        
        _builder5 = new TypedQueryBuilder();
        for (int i = 0; i < 5; i++)
        {
            _builder5.AddInstance(new GetProductsByCategoryQuery(i + 1));
        }
        
        _builder10 = new TypedQueryBuilder();
        for (int i = 0; i < 10; i++)
        {
            _builder10.AddInstance(new GetProductsByCategoryQuery(i + 1));
        }
        
        _builder20 = new TypedQueryBuilder();
        for (int i = 0; i < 20; i++)
        {
            _builder20.AddInstance(new GetProductsByCategoryQuery(i + 1));
        }
    }
    
    [Benchmark(Baseline = true)]
    public object BuildBatch_1Query()
    {
        return SqlBatchBuilder.Build(_builder1, _context);
    }
    
    [Benchmark]
    public object BuildBatch_5Queries()
    {
        return SqlBatchBuilder.Build(_builder5, _context);
    }
    
    [Benchmark]
    public object BuildBatch_10Queries()
    {
        return SqlBatchBuilder.Build(_builder10, _context);
    }
    
    [Benchmark]
    public object BuildBatch_20Queries()
    {
        return SqlBatchBuilder.Build(_builder20, _context);
    }
    
    // Test with varying parameter counts
    
    [Benchmark]
    public object BuildBatch_5Queries_NoParams()
    {
        var builder = new TypedQueryBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder.AddInstance(new GetActiveProductsQuery());
        }
        return SqlBatchBuilder.Build(builder, _context);
    }
    
    [Benchmark]
    public object BuildBatch_5Queries_1ParamEach()
    {
        var builder = new TypedQueryBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder.AddInstance(new GetProductByIdQuery(i + 1));
        }
        return SqlBatchBuilder.Build(builder, _context);
    }
    
    [Benchmark]
    public object BuildBatch_5Queries_5ParamsEach()
    {
        var builder = new TypedQueryBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder.AddInstance(new GetProductsMultiFilterQuery(1, 10m, 500m, true, 5));
        }
        return SqlBatchBuilder.Build(builder, _context);
    }
    
    [Benchmark]
    public object BuildBatch_5Queries_10ParamsEach()
    {
        var builder = new TypedQueryBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder.AddInstance(new GetProductsWith10ParamsQuery(
                1, 10m, 500m, true, 5,
                "%Product%", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, 0, 50));
        }
        return SqlBatchBuilder.Build(builder, _context);
    }
}
