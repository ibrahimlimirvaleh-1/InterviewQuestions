using Microsoft.EntityFrameworkCore;
public static class SeedExtras
{
    public static async Task AddAsync(DbContextOptions<CafeContext> options)
    {
        await using var db = new CafeContext(options);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Cay" },
            new Product { Id = 2, Name = "Tort" });
        db.Payments.AddRange(
            new Payment { Id = 1, OrderId = 42, Amount = 5 },
            new Payment { Id = 2, OrderId = 42, Amount = 10 },
            new Payment { Id = 3, OrderId = 42, Amount = 15 });
        await db.SaveChangesAsync();

        var tea = await db.OrderItems.SingleAsync(i => i.Id == 101);
        var cake = await db.OrderItems.SingleAsync(i => i.Id == 102);
        tea.ProductId = 1;
        cake.ProductId = 2;
        await db.SaveChangesAsync();
    }
}
