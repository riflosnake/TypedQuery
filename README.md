# TypedQuery

[![NuGet](https://img.shields.io/nuget/v/TypedQuery.svg)](https://www.nuget.org/packages/TypedQuery)
[![License](https://img.shields.io/github/license/riflosnake/TypedQuery.svg)](LICENSE)

A high-performant .NET library for **batching multiple database queries into a single roundtrip** with type-safe, reusable query definitions.

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

This drives developers toward **cleaner code** while **improving performance** when multiple independent queries are needed together and database roundtrips are expensive.

## Installation

```bash
dotnet add package TypedQuery
```
or for EF Core integration =>
```bash
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

## Why TypedQuery?

| Feature | Description |
|--------|-------------|
| Single Roundtrip | Multiple queries executed in one database call |
| Type-Safe | Queries and results are strongly typed |
| Reusable | Queries are first-class objects |
| Clean Structure | No manual SQL concatenation |
| SQL Caching | EF Core queries compiled once, reused with Dapper |
| High Performance | Faster than sequential EF Core for multi-query scenarios |

---

## Performance Overview

### Local Benchmarks (SQLite in-memory, .NET 8)

| Scenario | Time | Comparison |
|---------|------|------------|
| EF Core Direct | 71 μs | Baseline |
| TypedQuery EF Core (Warm) | 26 μs | 2.7× faster |
| TypedQuery Raw SQL | 28 μs | 2.5× faster |
| TypedQuery EF Core (Cold) | 808 μs | One-time cost |
| EF Core Sequential (5 queries) | 373 μs | Baseline |
| TypedQuery Batched (5 queries) | 132 μs | 2.8× faster |

---

### Execution Model

TypedQuery uses a **dual-mode execution strategy**:

1. Cold execution (first use)
   - EF Core translates LINQ to SQL
   - SQL template and parameter metadata are cached

2. Warm execution (subsequent uses)
   - EF Core is skipped
   - Cached SQL is executed via Dapper

Implications:
- Cold cost is paid once per query type
- Warm execution is significantly faster and allocates less memory

---

## Packages

| Package | Description |
|---------|-------------|
| TypedQuery.Abstractions | Core interfaces |
| TypedQuery | Query execution engine |
| TypedQuery.EntityFrameworkCore | EF Core integration and SQL caching |

## Contributing

Contributions are welcome! This library is in active development.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built on top of [Dapper](https://github.com/DapperLib/Dapper) for query execution
- Inspired by the need for cleaner batch query patterns in .NET
