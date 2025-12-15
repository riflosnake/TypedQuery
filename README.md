# TypedQuery

A .NET library for **batching multiple database queries into a single roundtrip** with type-safe, reusable query definitions.

## The Problem

### 1. EF Core can't run queries concurrently

```csharp
// 3 sequential roundtrips, no concurrency allowed
var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
var orders = await db.Orders.Where(o => o.UserId == userId).ToListAsync();
var addresses = await db.Addresses.Where(a => a.UserId == userId).ToListAsync();
// Each query waits for the previous one. Database executes them one by one.
```

### 2. Dapper's `QueryMultipleAsync` gets messy

```csharp
// hard to read, maintain, and reuse
var sql = @"
    SELECT * FROM Users WHERE Id = @userId;
    SELECT * FROM Orders WHERE UserId = @userId;
    SELECT * FROM Addresses WHERE UserId = @userId;";

using var multi = await connection.QueryMultipleAsync(sql, new { userId = 5 });
var user = await multi.ReadFirstOrDefaultAsync<User>();
var orders = (await multi.ReadAsync<Order>()).ToList();
var addresses = (await multi.ReadAsync<Address>()).ToList();
```

## The Solution

TypedQuery batches your queries into **a single SQL script**.

```csharp
var result = await db
    .ToTypedQuery()
    .Add(new GetUserById(userId))
    .Add(new GetOrdersByUserId(userId))
    .Add(new GetAddressesByUserId(userId))
    .ExecuteAsync();

var user = result.Get<UserDto>();
var orders = result.GetList<OrderDto>();
var addresses = result.GetList<AddressDto>();
// One roundtrip.
```

This drives developers toward cleaner code while improving performance when multiple independent queries are needed together and database roundtrips are expensive.

## Installation

```bash
dotnet add package TypedQuery

# or for EF Core integration =>

dotnet add package TypedQuery.EntityFrameworkCore
```

## Quick Start

### Define Queries

#### Raw SQL Query

```csharp
public class GetUserById(int id) : ITypedQuery<UserDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, Email FROM Users WHERE Id = @id",
            new { id });
    }
}
```

#### EF Core Query

```csharp
public class GetOrdersByUserId(int userId) : ITypedQuery<AppDbContext, OrderDto>
{
    public IQueryable<OrderDto> Query(AppDbContext db)
    {
        return db.Orders
            .Where(o => o.UserId == userId)
            .Select(o => new OrderDto { Id = o.Id, Total = o.Total });
    }
}
```
> **⚠️ Important:** To use EF Core queries, you must register the TypedQuery interceptor:
> ```csharp
> services.AddDbContext<AppDbContext>(options => 
> {
>     options.UseSqlServer(connectionString);
>     options.UseTypedQuery();  // Required for EF Core mode!
> });
> ```

### Execute Queries

#### With EF Core DbContext

```csharp
var result = await dbContext
    .ToTypedQuery()
    .Add(new GetUserById(5))
    .Add(new GetOrdersByUserId(5))
    .ExecuteAsync();

var user = result.Get<UserDto>();
var orders = result.GetList<OrderDto>();
```

#### With Raw DbConnection

```csharp
var result = await connection
    .ToTypedQuery()
    .Add(new GetUserById(5))
    .Add(new GetActiveProducts())
    .ExecuteAsync();

var user = result.Get<UserDto>();
var products = result.GetList<ProductDto>();
```

## Why Use TypedQuery?

| Benefit | Description |
|---------|-------------|
| **Single Roundtrip** | Multiple queries in one database call |
| **Type-Safe** | Queries are classes with typed results |
| **Clean Code** | Structured convention, no SQL string concatenation |
| **SQL Caching** | EF Core queries compiled once, reused with Dapper |
| **Performance** | 2.5-3× faster than sequential EF Core (see benchmarks) |

## ⚡ Performance

### Key Results (SQLite in-memory, .NET 8.0)

| Scenario | Time | vs EF Core Direct | Memory |
|----------|------|-------------------|--------|
| **EF Core Direct** | 71 μs | baseline | 18.6 KB |
| **TypedQuery EF Core (Warm/Cached)** | 26 μs | **2.7× faster** ✅ | 9.6 KB |
| **TypedQuery Raw SQL** | 28 μs | 2.5× faster | 10.1 KB |
| **TypedQuery EF Core (Cold)** | 808 μs | 11× slower | 95.5 KB |

### Batched Queries (5 queries)

| Scenario | Time | Speedup | Memory |
|----------|------|---------|--------|
| **EF Core Sequential** | 373 μs | baseline | 88.7 KB |
| **TypedQuery Batched (Cached)** | 132 μs | **2.8× faster** ✅ | 45.7 KB |

### How It Works

TypedQuery uses a **dual-mode execution model**:

1. **First Call (Cold):** EF Core compiles LINQ → SQL, TypedQuery caches the template
2. **Subsequent Calls (Warm):** Skips EF Core entirely, executes via Dapper with cached SQL

This means:
- ❄️ **Cold start:** ~800μs overhead (one-time per query type)
- 🔥 **Warm/Cached:** **2.7× faster than EF Core** with 50% less memory

### With Network Latency

The benefits multiply with real-world network latency:

| Network Latency | EF Core Sequential (5 queries) | TypedQuery Batched | Speedup |
|-----------------|--------------------------------|-------------------|---------|
| 0ms (localhost) | 373 μs | 132 μs | **2.8×** |
| 5ms (cloud DB) | ~25 ms | ~5 ms | **5×** |
| 10ms (cross-region) | ~50 ms | ~10 ms | **5×** |
| 20ms (VPN/distant) | ~100 ms | ~20 ms | **5×** |

### Benchmark Details

```
BenchmarkDotNet v0.14.0, Windows 11
AMD Ryzen 5 5600U with Radeon Graphics, 6 cores
.NET 8.0.22, X64 RyuJIT AVX2

| Method                             | Mean      | Ratio | Allocated |
|----------------------------------- |----------:|------:|----------:|
| EfCore_Direct                      |  70.97 μs |  1.00 |  18.61 KB |
| TypedQuery_EfCore_Warm             |  26.44 μs |  0.37 |   9.58 KB |
| TypedQuery_RawSql                  |  28.10 μs |  0.40 |  10.09 KB |
| TypedQuery_EfCore_Cold             | 808.17 μs | 11.39 |  95.46 KB |
| TypedQuery_EfCore_Batched_5Queries | 131.98 μs |  1.86 |  45.74 KB |
| EfCore_Sequential_5Queries         | 372.89 μs |  5.25 |  88.74 KB |
```

**See [benchmarks/](benchmarks/) for full benchmark code and results.**

---

## Packages

| Package | Description |
|---------|-------------|
| `TypedQuery.Abstractions` | Core interfaces (`ITypedQuery<T>`) |
| `TypedQuery` | Query execution with Dapper |
| `TypedQuery.EntityFrameworkCore` | EF Core integration with SQL caching |

## How SQL Caching Works

When you use EF Core queries with TypedQuery:

```csharp
// First call: EF Core compiles LINQ → SQL, template cached
var result1 = await db.ToTypedQuery()
    .Add(new GetProductById(1))  // Compiles, caches
    .ExecuteAsync();

// Subsequent calls: Uses cached SQL, executes via Dapper
var result2 = await db.ToTypedQuery()
    .Add(new GetProductById(999))  // Cache hit! No EF Core
    .ExecuteAsync();
```

**What gets cached:**
- SQL template
- Parameter metadata (names, types, positions)

**What's fresh each call:**
- Parameter values (read from query instance fields via reflection)

**How parameter binding works:**
- EF Core parameters are named like `@__fieldName_0`
- TypedQuery extracts the field name from the parameter name
- This enables reliable binding even when parameter values are identical

## Contributing

Contributions are welcome! This library is in active development.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built on top of [Dapper](https://github.com/DapperLib/Dapper) for query execution
- Inspired by the need for cleaner batch query patterns in .NET
