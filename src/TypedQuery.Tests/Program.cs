using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TypedQuery;
using TypedQuery.Abstractions;
using TypedQuery.EntityFrameworkCore;
using TypedQuery.EntityFrameworkCore.Interceptor;

Console.WriteLine("=== TypedQuery Comprehensive Test Suite ===\n");

var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        CREATE TABLE Customers (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Email TEXT,
            IsActive INTEGER NOT NULL DEFAULT 1,
            Score INTEGER
        );
        INSERT INTO Customers (Id, Name, Email, IsActive, Score) VALUES 
            (1, 'Alice', 'alice@test.com', 1, 100),
            (2, 'Bob', 'bob@test.com', 1, 100),
            (3, 'Charlie', 'charlie@test.com', 0, 50),
            (4, 'Diana', 'diana@test.com', 1, 75),
            (5, 'Eve', 'eve@test.com', 0, NULL);
        """;
    await cmd.ExecuteNonQueryAsync();
}

var passed = 0;
var failed = 0;
var failedTests = new List<string>();

void Assert(bool cond, string name)
{
    if (cond) { Console.WriteLine($"  [PASS] {name}"); passed++; }
    else { Console.WriteLine($"  [FAIL] {name}"); failed++; failedTests.Add(name); }
}

void AssertThrows<T>(Action a, string name) where T : Exception
{
    try { a(); Console.WriteLine($"  [FAIL] {name} - no exception"); failed++; failedTests.Add(name); }
    catch (T) { Console.WriteLine($"  [PASS] {name}"); passed++; }
    catch (Exception ex) { Console.WriteLine($"  [FAIL] {name} - got {ex.GetType().Name}: {ex.Message}"); failed++; failedTests.Add(name); }
}

var opts = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).UseTypedQuery();

// === SECTION 1: RAW SQL ===
Console.WriteLine("\n--- Section 1: Raw SQL ---");
{
    var r = await connection.ToTypedQuery().Add(new GetAllCustomersRaw()).ExecuteAsync();
    Assert(r.GetList<CustomerDto>().Count == 5, "GetAll returns 5");
}
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(2)).ExecuteAsync();
    Assert(r.GetFirstOrDefault<CustomerDto>()?.Name == "Bob", "GetById(2) = Bob");
}
{
    var r1 = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(1)).ExecuteAsync();
    var r2 = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(4)).ExecuteAsync();
    Assert(r1.GetFirstOrDefault<CustomerDto>()?.Name == "Alice", "Param 1 = Alice");
    Assert(r2.GetFirstOrDefault<CustomerDto>()?.Name == "Diana", "Param 4 = Diana");
}

// === SECTION 2: EF CORE ---
Console.WriteLine("\n--- Section 2: EF Core ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetActiveCustomersEf()).ExecuteAsync();
    Assert(r.GetList<CustomerDto>().Count == 3, "Active customers = 3");
}
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetCustomerByIdEf(1)).ExecuteAsync();
    Assert(r.GetFirstOrDefault<CustomerDto>()?.Name == "Alice", "EF GetById(1) = Alice");
}
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetCustomerByNameEf("Charlie")).ExecuteAsync();
    Assert(r.GetFirstOrDefault<CustomerDto>()?.Name == "Charlie", "EF GetByName works");
}

// === SECTION 3: CACHING ===
Console.WriteLine("\n--- Section 3: Caching ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db1 = new TestDbContext(opts.Options);
    var r1 = await db1.ToTypedQuery().Add(new GetCustomerByIdEf(1)).ExecuteAsync();
    var s1 = TypedQueryInterceptor.GetCacheStats();

    await using var db2 = new TestDbContext(opts.Options);
    var r2 = await db2.ToTypedQuery().Add(new GetCustomerByIdEf(4)).ExecuteAsync();
    var s2 = TypedQueryInterceptor.GetCacheStats();

    Assert(r1.GetFirstOrDefault<CustomerDto>()?.Name == "Alice", "First = Alice");
    Assert(r2.GetFirstOrDefault<CustomerDto>()?.Name == "Diana", "Second = Diana (cached, different param)");
    Assert(s1.TemplateCount == 1, "Cached after first");
    Assert(s2.TemplateCount == 1, "Reused on second");
}

// Test: Same values for different parameters (NO LONGER A COLLISION!)
// With name-based matching, @__min_0 -> 'min' field, @__max_1 -> 'max' field
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetCustomersByScoreRangeEf(100, 100)).ExecuteAsync();
    var isCacheable = TypedQueryInterceptor.IsCacheable(typeof(GetCustomersByScoreRangeEf));
    Assert(isCacheable, "Same values (100,100) IS cacheable with name-based matching");
    
    // Verify it returns correct results
    var customers = r.GetList<CustomerDto>();
    Assert(customers.Count == 2, "Score=100 returns Alice and Bob");
}

// === SECTION 4: CONDITIONAL QUERIES ===
Console.WriteLine("\n--- Section 4: Conditional ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r1 = await db.ToTypedQuery().Add(new ConditionalQueryEf(true)).ExecuteAsync();
    Assert(r1.GetList<CustomerDto>().Count == 3, "filterActive=true returns 3");
}
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r2 = await db.ToTypedQuery().Add(new ConditionalQueryEf(false)).ExecuteAsync();
    var list = r2.GetList<CustomerDto>();
    Console.WriteLine($"    (filterActive=false returned {list.Count} customers)");
    Assert(list.Count == 5, "filterActive=false returns 5");
}

// === SECTION 5: MIXED BATCH ---
Console.WriteLine("\n--- Section 5: Mixed Batch ---");
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery()
        .Add(new GetCustomerByIdEf(1))
        .Add(new GetCountRaw())
        .ExecuteAsync();
    Assert(r.GetFirstOrDefault<CustomerDto>()?.Name == "Alice", "EF in mixed batch");
    Assert(r.GetFirstOrDefault<CountResult>()?.Count == 5, "Raw in mixed batch");
}

// === SECTION 6: RESULT ACCESSORS ---
Console.WriteLine("\n--- Section 6: Accessors ---");
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(1)).ExecuteAsync();
    Assert(r.GetSingle<CustomerDto>().Name == "Alice", "GetSingle works");
}
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(999)).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetSingle<CustomerDto>(), "GetSingle throws on empty");
}
{
    var r = await connection.ToTypedQuery().Add(new GetAllCustomersRaw()).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetSingle<CustomerDto>(), "GetSingle throws on many");
}
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(999)).ExecuteAsync();
    Assert(r.GetSingleOrDefault<CustomerDto>() == null, "GetSingleOrDefault null on empty");
}
{
    var r = await connection.ToTypedQuery().Add(new GetAllCustomersRaw()).ExecuteAsync();
    Assert(r.GetFirst<CustomerDto>() != null, "GetFirst works");
}
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(999)).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetFirst<CustomerDto>(), "GetFirst throws on empty");
}
{
    var r = await connection.ToTypedQuery().Add(new GetCustomerByIdRaw(999)).ExecuteAsync();
    Assert(r.GetFirstOrDefault<CustomerDto>() == null, "GetFirstOrDefault null on empty");
}

// === SECTION 7: EDGE CASES ===
Console.WriteLine("\n--- Section 7: Edge Cases ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetByNullScoreEf()).ExecuteAsync();
    Assert(r.GetList<CustomerDto>().Count == 1, "NULL score finds Eve");
}
{
    var r = await connection.ToTypedQuery().Add(new SearchEmailRaw("")).ExecuteAsync();
    Assert(r.GetList<CustomerDto>().Count == 5, "Empty pattern matches all");
}
{
    var r = await connection.ToTypedQuery().Add(new SearchEmailRaw("alice@")).ExecuteAsync();
    Assert(r.GetList<CustomerDto>().Count == 1, "@ in param works");
}

// Same values test - With name-based matching, ALL queries are cacheable!
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    await db.ToTypedQuery().Add(new GetCustomersByScoreRangeEf(50, 50)).ExecuteAsync();
    var isCacheable = TypedQueryInterceptor.IsCacheable(typeof(GetCustomersByScoreRangeEf));
    Console.WriteLine($"    (ScoreRange(50,50) cacheable: {isCacheable})");
    Assert(isCacheable, "Same values (50,50) IS cacheable with name matching");
}
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    await db.ToTypedQuery().Add(new GetCustomersByScoreRangeEf(50, 100)).ExecuteAsync();
    var isCacheable = TypedQueryInterceptor.IsCacheable(typeof(GetCustomersByScoreRangeEf));
    Console.WriteLine($"    (ScoreRange(50,100) cacheable: {isCacheable})");
    Assert(isCacheable, "Different values (50,100) cacheable");
}

// Verify cached query returns correct results even with same initial values
TypedQueryInterceptor.ClearAll();
{
    await using var db1 = new TestDbContext(opts.Options);
    var r1 = await db1.ToTypedQuery().Add(new GetCustomersByScoreRangeEf(50, 50)).ExecuteAsync();
    
    await using var db2 = new TestDbContext(opts.Options);
    var r2 = await db2.ToTypedQuery().Add(new GetCustomersByScoreRangeEf(75, 100)).ExecuteAsync();
    
    var score50 = r1.GetList<CustomerDto>();
    var score75to100 = r2.GetList<CustomerDto>();
    
    Assert(score50.Count == 1, "Score=50 returns Charlie (1 customer)");
    Assert(score75to100.Count == 3, "Score 75-100 returns 3 customers");
}

// === SECTION 8: PERFORMANCE ===
Console.WriteLine("\n--- Section 8: Performance ---");
TypedQueryInterceptor.ClearAll();
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        await using var db = new TestDbContext(opts.Options);
        await db.ToTypedQuery().Add(new GetCustomerByIdEf(i % 5 + 1)).ExecuteAsync();
    }
    sw.Stop();
    Console.WriteLine($"  100 cached calls: {sw.ElapsedMilliseconds}ms");
    Assert(sw.ElapsedMilliseconds < 5000, "100 calls under 5s");
}

// === SUMMARY ===
Console.WriteLine($"\n========================================");
Console.WriteLine($"RESULTS: {passed} passed, {failed} failed");
Console.WriteLine($"========================================");
if (failed > 0)
{
    Console.WriteLine("\nFailed tests:");
    foreach (var t in failedTests) Console.WriteLine($"  - {t}");
    Environment.Exit(1);
}
Console.WriteLine("\nALL TESTS PASSED!");


// === QUERY CLASSES ===
public class GetAllCustomersRaw : ITypedQuery<CustomerDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT Id, Name, Email, IsActive, Score FROM Customers");
}

public class GetCustomerByIdRaw(int id) : ITypedQuery<CustomerDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT Id, Name, Email, IsActive, Score FROM Customers WHERE Id = @id", new { id });
}

public class GetCountRaw : ITypedQuery<CountResult>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT COUNT(*) as Count FROM Customers");
}

public class SearchEmailRaw(string p) : ITypedQuery<CustomerDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT Id, Name, Email, IsActive, Score FROM Customers WHERE Email LIKE @p", new { p = $"%{p}%" });
}

public class GetActiveCustomersEf : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => c.IsActive)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

public class GetCustomerByIdEf(int id) : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => c.Id == id)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

public class GetCustomerByNameEf(string name) : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => c.Name == name)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

public class GetCustomersByScoreRangeEf(int min, int max) : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => c.Score >= min && c.Score <= max)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

public class ConditionalQueryEf(bool filterActive) : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db)
    {
        var q = db.Customers.AsQueryable();
        if (filterActive) q = q.Where(c => c.IsActive);
        return q.Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
    }
}

public class GetByNullScoreEf : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => c.Score == null)
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

public class TwoBoolsEf(bool a, bool b) : ITypedQuery<TestDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(TestDbContext db) =>
        db.Customers.Where(c => (!a || c.IsActive) && (!b || c.Email != null))
            .Select(c => new CustomerDto { Id = c.Id, Name = c.Name, Email = c.Email, IsActive = c.IsActive, Score = c.Score });
}

// === MODELS ===
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int? Score { get; set; }
}

public class CountResult { public int Count { get; set; } }

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int? Score { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    protected override void OnModelCreating(ModelBuilder m) =>
        m.Entity<Customer>(e => { e.ToTable("Customers"); e.HasKey(c => c.Id); });
}
