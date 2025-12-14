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

**For single queries, avoid the overhead (negligible but measurable).**

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
            [new SqlParameter("@id", id)]);
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
| **Performance** | 3–11×+ faster under network latency (see below) |

## ⚡ Performance

> **TL;DR:** TypedQuery RawSQL mode matches Dapper performance. TypedQuery EF Core mode has significant overhead on localhost but wins with network latency due to single-roundtrip architecture.

### Localhost Benchmarks (SQLite in-memory, ~0ms latency)

#### RawSQL Mode Performance

TypedQuery RawSQL mode is for users writing raw SQL queries (like Dapper users):

| Scenario | TypedQuery RawSQL | Dapper | Overhead |
|----------|-------------------|--------|----------|
| **Single Query** | 536 μs | 530 μs | +1% |
| **5 Queries Batched** | 123 μs | 123 μs | +0.5% |
| **10 Queries Batched** | 264 μs | 253 μs | +4% |

**Conclusion:** RawSQL mode has negligible overhead vs Dapper.

#### EF Core Mode Performance

TypedQuery EF Core mode is for users wanting to write LINQ queries but batch them:

| Scenario | TypedQuery EF Core | EF Core Sequential | Result |
|----------|-------------------|-------------------|--------|
| **Single Query** | N/A (not intended use) | 970 μs | - |
| **5 Queries Batched** | 5,326 μs | 416 μs | **12.8× slower** |
| **10 Queries Batched** | 10,815 μs | 799 μs | **13.5× slower** |

**Conclusion:** On localhost, EF Core mode has multi-millisecond CPU overhead from the interceptor-based SQL capture mechanism. **This is slower than just running EF Core queries sequentially.**

**Why the overhead?**
- Each LINQ query is executed through EF Core to capture the generated SQL (~1ms per query)
- Parameter cloning and batching adds cost
- On localhost, this CPU cost dominates because network latency is ~0ms

### Real-World Performance (with network latency)

**Critical Context:** The localhost benchmarks above are misleading because they don't include network latency. In production where database round-trips can have meaningful overhead, TypedQuery's single-roundtrip architecture shines.

#### RawSQL Mode with Network Latency

RawSQL mode stays close to Dapper (both use single roundtrip):

| Network Latency | Sequential Dapper | TypedQuery RawSQL Batched | Speedup |
|-----------------|-------------------|---------------------------|---------|
| 0ms (localhost) | 412 μs | 123 μs | 3.4× faster |
| 5ms (cloud DB) | ~25 ms | ~5 ms | 5× faster |
| 10ms | ~50 ms | ~10 ms | 5× faster |

#### EF Core Mode with Network Latency

This is where the story changes - the fixed CPU overhead gets amortized by network savings:

**5 Queries Example:**

| Network Latency | EF Core Sequential | TypedQuery EF Core | Result |
|-----------------|--------------------|--------------------|--------|
| 0ms (localhost) | 416 μs | 5,326 μs | **12.8× slower** ❌ |
| 5ms (cloud DB) | 27.1 ms | 9.75 ms | **2.8× faster** ✅ |
| 10ms (cross-region) | 52.1 ms | 14.75 ms | **3.5× faster** ✅ |
| 20ms (VPN/distant) | 102.1 ms | 24.75 ms | **4.1× faster** ✅ |

**10 Queries Example:**

| Network Latency | EF Core Sequential | TypedQuery EF Core | Result |
|-----------------|--------------------|--------------------|--------|
| 0ms (localhost) | 799 μs | 10,815 μs | **13.5× slower** ❌ |
| 5ms (cloud DB) | 58.0 ms | 15.8 ms | **3.7× faster** ✅ |
| 10ms (cross-region) | 108.0 ms | 20.8 ms | **5.2× faster** ✅ |
| 20ms (VPN/distant) | 208.0 ms | 30.8 ms | **6.7× faster** ✅ |

### Key Insights

1. **RawSQL Mode:** Matches Dapper, great for raw SQL users who want typed organization
2. **EF Core Mode on Localhost:** Slower than native EF Core (10-15ms overhead for 5-10 queries)
3. **EF Core Mode on Cloud:** Faster than native EF Core (3-7× speedup with typical latency)
4. **The Trade-off:** Fixed CPU cost vs variable network cost

**See [benchmarks/BENCHMARK_RESULTS.md](benchmarks/BENCHMARK_RESULTS.md) for detailed analysis.**


---

## Packages

| Package | Description |
|---------|-------------|
| `TypedQuery.Abstractions` | Core interfaces (`ITypedQuery<T>`) |
| `TypedQuery` | Query execution with Dapper |
| `TypedQuery.EntityFrameworkCore` | EF Core integration |

## Contributing

Contributions are welcome! This library is in active development and there's room for performance improvements, especially in the EF Core query capture mechanism.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built on top of [Dapper](https://github.com/DapperLib/Dapper) for query execution
- Inspired by the need for cleaner batch query patterns in .NET
