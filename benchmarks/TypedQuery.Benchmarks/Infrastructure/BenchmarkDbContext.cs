using Microsoft.EntityFrameworkCore;
using TypedQuery.EntityFrameworkCore;

namespace TypedQuery.Benchmarks.Infrastructure;

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }
    
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure indexes for realistic query performance
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.IsActive);
            entity.HasIndex(p => new { p.CategoryId, p.IsActive });
        });
        
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.OrderDate);
            entity.HasIndex(o => o.Status);
        });
        
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductId);
        });
        
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
        });
    }
}
