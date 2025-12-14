using Microsoft.Data.Sqlite;
using System.Data.Common;
using TypedQuery.Abstractions;
using TypedQuery.Benchmarks.Infrastructure;

namespace TypedQuery.Benchmarks.Queries;

// Simple queries with no parameters

public class GetActiveProductsQuery : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, Price FROM Products WHERE IsActive = 1",
            Array.Empty<DbParameter>());
    }
}

public class GetAllCategoriesQuery : ITypedQuery<CategoryDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, DisplayOrder FROM Categories ORDER BY DisplayOrder",
            Array.Empty<DbParameter>());
    }
}

public class GetAllCustomersQuery : ITypedQuery<CustomerDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, FirstName, LastName, Email FROM Customers",
            Array.Empty<DbParameter>());
    }
}

// Parameterized queries

public class GetProductByIdQuery(int id) : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, Price FROM Products WHERE Id = @id",
            new DbParameter[] { new SqliteParameter("@id", id) });
    }
}

public class GetProductsByCategoryQuery(int categoryId) : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, Price FROM Products WHERE CategoryId = @categoryId AND IsActive = 1",
            new DbParameter[] { new SqliteParameter("@categoryId", categoryId) });
    }
}

public class GetProductsByPriceRangeQuery(decimal minPrice, decimal maxPrice) : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, Name, Price FROM Products WHERE Price BETWEEN @minPrice AND @maxPrice AND IsActive = 1",
            new DbParameter[] 
            { 
                new SqliteParameter("@minPrice", minPrice),
                new SqliteParameter("@maxPrice", maxPrice)
            });
    }
}

public class GetCustomerByIdQuery(int id) : ITypedQuery<CustomerDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            "SELECT Id, FirstName, LastName, Email FROM Customers WHERE Id = @id",
            new DbParameter[] { new SqliteParameter("@id", id) });
    }
}

public class GetOrdersByCustomerQuery(int customerId) : ITypedQuery<OrderSummaryDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT o.Id, o.CustomerId, c.FirstName || ' ' || c.LastName as CustomerName, 
                     o.OrderDate, o.TotalAmount, o.Status
              FROM Orders o
              INNER JOIN Customers c ON o.CustomerId = c.Id
              WHERE o.CustomerId = @customerId
              ORDER BY o.OrderDate DESC",
            new DbParameter[] { new SqliteParameter("@customerId", customerId) });
    }
}

// Complex queries with joins

public class GetProductDetailsQuery(int productId) : ITypedQuery<ProductDetailDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT p.Id, p.Name, p.Description, p.Price, p.CategoryId, 
                     c.Name as CategoryName, p.StockQuantity, p.IsActive
              FROM Products p
              INNER JOIN Categories c ON p.CategoryId = c.Id
              WHERE p.Id = @productId",
            new DbParameter[] { new SqliteParameter("@productId", productId) });
    }
}

public class GetOrderDetailQuery(int orderId) : ITypedQuery<OrderDetailDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT o.Id, o.OrderDate, o.TotalAmount, o.Status,
                     c.FirstName || ' ' || c.LastName as CustomerName,
                     c.Email as CustomerEmail
              FROM Orders o
              INNER JOIN Customers c ON o.CustomerId = c.Id
              WHERE o.Id = @orderId",
            new DbParameter[] { new SqliteParameter("@orderId", orderId) });
    }
}

public class GetOrderItemsQuery(int orderId) : ITypedQuery<OrderItemDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT oi.Id, oi.OrderId, oi.ProductId, p.Name as ProductName,
                     oi.Quantity, oi.UnitPrice, (oi.Quantity * oi.UnitPrice) as TotalPrice
              FROM OrderItems oi
              INNER JOIN Products p ON oi.ProductId = p.Id
              WHERE oi.OrderId = @orderId",
            new DbParameter[] { new SqliteParameter("@orderId", orderId) });
    }
}

// Aggregation queries

public class GetDashboardStatsQuery : ITypedQuery<DashboardStatsDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT 
                (SELECT COUNT(*) FROM Products WHERE IsActive = 1) as TotalProducts,
                (SELECT COUNT(*) FROM Categories WHERE IsActive = 1) as TotalCategories,
                (SELECT COUNT(*) FROM Customers) as TotalCustomers,
                (SELECT COUNT(*) FROM Orders) as TotalOrders",
            Array.Empty<DbParameter>());
    }
}

// Query with multiple parameters (for parameter overhead testing)

public class GetProductsMultiFilterQuery(int categoryId, decimal minPrice, decimal maxPrice, bool isActive, int minStock) 
    : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT Id, Name, Price 
              FROM Products 
              WHERE CategoryId = @categoryId 
                AND Price BETWEEN @minPrice AND @maxPrice 
                AND IsActive = @isActive
                AND StockQuantity >= @minStock",
            new DbParameter[] 
            { 
                new SqliteParameter("@categoryId", categoryId),
                new SqliteParameter("@minPrice", minPrice),
                new SqliteParameter("@maxPrice", maxPrice),
                new SqliteParameter("@isActive", isActive ? 1 : 0),
                new SqliteParameter("@minStock", minStock)
            });
    }
}

public class GetProductsWith10ParamsQuery(
    int categoryId, decimal minPrice, decimal maxPrice, bool isActive, int minStock,
    string namePattern, DateTime fromDate, DateTime toDate, int offset, int limit) 
    : ITypedQuery<ProductDto>
{
    public QueryDefinition Build(QueryBuildContext context)
    {
        return new QueryDefinition(
            @"SELECT Id, Name, Price 
              FROM Products 
              WHERE CategoryId = @categoryId 
                AND Price BETWEEN @minPrice AND @maxPrice 
                AND IsActive = @isActive
                AND StockQuantity >= @minStock
                AND Name LIKE @namePattern
                AND CreatedAt BETWEEN @fromDate AND @toDate
              ORDER BY Id
              LIMIT @limit OFFSET @offset",
            new DbParameter[] 
            { 
                new SqliteParameter("@categoryId", categoryId),
                new SqliteParameter("@minPrice", minPrice),
                new SqliteParameter("@maxPrice", maxPrice),
                new SqliteParameter("@isActive", isActive ? 1 : 0),
                new SqliteParameter("@minStock", minStock),
                new SqliteParameter("@namePattern", namePattern),
                new SqliteParameter("@fromDate", fromDate),
                new SqliteParameter("@toDate", toDate),
                new SqliteParameter("@offset", offset),
                new SqliteParameter("@limit", limit)
            });
    }
}
