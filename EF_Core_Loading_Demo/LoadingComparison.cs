using Microsoft.EntityFrameworkCore;

public static class LoadingComparison
{
    public static async Task RunAsync(
        string mode, CafeContext db, List<string> sql)
    {
        IQueryable<Order> query = db.Orders.OrderBy(o => o.Id);
        if (mode == "nplus1-eager")
            query = query.Include(o => o.Items);
        var orders = await query.ToListAsync();
        Console.WriteLine("Orders SQL: " + sql.Count);

        foreach (var order in orders)
        {
            if (mode == "nplus1-explicit")
                await db.Entry(order).Collection(o => o.Items)
                    .LoadAsync();
            var count = order.Items.Count;
            Console.WriteLine($"Order {order.Id}: Items {count}; SQL {sql.Count}");
        }

        Console.WriteLine("Cemi SQL: " + sql.Count);
        var expected = mode == "nplus1-eager" ? 1 : 4;
        if (sql.Count != expected || orders.Count != 3 ||
            !orders.Select(o => o.Items.Count).SequenceEqual(new[] { 2, 1, 0 }))
            throw new Exception("Loading comparison mismatch");
        foreach (var command in sql) Console.WriteLine(command);
    }
}
