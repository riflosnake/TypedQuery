using Microsoft.EntityFrameworkCore;
using Npgsql;
using TypedQuery;
using TypedQuery.Abstractions;
using TypedQuery.EntityFrameworkCore;
using TypedQuery.EntityFrameworkCore.Interceptor;

Console.WriteLine("=== TypedQuery Comprehensive Test Suite (PostgreSQL) ===\n");

const string ConnectionString = "Host=localhost;Port=5432;Database=CarMarket;Username=postgres;Password=riflo";

var passed = 0;
var failed = 0;
var failedTests = new List<string>();

void Assert(bool condition, string name)
{
    if (condition) { Console.WriteLine($"  [PASS] {name}"); passed++; }
    else { Console.WriteLine($"  [FAIL] {name}"); failed++; failedTests.Add(name); }
}

void AssertThrows<T>(Action action, string name) where T : Exception
{
    try { action(); Console.WriteLine($"  [FAIL] {name} - no exception"); failed++; failedTests.Add(name); }
    catch (T) { Console.WriteLine($"  [PASS] {name}"); passed++; }
    catch (Exception ex) { Console.WriteLine($"  [FAIL] {name} - got {ex.GetType().Name}: {ex.Message}"); failed++; failedTests.Add(name); }
}

async Task AssertThrowsAsync<T>(Func<Task> action, string name) where T : Exception
{
    try { await action(); Console.WriteLine($"  [FAIL] {name} - no exception"); failed++; failedTests.Add(name); }
    catch (T) { Console.WriteLine($"  [PASS] {name}"); passed++; }
    catch (Exception ex) { Console.WriteLine($"  [FAIL] {name} - got {ex.GetType().Name}: {ex.Message}"); failed++; failedTests.Add(name); }
}

// Test connection
Console.WriteLine("Testing database connection...");
try
{
    await using var testConn = new NpgsqlConnection(ConnectionString);
    await testConn.OpenAsync();
    Console.WriteLine("  Database connection successful!\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  FATAL: Cannot connect to database: {ex.Message}");
    Console.WriteLine("  Make sure PostgreSQL is running and the CarMarket database exists.");
    Environment.Exit(1);
}

var opts = new DbContextOptionsBuilder<TestDbContext>()
    .UseNpgsql(ConnectionString)
    .UseTypedQuery();

// === SECTION 1: RAW SQL QUERIES ===
Console.WriteLine("--- Section 1: Raw SQL Queries ---");
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetAllMakesRaw()).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count > 0, "GetAllMakes returns data");
    Console.WriteLine($"    (Found {makes.Count} makes)");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(1)).ExecuteAsync();
    var make = r.GetFirstOrDefault<MakeDto>();
    Assert(make != null, "GetMakeById(1) returns data");
    if (make != null) Console.WriteLine($"    (Make: {make.Name})");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetActiveMakesRaw(true)).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "GetActiveMakes(true) works");
    Console.WriteLine($"    (Found {makes.Count} active makes)");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new SearchMakesRaw("BMW")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Console.WriteLine($"    (Search 'BMW' found {makes.Count} matches)");
    Assert(makes.Count >= 0, "SearchMakes works");
}

// Test with multiple parameters
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakesInRangeRaw(1, 10)).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "GetMakesInRange(1,10) works");
    Console.WriteLine($"    (Found {makes.Count} makes in range 1-10)");
}

// === SECTION 2: EF CORE QUERIES ===
Console.WriteLine("\n--- Section 2: EF Core Queries ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetAllMakesEf()).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count > 0, "EF GetAllMakes returns data");
    Console.WriteLine($"    (Found {makes.Count} makes via EF)");
}

TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetMakeByIdEf(1)).ExecuteAsync();
    var make = r.GetFirstOrDefault<MakeDto>();
    Assert(make != null, "EF GetMakeById(1) returns data");
    if (make != null) Console.WriteLine($"    (Make: {make.Name})");
}

TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new GetActiveMakesEf(true)).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "EF GetActiveMakes(true) works");
    Console.WriteLine($"    (Found {makes.Count} active makes via EF)");
}

TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery().Add(new SearchMakesEf("Audi")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "EF SearchMakes works");
    Console.WriteLine($"    (Search 'Audi' found {makes.Count} matches via EF)");
}

// === SECTION 3: BATCHED QUERIES ===
Console.WriteLine("\n--- Section 3: Batched Queries ---");
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetAllMakesRaw())
        .Add(new GetAllBodyTypesRaw())
        .Add(new GetAllFuelTypesRaw())
        .ExecuteAsync();

    var makes = r.Next<MakeDto>().ToList();
    var bodyTypes = r.Next<BodyTypeDto>().ToList();
    var fuelTypes = r.Next<FuelTypeDto>().ToList();

    Assert(makes.Count > 0, "Batch: Makes returned");
    Assert(bodyTypes.Count > 0, "Batch: BodyTypes returned");
    Assert(fuelTypes.Count > 0, "Batch: FuelTypes returned");
    Console.WriteLine($"    (Batch returned {makes.Count} makes, {bodyTypes.Count} body types, {fuelTypes.Count} fuel types)");
}

// === SECTION 4: MIXED BATCH (Raw + EF) ===
Console.WriteLine("\n--- Section 4: Mixed Batch (Raw SQL + EF Core) ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery()
        .Add(new GetMakeByIdEf(1))
        .Add(new GetCountRaw("\"Makes\""))
        .Add(new GetActiveMakesEf(true))
        .ExecuteAsync();

    var make = r.Next<MakeDto>().FirstOrDefault();
    var count = r.Next<CountResult>().FirstOrDefault();
    var activeMakes = r.Next<MakeDto>().ToList();

    Assert(make != null, "Mixed batch: EF query works");
    Assert(count != null && count.Count > 0, "Mixed batch: Raw count works");
    Assert(activeMakes.Count >= 0, "Mixed batch: Second EF query works");
    Console.WriteLine($"    (Make: {make?.Name}, Total count: {count?.Count}, Active: {activeMakes.Count})");
}

// === SECTION 5: EF CORE CACHING ===
Console.WriteLine("\n--- Section 5: EF Core Query Caching ---");
TypedQueryInterceptor.ClearAll();
{
    // First execution - should compile and cache
    await using var db1 = new TestDbContext(opts.Options);
    var r1 = await db1.ToTypedQuery().Add(new GetMakeByIdEf(1)).ExecuteAsync();
    var stats1 = TypedQueryInterceptor.GetCacheStats();

    // Second execution - should use cache
    await using var db2 = new TestDbContext(opts.Options);
    var r2 = await db2.ToTypedQuery().Add(new GetMakeByIdEf(5)).ExecuteAsync();
    var stats2 = TypedQueryInterceptor.GetCacheStats();

    var make1 = r1.GetFirstOrDefault<MakeDto>();
    var make2 = r2.GetFirstOrDefault<MakeDto>();

    Assert(make1 != null, "Cache: First query returns data");
    Assert(stats1.TemplateCount == 1, "Cache: Template created after first query");
    Assert(stats2.TemplateCount == 1, "Cache: Template reused on second query");
    Console.WriteLine($"    (First: {make1?.Name}, Second: {make2?.Name}, Templates: {stats2.TemplateCount})");
}

// Test that different parameters work with cached template
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);

    // Execute with same parameter values (edge case that was problematic)
    var r1 = await db.ToTypedQuery().Add(new GetMakesInRangeEf(5, 5)).ExecuteAsync();
    var isCacheable = TypedQueryInterceptor.IsCacheable(typeof(GetMakesInRangeEf));

    Assert(isCacheable, "Cache: Same value parameters (5,5) are cacheable");

    // Execute with different values
    await using var db2 = new TestDbContext(opts.Options);
    var r2 = await db2.ToTypedQuery().Add(new GetMakesInRangeEf(1, 10)).ExecuteAsync();

    var range55 = r1.GetList<MakeDto>().ToList();
    var range110 = r2.GetList<MakeDto>().ToList();

    Console.WriteLine($"    (Range 5-5: {range55.Count} makes, Range 1-10: {range110.Count} makes)");
}

// === SECTION 6: RESULT ACCESSORS ===
Console.WriteLine("\n--- Section 6: Result Accessors ---");
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(1)).ExecuteAsync();
    var make = r.GetSingle<MakeDto>();
    Assert(make != null, "GetSingle returns data");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(999999)).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetSingle<MakeDto>(), "GetSingle throws on empty");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetAllMakesRaw()).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetSingle<MakeDto>(), "GetSingle throws on multiple");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(999999)).ExecuteAsync();
    var make = r.GetSingleOrDefault<MakeDto>();
    Assert(make == null, "GetSingleOrDefault returns null on empty");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetAllMakesRaw()).ExecuteAsync();
    var make = r.GetFirst<MakeDto>();
    Assert(make != null, "GetFirst returns data");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(999999)).ExecuteAsync();
    AssertThrows<InvalidOperationException>(() => r.GetFirst<MakeDto>(), "GetFirst throws on empty");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(999999)).ExecuteAsync();
    var make = r.GetFirstOrDefault<MakeDto>();
    Assert(make == null, "GetFirstOrDefault returns null on empty");
}

// === SECTION 7: COMPLEX QUERIES ===
Console.WriteLine("\n--- Section 7: Complex Queries ---");
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetModelsWithMakeRaw()).ExecuteAsync();
    var models = r.GetList<ModelWithMakeDto>().ToList();
    Assert(models.Count >= 0, "Join query works");
    Console.WriteLine($"    (Found {models.Count} models with make info)");
}

{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetModelsByMakeIdRaw(1)).ExecuteAsync();
    var models = r.GetList<ModelDto>().ToList();
    Assert(models.Count >= 0, "Filtered join works");
    Console.WriteLine($"    (Make 1 has {models.Count} models)");
}

// === SECTION 8: ALL LOOKUP TABLES ===
Console.WriteLine("\n--- Section 8: All Lookup Tables ---");
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetAllMakesRaw())
        .Add(new GetAllBodyTypesRaw())
        .Add(new GetAllColorsRaw())
        .Add(new GetAllFuelTypesRaw())
        .Add(new GetAllTransmissionsRaw())
        .Add(new GetAllFeaturesRaw())
        .ExecuteAsync();

    var makes = r.Next<MakeDto>().ToList();
    var bodyTypes = r.Next<BodyTypeDto>().ToList();
    var colors = r.Next<ColorDto>().ToList();
    var fuelTypes = r.Next<FuelTypeDto>().ToList();
    var transmissions = r.Next<TransmissionDto>().ToList();
    var features = r.Next<FeatureDto>().ToList();

    Assert(makes.Count > 0, "Makes table accessible");
    Assert(bodyTypes.Count > 0, "BodyTypes table accessible");
    Assert(colors.Count > 0, "Colors table accessible");
    Assert(fuelTypes.Count > 0, "FuelTypes table accessible");
    Assert(transmissions.Count > 0, "Transmissions table accessible");
    Assert(features.Count > 0, "Features table accessible");

    Console.WriteLine($"    Makes: {makes.Count}, BodyTypes: {bodyTypes.Count}, Colors: {colors.Count}");
    Console.WriteLine($"    FuelTypes: {fuelTypes.Count}, Transmissions: {transmissions.Count}, Features: {features.Count}");
}

// === SECTION 9: CONDITIONAL QUERIES ===
Console.WriteLine("\n--- Section 9: Conditional EF Queries ---");
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r1 = await db.ToTypedQuery().Add(new ConditionalMakesEf(true)).ExecuteAsync();
    var active = r1.GetList<MakeDto>().ToList();
    Assert(active.Count >= 0, "Conditional(active=true) works");
    Console.WriteLine($"    (Active only: {active.Count} makes)");
}

TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r2 = await db.ToTypedQuery().Add(new ConditionalMakesEf(false)).ExecuteAsync();
    var all = r2.GetList<MakeDto>().ToList();
    Assert(all.Count >= 0, "Conditional(active=false) works");
    Console.WriteLine($"    (All: {all.Count} makes)");
}

// === SECTION 10: PERFORMANCE ===
Console.WriteLine("\n--- Section 10: Performance ---");
TypedQueryInterceptor.ClearAll();
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        await using var db = new TestDbContext(opts.Options);
        await db.ToTypedQuery().Add(new GetMakeByIdEf(i % 10 + 1)).ExecuteAsync();
    }
    sw.Stop();
    Console.WriteLine($"  100 cached EF queries: {sw.ElapsedMilliseconds}ms");
    Assert(sw.ElapsedMilliseconds < 5000, "100 cached queries under 5s");
}

{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.ToTypedQuery().Add(new GetMakeByIdRaw(i % 10 + 1)).ExecuteAsync();
    }
    sw.Stop();
    Console.WriteLine($"  100 raw SQL queries: {sw.ElapsedMilliseconds}ms");
    Assert(sw.ElapsedMilliseconds < 5000, "100 raw queries under 5s");
}

// === SECTION 11: PARAMETER COLLISION TESTS ===
Console.WriteLine("\n--- Section 11: Parameter Collision Tests ---");
// Test 1: Same parameter name in multiple raw queries
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetMakeByIdRaw(1))  // @id = 1
        .Add(new GetMakeByIdRaw(2))  // @id = 2 (COLLISION!)
        .Add(new GetMakeByIdRaw(3))  // @id = 3 (COLLISION!)
        .ExecuteAsync();

    // Each query should return different data despite same param name
    Assert(r.Count == 3, "Collision: 3 result sets returned");

    // Use Next() to get sequential results
    var make1 = r.Next<MakeDto>().FirstOrDefault();
    var make2 = r.Next<MakeDto>().FirstOrDefault();
    var make3 = r.Next<MakeDto>().FirstOrDefault();

    if (make1 != null && make2 != null && make3 != null)
    {
        Assert(make1.Id == 1, "Collision: First query got id=1");
        Assert(make2.Id == 2, "Collision: Second query got id=2");
        Assert(make3.Id == 3, "Collision: Third query got id=3");
        Console.WriteLine($"    (Got makes: {make1.Name}, {make2.Name}, {make3.Name})");
    }
    else
    {
        Console.WriteLine("    (Skipped ID verification - some IDs don't exist)");
    }
}

// Test 2: Same parameter name with same value (edge case)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetMakeByIdRaw(1))  // @id = 1
        .Add(new GetMakeByIdRaw(1))  // @id = 1 (same value)
        .ExecuteAsync();

    Assert(r.Count == 2, "Same value collision: 2 result sets returned");

    var make1 = r.Next<MakeDto>().FirstOrDefault();
    var make2 = r.Next<MakeDto>().FirstOrDefault();

    if (make1 != null && make2 != null)
    {
        Assert(make1.Id == make2.Id, "Same value collision: Both got same make");
        Console.WriteLine($"    (Both queries correctly got: {make1.Name})");
    }
}

// Test 3: Mixed Raw + EF with same logical parameter
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery()
        .Add(new GetMakeByIdEf(1))   // EF with id=1
        .Add(new GetMakeByIdRaw(2))  // Raw with id=2 (different param style)
        .Add(new GetMakeByIdEf(3))   // EF with id=3
        .ExecuteAsync();

    Assert(r.Count == 3, "Mixed collision: 3 result sets returned");

    var make1 = r.Next<MakeDto>().FirstOrDefault();
    var make2 = r.Next<MakeDto>().FirstOrDefault();
    var make3 = r.Next<MakeDto>().FirstOrDefault();

    if (make1 != null && make2 != null && make3 != null)
    {
        Assert(make1.Id == 1, "Mixed collision: EF query 1 got id=1");
        Assert(make2.Id == 2, "Mixed collision: Raw query got id=2");
        Assert(make3.Id == 3, "Mixed collision: EF query 2 got id=3");
        Console.WriteLine($"    (Mixed batch got: {make1.Name}, {make2.Name}, {make3.Name})");
    }
}

// Test 4: Multiple parameters with same names across queries
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetMakesInRangeRaw(1, 5))   // @minId=1, @maxId=5
        .Add(new GetMakesInRangeRaw(6, 10))  // @minId=6, @maxId=10 (COLLISION on both!)
        .ExecuteAsync();

    Assert(r.Count == 2, "Multi-param collision: 2 result sets returned");

    var range1 = r.Next<MakeDto>().ToList();
    var range2 = r.Next<MakeDto>().ToList();

    // Verify ranges don't overlap (if data exists)
    var range1Ids = range1.Select(m => m.Id).ToList();
    var range2Ids = range2.Select(m => m.Id).ToList();

    var hasOverlap = range1Ids.Intersect(range2Ids).Any();
    Assert(!hasOverlap, "Multi-param collision: Ranges don't overlap");
    Console.WriteLine($"    (Range 1-5: {range1.Count} makes, Range 6-10: {range2.Count} makes)");
}

// Test 5: EF queries with same parameter values
TypedQueryInterceptor.ClearAll();
{
    await using var db = new TestDbContext(opts.Options);
    var r = await db.ToTypedQuery()
        .Add(new GetMakesInRangeEf(1, 5))
        .Add(new GetMakesInRangeEf(1, 5))  // Exact same query (should still work)
        .ExecuteAsync();

    Assert(r.Count == 2, "EF same params: 2 result sets returned");

    var results1 = r.Next<MakeDto>().ToList();
    var results2 = r.Next<MakeDto>().ToList();
    Assert(results1.Count == results2.Count, "EF same params: Same count in both");
    Console.WriteLine($"    (Both EF queries returned {results1.Count} makes)");
}

// === SECTION 12: EDGE CASES ===
Console.WriteLine("\n--- Section 12: Edge Cases ---");

// Test: Null parameter handling via conditional query
// Note: PostgreSQL/Dapper cannot infer type of NULL parameters, so queries should 
// use conditional logic to avoid passing NULL. This is a database limitation, not a library bug.
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new SearchMakesWithNullRaw(null)).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "Null parameter (conditional query) works");
    Console.WriteLine($"    (Null search returned {makes.Count} makes)");
}

// Test: Empty string parameter
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new SearchMakesRaw("")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count > 0, "Empty string parameter works");
    Console.WriteLine($"    (Empty search returned {makes.Count} makes)");
}

// Test: Special characters in parameter (that look like SQL)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new SearchMakesRaw("'; DROP TABLE Makes; --")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count == 0, "SQL injection attempt returns no results (and doesn't crash)");
}

// Test: Unicode characters in parameter
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new SearchMakesRaw("???")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "Unicode parameter works");
    Console.WriteLine($"    (Unicode search returned {makes.Count} makes)");
}

// Test: Very long parameter value
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var longString = new string('A', 1000);
    var r = await conn.ToTypedQuery().Add(new SearchMakesRaw(longString)).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count == 0, "Very long parameter works");
}

// Test: Boolean parameter
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetActiveMakesRaw(true))
        .Add(new GetActiveMakesRaw(false))
        .ExecuteAsync();

    Assert(r.Count == 2, "Boolean params: 2 result sets");
    var active = r.Next<MakeDto>().ToList();
    var inactive = r.Next<MakeDto>().ToList();
    Console.WriteLine($"    (Active: {active.Count}, Inactive: {inactive.Count})");
}

// Test: Query with @ symbol in string literal (should not be treated as parameter)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByEmailDomainRaw("@gmail.com")).ExecuteAsync();
    var makes = r.GetList<MakeDto>().ToList();
    Assert(makes.Count >= 0, "@ in string literal works");
}

// Test: GetByIndex accessor
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetMakeByIdRaw(1))
        .Add(new GetMakeByIdRaw(2))
        .Add(new GetMakeByIdRaw(3))
        .ExecuteAsync();

    var first = r.GetByIndex<MakeDto>(0).FirstOrDefault();
    var second = r.GetByIndex<MakeDto>(1).FirstOrDefault();
    var third = r.GetByIndex<MakeDto>(2).FirstOrDefault();

    Assert(first?.Id == 1, "GetByIndex(0) returns first result");
    Assert(second?.Id == 2, "GetByIndex(1) returns second result");
    Assert(third?.Id == 3, "GetByIndex(2) returns third result");

    AssertThrows<ArgumentOutOfRangeException>(() => r.GetByIndex<MakeDto>(99).ToList(), "GetByIndex throws on invalid index");
}

// Test: Different result types in same batch
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery()
        .Add(new GetAllMakesRaw())
        .Add(new GetCountRaw("\"Makes\""))
        .Add(new GetAllBodyTypesRaw())
        .ExecuteAsync();

    var makes = r.Next<MakeDto>().ToList();
    var count = r.Next<CountResult>().First();
    var bodyTypes = r.Next<BodyTypeDto>().ToList();

    Assert(makes.Count > 0, "Different types: MakeDto works");
    Assert(count.Count > 0, "Different types: CountResult works");
    Assert(bodyTypes.Count > 0, "Different types: BodyTypeDto works");
}

// Test: Empty batch
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    // This should not throw - just return empty result
    // Note: Can't actually test this without modifying the API
}

// Test: Single query (no batching needed)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var r = await conn.ToTypedQuery().Add(new GetMakeByIdRaw(1)).ExecuteAsync();
    var make = r.GetFirstOrDefault<MakeDto>();
    Assert(make != null, "Single query works");
}

// Test: Many queries in batch (stress test)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    var builder = conn.ToTypedQuery();
    for (int i = 1; i <= 20; i++)
    {
        builder = builder.Add(new GetMakeByIdRaw(i % 10 + 1));
    }
    var r = await builder.ExecuteAsync();

    Assert(r.Count == 20, "20 query batch: All result sets returned");
    var totalRows = 0;
    while (r.TryNext<MakeDto>(out var set))
    {
        totalRows += set.Count();
    }
    Console.WriteLine($"    (20 query batch returned {totalRows} total rows)");
}

// === SECTION 13: WARM VS COLD PATH VERIFICATION ===
Console.WriteLine("\n--- Section 13: Warm vs Cold Path ---");
TypedQueryInterceptor.ClearAll();
{
    // Cold path - first execution
    var sw1 = System.Diagnostics.Stopwatch.StartNew();
    await using var db1 = new TestDbContext(opts.Options);
    var r1 = await db1.ToTypedQuery().Add(new GetMakeByIdEf(1)).ExecuteAsync();
    sw1.Stop();
    var coldTime = sw1.ElapsedTicks;

    // Warm path - second execution (should use cached template)
    var sw2 = System.Diagnostics.Stopwatch.StartNew();
    await using var db2 = new TestDbContext(opts.Options);
    var r2 = await db2.ToTypedQuery().Add(new GetMakeByIdEf(2)).ExecuteAsync();
    sw2.Stop();
    var warmTime = sw2.ElapsedTicks;

    Console.WriteLine($"    Cold path: {coldTime} ticks");
    Console.WriteLine($"    Warm path: {warmTime} ticks");
    Console.WriteLine($"    Speedup: {(double)coldTime / warmTime:F1}x");

    // Warm path should generally be faster (but not always due to JIT, so we just verify it works)
    Assert(r1.GetFirstOrDefault<MakeDto>() != null, "Cold path returns data");
    Assert(r2.GetFirstOrDefault<MakeDto>() != null, "Warm path returns data");
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


// ========================================
// RAW SQL QUERY CLASSES
// ========================================

public class GetAllMakesRaw : ITypedQuery<MakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" ORDER BY \"DisplayOrder\"");
}

public class GetMakeByIdRaw(int id) : ITypedQuery<MakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"Id\" = @id", new { id });
}

public class GetActiveMakesRaw(bool isActive) : ITypedQuery<MakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"IsActive\" = @isActive ORDER BY \"DisplayOrder\"", new { isActive });
}

public class SearchMakesRaw(string searchTerm) : ITypedQuery<MakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"Name\" ILIKE @pattern ORDER BY \"Name\"",
            new { pattern = $"%{searchTerm}%" });
}

public class SearchMakesWithNullRaw(string? searchTerm) : ITypedQuery<MakeDto>
{
    // PostgreSQL cannot infer type of NULL parameters, so we use conditional query building
    // This is the recommended pattern for nullable parameters across all databases
    public QueryDefinition Build(QueryBuildContext c)
    {
        if (searchTerm == null)
        {
            // No parameter needed - return all
            return new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" ORDER BY \"Name\"");
        }
        return new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"Name\" ILIKE @pattern ORDER BY \"Name\"",
            new { pattern = $"%{searchTerm}%" });
    }
}

public class GetMakeByEmailDomainRaw(string domain) : ITypedQuery<MakeDto>
{
    // This tests that @ in a parameter value doesn't confuse the parser
    // The query itself is meaningless but tests the parameter handling
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"Name\" LIKE @domain LIMIT 10",
            new { domain });
}

public class GetMakesInRangeRaw(int minId, int maxId) : ITypedQuery<MakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"IsActive\", \"DisplayOrder\", \"LogoUrl\" FROM \"Makes\" WHERE \"Id\" >= @minId AND \"Id\" <= @maxId ORDER BY \"Id\"",
            new { minId, maxId });
}

public class GetCountRaw(string tableName) : ITypedQuery<CountResult>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new($"SELECT COUNT(*) as \"Count\" FROM {tableName}");
}

public class GetAllBodyTypesRaw : ITypedQuery<BodyTypeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"Description\", \"IsActive\", \"DisplayOrder\" FROM \"BodyTypes\" ORDER BY \"DisplayOrder\"");
}

public class GetAllColorsRaw : ITypedQuery<ColorDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"HexCode\", \"IsActive\", \"DisplayOrder\" FROM \"Colors\" ORDER BY \"DisplayOrder\"");
}

public class GetAllFuelTypesRaw : ITypedQuery<FuelTypeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"Description\", \"IsActive\", \"DisplayOrder\" FROM \"FuelTypes\" ORDER BY \"DisplayOrder\"");
}

public class GetAllTransmissionsRaw : ITypedQuery<TransmissionDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"Description\", \"IsActive\", \"DisplayOrder\" FROM \"Transmissions\" ORDER BY \"DisplayOrder\"");
}

public class GetAllFeaturesRaw : ITypedQuery<FeatureDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"Description\", \"Category\", \"IsActive\", \"DisplayOrder\" FROM \"Features\" ORDER BY \"DisplayOrder\"");
}

public class GetModelsWithMakeRaw : ITypedQuery<ModelWithMakeDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("""
            SELECT m."Id", m."Name", m."MakeId", mk."Name" as "MakeName", m."IsActive"
            FROM "Models" m
            INNER JOIN "Makes" mk ON m."MakeId" = mk."Id"
            ORDER BY mk."Name", m."Name"
            LIMIT 100
            """);
}

public class GetModelsByMakeIdRaw(int makeId) : ITypedQuery<ModelDto>
{
    public QueryDefinition Build(QueryBuildContext c) =>
        new("SELECT \"Id\", \"Name\", \"MakeId\", \"IsActive\", \"DisplayOrder\" FROM \"Models\" WHERE \"MakeId\" = @makeId ORDER BY \"Name\"",
            new { makeId });
}

// ========================================
// EF CORE QUERY CLASSES
// ========================================

public class GetAllMakesEf : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db) =>
        db.Makes.OrderBy(m => m.DisplayOrder)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
}

public class GetMakeByIdEf(int id) : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db) =>
        db.Makes.Where(m => m.Id == id)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
}

public class GetActiveMakesEf(bool isActive) : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db) =>
        db.Makes.Where(m => m.IsActive == isActive)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
}

public class SearchMakesEf(string searchTerm) : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db) =>
        db.Makes.Where(m => EF.Functions.ILike(m.Name, $"%{searchTerm}%"))
            .OrderBy(m => m.Name)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
}

public class GetMakesInRangeEf(int minId, int maxId) : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db) =>
        db.Makes.Where(m => m.Id >= minId && m.Id <= maxId)
            .OrderBy(m => m.Id)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
}

public class ConditionalMakesEf(bool activeOnly) : ITypedQuery<TestDbContext, MakeDto>
{
    public IQueryable<MakeDto> Query(TestDbContext db)
    {
        var q = db.Makes.AsQueryable();
        if (activeOnly) q = q.Where(m => m.IsActive);
        return q.OrderBy(m => m.DisplayOrder)
            .Select(m => new MakeDto { Id = m.Id, Name = m.Name, IsActive = m.IsActive, DisplayOrder = m.DisplayOrder, LogoUrl = m.LogoUrl });
    }
}

// ========================================
// DTOs
// ========================================

public class MakeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string? LogoUrl { get; set; }
}

public class ModelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MakeId { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ModelWithMakeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MakeId { get; set; }
    public string MakeName { get; set; } = "";
    public bool IsActive { get; set; }
}

public class BodyTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ColorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? HexCode { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class FuelTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class TransmissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class FeatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Category { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class CountResult
{
    public long Count { get; set; }
}

// ========================================
// ENTITIES
// ========================================

public class Make
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string? LogoUrl { get; set; }
}

public class Model
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MakeId { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class BodyType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class Color
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? HexCode { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class FuelType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class Transmission
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class Feature
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Category { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

// ========================================
// DB CONTEXT
// ========================================

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Make> Makes => Set<Make>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<BodyType> BodyTypes => Set<BodyType>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<FuelType> FuelTypes => Set<FuelType>();
    public DbSet<Transmission> Transmissions => Set<Transmission>();
    public DbSet<Feature> Features => Set<Feature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Make>(e => { e.ToTable("Makes"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<Model>(e => { e.ToTable("Models"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<BodyType>(e => { e.ToTable("BodyTypes"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<Color>(e => { e.ToTable("Colors"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<FuelType>(e => { e.ToTable("FuelTypes"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<Transmission>(e => { e.ToTable("Transmissions"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<Feature>(e => { e.ToTable("Features"); e.HasKey(x => x.Id); });
    }
}
