using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task CreateAsync(
        DbContextOptions<CafeContext> options)
    {
        await using var db = new CafeContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Orders.AddRange(
            new Order { Id = 42, CustomerName = "Aysel" },
            new Order { Id = 43, CustomerName = "Murad" },
            new Order { Id = 44, CustomerName = "Leyla" });

        db.OrderItems.AddRange(
            new OrderItem
            { Id = 101, OrderId = 42, ProductName = "Cay" },
            new OrderItem
            { Id = 102, OrderId = 42, ProductName = "Tort" },
            new OrderItem
            { Id = 103, OrderId = 43, ProductName = "Qehve" });

        await db.SaveChangesAsync();
    }
}
