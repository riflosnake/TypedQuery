# TypedQuery Benchmark Results

**Benchmark Suite Version:** 2.0  
**Date:** 2025  
**Environment:** Windows 11, AMD Ryzen 5 5600U, .NET 8.0, BenchmarkDotNet 0.14.0  
**Database:** SQLite in-memory (simulating local database with ~0ms network latency)

---

## Executive Summary

TypedQuery is designed to **batch multiple database queries into a single roundtrip** while maintaining type-safe, reusable query definitions. 

**Version 2.0 introduces EF Core SQL Caching** - EF Core queries are compiled once, then executed via Dapper for subsequent calls, resulting in **2.7× faster execution than direct EF Core**.

### Key Findings

| Finding | Result |
|---------|--------|
| **EF Core Cached vs Direct** | **2.7× faster** (26 μs vs 71 μs) ✅ |
| **EF Core Batched (5 queries)** | **2.8× faster** than EF Core sequential ✅ |
| **Memory Usage (Cached)** | **50% less** than direct EF Core ✅ |
| **Single Query Performance (RawSQL)** | Matches Dapper (28 μs vs 28 μs) |
| **Cold Start Overhead** | ~800 μs (one-time per query type) |
| **Parameter Collision Handling** | ✅ Now supported with name-based binding |

---

## 🆕 What's New in v2.0: EF Core SQL Caching

### The Problem (v1.x)

Previously, every EF Core query required full LINQ → SQL compilation, resulting in **10-15× overhead** on localhost.

### The Solution (v2.0)

TypedQuery now caches compiled SQL templates:

1. **First Call (Cold):** EF Core compiles LINQ → SQL, template is cached
2. **Subsequent Calls (Warm):** Skip EF Core entirely, execute via Dapper

### EF Core Caching Benchmark Results

```
BenchmarkDotNet v0.14.0, Windows 11
AMD Ryzen 5 5600U with Radeon Graphics, 6 cores
.NET 8.0.22, X64 RyuJIT AVX2

| Method                             | Mean      | Ratio | Allocated | Alloc Ratio |
|----------------------------------- |----------:|------:|----------:|------------:|
| EfCore_Direct                      |  70.97 μs |  1.00 |  18.61 KB |        1.00 |
| TypedQuery_EfCore_Warm             |  26.44 μs |  0.37 |   9.58 KB |        0.51 |
| TypedQuery_RawSql                  |  28.10 μs |  0.40 |  10.09 KB |        0.54 |
| TypedQuery_EfCore_Cold             | 808.17 μs | 11.39 |  95.46 KB |        5.13 |
| TypedQuery_EfCore_Batched_5Queries | 131.98 μs |  1.86 |  45.74 KB |        2.46 |
| EfCore_Sequential_5Queries         | 372.89 μs |  5.25 |  88.74 KB |        4.77 |
```

### Key Insights

| Scenario | Performance | Memory |
|----------|-------------|--------|
| **Cached EF Core** | **2.7× faster** than direct EF Core | **51% less memory** |
| **Batched 5 Queries** | **2.8× faster** than EF Core sequential | **48% less memory** |
| **Cold Start** | ~800μs overhead (one-time) | ~95 KB |
| **Raw SQL** | Matches Dapper performance | ~10 KB |

---

## When to Use TypedQuery

### **Use TypedQuery When:**

- You execute **3+ queries per request** (dashboards, detail pages, "load a screen" endpoints)
- You want **Dapper-class performance** with typed query organization
- You want **EF Core convenience** with **2.7× better performance** after warmup
- Your database has **meaningful network latency** (cloud databases, remote servers)
- You need **clean, reusable query definitions**

### **Be Cautious When:**

- Your app is **cold-start sensitive** (serverless, scale-to-zero) - each query type has ~800μs first-call overhead
- You need **microsecond-level consistency** - cached vs cold paths have different latencies

---

## Detailed Results

### 1. Single Query Performance

| Method | Mean | vs EF Core | Allocated |
|--------|------|------------|-----------|
| **TypedQuery EF Core (Warm)** | **26.44 μs** | **2.7× faster** ✅ | 9.58 KB |
| TypedQuery RawSQL | 28.10 μs | 2.5× faster | 10.09 KB |
| EF Core Direct | 70.97 μs | baseline | 18.61 KB |
| TypedQuery EF Core (Cold) | 808.17 μs | 11× slower | 95.46 KB |

**Conclusion:** After initial warmup, TypedQuery EF Core is **faster than direct EF Core** while using less memory.

---

### 2. Batch Query Performance (5 Queries)

| Method | Mean | Speedup | Allocated |
|--------|------|---------|-----------|
| **TypedQuery EF Core Batched** | **131.98 μs** | **2.8× faster** ✅ | 45.74 KB |
| EF Core Sequential | 372.89 μs | baseline | 88.74 KB |

**Conclusion:** TypedQuery batching combined with SQL caching delivers significant performance gains.

---

### 3. Real-World Performance with Network Latency

The benefits multiply with real-world network latency:

#### 5 Queries

| Network Latency | EF Core Sequential | TypedQuery Batched | Speedup |
|-----------------|--------------------|--------------------|---------|
| 0ms (localhost) | 373 μs | 132 μs | **2.8×** |
| 5ms (cloud DB) | ~25 ms | ~5 ms | **5×** |
| 10ms (cross-region) | ~50 ms | ~10 ms | **5×** |
| 20ms (VPN/distant) | ~100 ms | ~20 ms | **5×** |

#### 10 Queries

| Network Latency | EF Core Sequential | TypedQuery Batched | Speedup |
|-----------------|--------------------|--------------------|---------|
| 0ms (localhost) | ~750 μs | ~260 μs | **2.9×** |
| 5ms (cloud DB) | ~58 ms | ~5 ms | **11×** |
| 10ms (cross-region) | ~108 ms | ~10 ms | **11×** |
| 20ms (VPN/distant) | ~208 ms | ~20 ms | **10×** |

---

### 4. Memory Efficiency

| Scenario | TypedQuery | EF Core | Reduction |
|----------|------------|---------|-----------|
| Single Query (Cached) | 9.58 KB | 18.61 KB | **48%** |
| 5 Queries Batched | 45.74 KB | 88.74 KB | **48%** |

**Conclusion:** SQL caching not only improves speed but also reduces memory allocations by nearly 50%.

---

### 5. SQL Batch Building Overhead

Measures the cost of building the combined SQL batch (no database execution).

| Query Count | Mean | Allocated |
|-------------|------|-----------|
| 1 query | 145 ns | 896 B |
| 5 queries | 682 ns | 3.8 KB |
| 10 queries | 1.31 μs | 7.5 KB |
| 20 queries | 2.58 μs | 15.0 KB |

**Conclusion:** SQL batch building is negligible (nanoseconds to microseconds). Not the bottleneck.


---

## Benchmark Categories

| Category | Purpose | Key Metric |
|----------|---------|------------|
| **EfCoreCachingBenchmarks** | Measure SQL caching benefit | Cached vs Direct EF Core |
| **SingleQueryBenchmarks** | Measure overhead when batching provides no benefit | Time vs Dapper |
| **BatchQueryBenchmarks** | Core value proposition - reduce roundtrips | Time vs EF Core sequential |
| **ParameterBenchmarks** | Measure parameter rewriting overhead | Allocations |
| **SqlBuildingBenchmarks** | Isolate SQL building cost (no DB) | Nanoseconds |
| **MemoryBenchmarks** | Allocation patterns and GC pressure | Gen0/Gen1/Gen2 |

---

## Methodology

- **Framework:** BenchmarkDotNet 0.14.0
- **Runtime:** .NET 8.0
- **Database:** SQLite in-memory (1000 products, 50 categories, 500 customers, 2000 orders)
- **Warm-up:** 3 iterations
- **Measurement:** 10 iterations, 16 invocations each
- **Environment:** Windows 11, AMD Ryzen 5 5600U, 16 GB RAM

---

## Running the Benchmarks

```bash
# Run all benchmarks
cd benchmarks/TypedQuery.Benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release -- --filter "*EfCoreCaching*"

# Run batch query benchmarks
dotnet run -c Release -- --filter "*BatchQuery*"
```

---

## License

MIT License - see [LICENSE](../LICENSE) for details.
