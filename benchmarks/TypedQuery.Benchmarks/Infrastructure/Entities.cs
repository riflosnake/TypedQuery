using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TypedQuery.Benchmarks.Infrastructure;

public class Product
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    public int CategoryId { get; set; }
    
    public int StockQuantity { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; }
    
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }
}

public class Category
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; }
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Customer
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Order
{
    [Key]
    public int Id { get; set; }
    
    public int CustomerId { get; set; }
    
    public DateTime OrderDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    [Key]
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }
    
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
