using TypedQuery.Abstractions;
using TypedQuery.Benchmarks.Infrastructure;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Queries;

// Simple EF Core queries with no parameters

public class GetActiveProductsEfQuery : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}

public class GetAllCategoriesEfQuery : ITypedQuery<BenchmarkDbContext, CategoryDto>
{
    public IQueryable<CategoryDto> Query(BenchmarkDbContext db)
    {
        return db.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, DisplayOrder = c.DisplayOrder });
    }
}

public class GetAllCustomersEfQuery : ITypedQuery<BenchmarkDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(BenchmarkDbContext db)
    {
        return db.Customers
            .Select(c => new CustomerDto 
            { 
                Id = c.Id, 
                FirstName = c.FirstName, 
                LastName = c.LastName, 
                Email = c.Email 
            });
    }
}

// Parameterized EF Core queries

public class GetProductByIdEfQuery(int id) : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.Id == id)
            .Take(1)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}

public class GetProductsByCategoryEfQuery(int categoryId) : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}

public class GetProductsByPriceRangeEfQuery(decimal minPrice, decimal maxPrice) 
    : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice && p.IsActive)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}

public class GetCustomerByIdEfQuery(int id) : ITypedQuery<BenchmarkDbContext, CustomerDto>
{
    public IQueryable<CustomerDto> Query(BenchmarkDbContext db)
    {
        return db.Customers
            .Where(c => c.Id == id)
            .Take(1)
            .Select(c => new CustomerDto 
            { 
                Id = c.Id, 
                FirstName = c.FirstName, 
                LastName = c.LastName, 
                Email = c.Email 
            });
    }
}

public class GetOrdersByCustomerEfQuery(int customerId) : ITypedQuery<BenchmarkDbContext, OrderSummaryDto>
{
    public IQueryable<OrderSummaryDto> Query(BenchmarkDbContext db)
    {
        return db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer!.FirstName + " " + o.Customer.LastName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            });
    }
}

// Complex EF Core queries with joins

public class GetProductDetailsEfQuery(int productId) : ITypedQuery<BenchmarkDbContext, ProductDetailDto>
{
    public IQueryable<ProductDetailDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            });
    }
}

public class GetOrderDetailEfQuery(int orderId) : ITypedQuery<BenchmarkDbContext, OrderDetailDto>
{
    public IQueryable<OrderDetailDto> Query(BenchmarkDbContext db)
    {
        return db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CustomerName = o.Customer!.FirstName + " " + o.Customer.LastName,
                CustomerEmail = o.Customer.Email
            });
    }
}

public class GetOrderItemsEfQuery(int orderId) : ITypedQuery<BenchmarkDbContext, OrderItemDto>
{
    public IQueryable<OrderItemDto> Query(BenchmarkDbContext db)
    {
        return db.OrderItems
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
            });
    }
}

// Query with multiple parameters

public class GetProductsMultiFilterEfQuery(int categoryId, decimal minPrice, decimal maxPrice, bool isActive, int minStock) 
    : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.CategoryId == categoryId 
                     && p.Price >= minPrice 
                     && p.Price <= maxPrice 
                     && p.IsActive == isActive
                     && p.StockQuantity >= minStock)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}

public class GetProductsWith10ParamsEfQuery(
    int categoryId, decimal minPrice, decimal maxPrice, bool isActive, int minStock,
    string namePattern, DateTime fromDate, DateTime toDate, int offset, int limit) 
    : ITypedQuery<BenchmarkDbContext, ProductDto>
{
    public IQueryable<ProductDto> Query(BenchmarkDbContext db)
    {
        return db.Products
            .Where(p => p.CategoryId == categoryId 
                     && p.Price >= minPrice 
                     && p.Price <= maxPrice 
                     && p.IsActive == isActive
                     && p.StockQuantity >= minStock
                     && p.Name.Contains(namePattern)
                     && p.CreatedAt >= fromDate
                     && p.CreatedAt <= toDate)
            .OrderBy(p => p.Id)
            .Skip(offset)
            .Take(limit)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }
}
