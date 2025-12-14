using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Infrastructure;

public enum DataSize
{
    Small,
    Medium,
    Large
}

public static class DatabaseSetup
{
    /// <summary>
    /// Creates a SQLite in-memory connection with shared cache.
    /// The connection must remain open for the in-memory database to persist.
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Mode=Memory;Cache=Shared");
        connection.Open();
        return connection;
    }
    
    /// <summary>
    /// Creates a BenchmarkDbContext using the provided SQLite connection.
    /// </summary>
    public static BenchmarkDbContext CreateDbContext(SqliteConnection connection)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite(connection);
        
        // UseTypedQuery returns non-generic DbContextOptionsBuilder, so we need to call it differently
        ((DbContextOptionsBuilder)optionsBuilder).UseTypedQuery();
        
        var options = optionsBuilder.Options;
        
        var context = new BenchmarkDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }
    
    /// <summary>
    /// Seeds the database with test data based on the specified size.
    /// </summary>
    public static void SeedDatabase(BenchmarkDbContext context, DataSize size)
    {
        var (categoryCount, productCount, customerCount, orderCount) = size switch
        {
            DataSize.Small => (10, 100, 50, 200),
            DataSize.Medium => (50, 1000, 500, 2000),
            DataSize.Large => (100, 10000, 5000, 20000),
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
        
        // Seed Categories
        var categories = new List<Category>();
        for (int i = 1; i <= categoryCount; i++)
        {
            categories.Add(new Category
            {
                Id = i,
                Name = $"Category {i}",
                DisplayOrder = i,
                IsActive = i % 10 != 0 // 90% active
            });
        }
        context.Categories.AddRange(categories);
        context.SaveChanges();
        
        // Seed Products
        var products = new List<Product>();
        var random = new Random(42); // Fixed seed for reproducibility
        
        for (int i = 1; i <= productCount; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Description = i % 5 == 0 ? $"Description for product {i}" : null,
                Price = (decimal)(random.NextDouble() * 1000 + 10),
                CategoryId = random.Next(1, categoryCount + 1),
                StockQuantity = random.Next(0, 1000),
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                IsActive = i % 10 != 0 // 90% active
            });
        }
        context.Products.AddRange(products);
        context.SaveChanges();
        
        // Seed Customers
        var customers = new List<Customer>();
        for (int i = 1; i <= customerCount; i++)
        {
            customers.Add(new Customer
            {
                Id = i,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"customer{i}@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 730))
            });
        }
        context.Customers.AddRange(customers);
        context.SaveChanges();
        
        // Seed Orders and OrderItems
        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();
        int orderItemId = 1;
        
        for (int i = 1; i <= orderCount; i++)
        {
            var orderDate = DateTime.UtcNow.AddDays(-random.Next(0, 365));
            var order = new Order
            {
                Id = i,
                CustomerId = random.Next(1, customerCount + 1),
                OrderDate = orderDate,
                TotalAmount = 0, // Will be calculated
                Status = (i % 4) switch
                {
                    0 => "Pending",
                    1 => "Processing",
                    2 => "Shipped",
                    _ => "Delivered"
                }
            };
            
            // Add 1-5 items per order
            int itemCount = random.Next(1, 6);
            decimal totalAmount = 0;
            
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[random.Next(products.Count)];
                var quantity = random.Next(1, 10);
                var unitPrice = product.Price;
                
                orderItems.Add(new OrderItem
                {
                    Id = orderItemId++,
                    OrderId = i,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                });
                
                totalAmount += unitPrice * quantity;
            }
            
            order.TotalAmount = totalAmount;
            orders.Add(order);
        }
        
        context.Orders.AddRange(orders);
        context.OrderItems.AddRange(orderItems);
        context.SaveChanges();
    }
}
