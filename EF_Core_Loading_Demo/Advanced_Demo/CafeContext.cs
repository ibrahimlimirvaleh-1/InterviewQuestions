using Microsoft.EntityFrameworkCore;
public class CafeContext : DbContext
{
    public CafeContext(DbContextOptions<CafeContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Payment> Payments => Set<Payment>();
}
