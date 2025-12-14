# TypedQuery Benchmarks

Comprehensive performance benchmarks for the TypedQuery library.

## Quick Start

Run all benchmarks:
```powershell
dotnet run -c Release --project benchmarks/TypedQuery.Benchmarks
```

## Available Benchmarks

| Benchmark | Purpose |
|-----------|---------|
| **SingleQueryBenchmarks** | Compare single query overhead: ADO.NET vs Dapper vs EF Core vs TypedQuery |
| **BatchQueryBenchmarks** | **? Most Important** - Shows TypedQuery's batching advantage (2-20 queries) |
| **ParameterBenchmarks** | Measure parameter handling and rewriting overhead |
| **SqlBuildingBenchmarks** | Isolate SQL batch building cost (no DB execution) |
| **ResultMappingBenchmarks** | Measure result materialization and access patterns |
| **MemoryBenchmarks** | Memory allocation analysis with GC diagnostics |
| **ConcurrencyBenchmarks** | Multi-threaded execution and thread-safety |
| **RealWorldScenarioBenchmarks** | Practical scenarios: dashboards, order details, search |

## Running Specific Benchmarks

Run a specific benchmark class:
```powershell
dotnet run -c Release --project benchmarks/TypedQuery.Benchmarks -- --filter *BatchQuery*
```

Run with specific job (quick test):
```powershell
dotnet run -c Release --project benchmarks/TypedQuery.Benchmarks -- --job short
```

Generate HTML report:
```powershell
dotnet run -c Release --project benchmarks/TypedQuery.Benchmarks -- --exporters html
```

## Understanding Results

### Key Metrics

- **Mean**: Average execution time
- **Error**: 99.9% confidence interval
- **StdDev**: Standard deviation of measurements
- **Allocated**: Memory allocated per operation
- **Gen0/Gen1/Gen2**: Garbage collection counts
- **Rank**: Relative ranking (1 = best)

### Expected Performance Characteristics

1. **Single Queries**: TypedQuery has small overhead (~5-15%) due to abstraction
2. **Batched Queries (3+)**: TypedQuery shows significant advantage (30-70% faster) by reducing round-trips
3. **Memory**: Parameter rewriting and dictionary lookups add allocations
4. **EF Core Integration**: Interceptor-based capture has measurable cost vs raw SQL

### Break-even Point

TypedQuery typically becomes faster than sequential queries at **3+ queries** due to reduced network round-trips.

## Database Setup

Benchmarks use SQLite in-memory database with:
- **Small**: 100 products, 10 categories, 50 customers, 200 orders
- **Medium**: 1000 products, 50 categories, 500 customers, 2000 orders
- **Large**: 10000 products, 100 categories, 5000 customers, 20000 orders

Most benchmarks use **Medium** size for balance between realism and speed.

## Results Location

BenchmarkDotNet generates results in:
- `BenchmarkDotNet.Artifacts/results/` - Detailed reports (Markdown, HTML, CSV)
- Console output with summary tables

## Requirements

- .NET 8 SDK
- Release configuration (Debug builds will fail)
- No other intensive processes running (for accurate results)

## Notes

- Always run in **Release** configuration
- Results are reproducible (fixed random seed)
- First run may be slower due to JIT compilation
- Warm-up runs are included to stabilize JIT
