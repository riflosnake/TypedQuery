using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.Benchmarks.Queries;

namespace TypedQuery.Benchmarks.Benchmarks;

/// <summary>
/// Real-world application scenarios demonstrating TypedQuery's practical benefits.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class RealWorldScenarioBenchmarks
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
    
    // Scenario 1: Dashboard Load - Multiple lookup tables
    
    [Benchmark(Baseline = true)]
    public async Task<object> Scenario1_Dashboard_Dapper_Sequential()
    {
        var products = await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1");
        var categories = await _connection.QueryAsync<CategoryDto>(
            "SELECT Id, Name, DisplayOrder FROM Categories WHERE IsActive = 1");
        var customers = await _connection.QueryAsync<CustomerDto>(
            "SELECT Id, FirstName, LastName, Email FROM Customers");
        var stats = await _connection.QueryFirstOrDefaultAsync<DashboardStatsDto>(
            @"SELECT 
                (SELECT COUNT(*) FROM Products WHERE IsActive = 1) as TotalProducts,
                (SELECT COUNT(*) FROM Categories WHERE IsActive = 1) as TotalCategories,
                (SELECT COUNT(*) FROM Customers) as TotalCustomers,
                (SELECT COUNT(*) FROM Orders) as TotalOrders");
        
        return new { products, categories, customers, stats };
    }
    
    [Benchmark]
    public async Task<object> Scenario1_Dashboard_Dapper_Batched()
    {
        var sql = @"
            SELECT Id, Name, Price FROM Products WHERE IsActive = 1;
            SELECT Id, Name, DisplayOrder FROM Categories WHERE IsActive = 1;
            SELECT Id, FirstName, LastName, Email FROM Customers;
            SELECT 
                (SELECT COUNT(*) FROM Products WHERE IsActive = 1) as TotalProducts,
                (SELECT COUNT(*) FROM Categories WHERE IsActive = 1) as TotalCategories,
                (SELECT COUNT(*) FROM Customers) as TotalCustomers,
                (SELECT COUNT(*) FROM Orders) as TotalOrders;
        ";
        
        using var gridReader = await _connection.QueryMultipleAsync(sql);
        
        var products = await gridReader.ReadAsync<ProductDto>();
        var categories = await gridReader.ReadAsync<CategoryDto>();
        var customers = await gridReader.ReadAsync<CustomerDto>();
        var stats = await gridReader.ReadFirstOrDefaultAsync<DashboardStatsDto>();
        
        return new { products, categories, customers, stats };
    }
    
    [Benchmark]
    public async Task<object> Scenario1_Dashboard_EfCore_Sequential()
    {
        var products = await _dbContext.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToListAsync();
        
        var categories = await _dbContext.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, DisplayOrder = c.DisplayOrder })
            .ToListAsync();
        
        var customers = await _dbContext.Customers
            .Select(c => new CustomerDto { Id = c.Id, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email })
            .ToListAsync();
        
        var stats = new DashboardStatsDto
        {
            TotalProducts = await _dbContext.Products.CountAsync(p => p.IsActive),
            TotalCategories = await _dbContext.Categories.CountAsync(c => c.IsActive),
            TotalCustomers = await _dbContext.Customers.CountAsync(),
            TotalOrders = await _dbContext.Orders.CountAsync()
        };
        
        return new { products, categories, customers, stats };
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> Scenario1_Dashboard_TypedQuery_Batched()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .Add(new GetAllCustomersQuery())
            .Add(new GetDashboardStatsQuery())
            .ExecuteAsync();
    }
    
    // Scenario 2: Order Details Page - Load order with related data
    
    [Benchmark]
    public async Task<object> Scenario2_OrderDetails_Dapper_Sequential()
    {
        int orderId = 100;
        
        var order = await _connection.QueryFirstOrDefaultAsync<OrderDetailDto>(
            @"SELECT o.Id, o.OrderDate, o.TotalAmount, o.Status,
                     c.FirstName || ' ' || c.LastName as CustomerName,
                     c.Email as CustomerEmail
              FROM Orders o
              INNER JOIN Customers c ON o.CustomerId = c.Id
              WHERE o.Id = @orderId",
            new { orderId });
        
        var items = await _connection.QueryAsync<OrderItemDto>(
            @"SELECT oi.Id, oi.OrderId, oi.ProductId, p.Name as ProductName,
                     oi.Quantity, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) as TotalPrice
              FROM OrderItems oi
              INNER JOIN Products p ON oi.ProductId = p.Id
              WHERE oi.OrderId = @orderId",
            new { orderId });
        
        return new { order, items };
    }
    
    [Benchmark]
    public async Task<object> Scenario2_OrderDetails_Dapper_Batched()
    {
        int orderId = 100;
        
        var sql = @"
            SELECT o.Id, o.OrderDate, o.TotalAmount, o.Status,
                   c.FirstName || ' ' || c.LastName as CustomerName,
                   c.Email as CustomerEmail
            FROM Orders o
            INNER JOIN Customers c ON o.CustomerId = c.Id
            WHERE o.Id = @orderId;
            
            SELECT oi.Id, oi.OrderId, oi.ProductId, p.Name as ProductName,
                   oi.Quantity, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) as TotalPrice
            FROM OrderItems oi
            INNER JOIN Products p ON oi.ProductId = p.Id
            WHERE oi.OrderId = @orderId;
        ";
        
        using var gridReader = await _connection.QueryMultipleAsync(sql, new { orderId });
        
        var order = await gridReader.ReadFirstOrDefaultAsync<OrderDetailDto>();
        var items = await gridReader.ReadAsync<OrderItemDto>();
        
        return new { order, items };
    }
    
    [Benchmark]
    public async Task<object> Scenario2_OrderDetails_EfCore_Sequential()
    {
        int orderId = 100;
        
        var order = await _dbContext.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CustomerName = o.Customer!.FirstName + " " + o.Customer.LastName,
                CustomerEmail = o.Customer.Email
            })
            .FirstOrDefaultAsync();
        
        var items = await _dbContext.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                ProductName = oi.Product!.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.Quantity * oi.UnitPrice
            })
            .ToListAsync();
        
        return new { order, items };
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> Scenario2_OrderDetails_TypedQuery_Batched()
    {
        int orderId = 100;
        
        return await _connection
            .ToTypedQuery()
            .Add(new GetOrderDetailQuery(orderId))
            .Add(new GetOrderItemsQuery(orderId))
            .ExecuteAsync();
    }
    
    // Scenario 3: Product Search with Filters - Main search + filter aggregations
    
    [Benchmark]
    public async Task<object> Scenario3_SearchWithFilters_Dapper_Sequential()
    {
        var products = await _connection.QueryAsync<ProductDto>(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1 ORDER BY Name LIMIT 50");
        
        var categories = await _connection.QueryAsync<CategoryDto>(
            "SELECT Id, Name, DisplayOrder FROM Categories WHERE IsActive = 1");
        
        var priceRanges = await _connection.QueryAsync<object>(
            @"SELECT 
                COUNT(CASE WHEN Price < 100 THEN 1 END) as Under100,
                COUNT(CASE WHEN Price BETWEEN 100 AND 500 THEN 1 END) as Between100And500,
                COUNT(CASE WHEN Price > 500 THEN 1 END) as Over500
              FROM Products WHERE IsActive = 1");
        
        return new { products, categories, priceRanges };
    }
    
    [Benchmark]
    public async Task<object> Scenario3_SearchWithFilters_Dapper_Batched()
    {
        var sql = @"
            SELECT Id, Name, Price FROM Products WHERE IsActive = 1 ORDER BY Name LIMIT 50;
            SELECT Id, Name, DisplayOrder FROM Categories WHERE IsActive = 1;
            SELECT 
                COUNT(CASE WHEN Price < 100 THEN 1 END) as Under100,
                COUNT(CASE WHEN Price BETWEEN 100 AND 500 THEN 1 END) as Between100And500,
                COUNT(CASE WHEN Price > 500 THEN 1 END) as Over500
            FROM Products WHERE IsActive = 1;
        ";
        
        using var gridReader = await _connection.QueryMultipleAsync(sql);
        
        var products = await gridReader.ReadAsync<ProductDto>();
        var categories = await gridReader.ReadAsync<CategoryDto>();
        var priceRanges = await gridReader.ReadAsync<object>();
        
        return new { products, categories, priceRanges };
    }
    
    [Benchmark]
    public async Task<TypedQueryResult> Scenario3_SearchWithFilters_TypedQuery_Batched()
    {
        return await _connection
            .ToTypedQuery()
            .Add(new GetActiveProductsQuery())
            .Add(new GetAllCategoriesQuery())
            .Add(new GetDashboardStatsQuery())
            .ExecuteAsync();
    }
}
