using Microsoft.EntityFrameworkCore;

public static class LazySteps
{
    public static async Task RunAsync(CafeContext db, List<string> sql)
    {
        var plain = new Order { Id = 42 };
        Console.WriteLine("New tip: " + plain.GetType().Name);
        Console.WriteLine("New Items: " + plain.Items.Count);
        Console.WriteLine("New SQL: " + sql.Count);

        var order = await db.Orders.SingleAsync(o => o.Id == 42);
        Console.WriteLine("EF tip: " + order.GetType().Name);
        Console.WriteLine("1) SQL: " + sql.Count);
        Console.WriteLine("1) Yuklenib: " +
            db.Entry(order).Collection(o => o.Items).IsLoaded);

        Console.WriteLine("2) Ad: " + order.CustomerName);
        Console.WriteLine("2) SQL: " + sql.Count);

        var items = order.Items;
        Console.WriteLine("3) SQL: " + sql.Count);
        Console.WriteLine("3) Yuklenib: " +
            db.Entry(order).Collection(o => o.Items).IsLoaded);

        Console.WriteLine("4) Items: " + items.Count);
        Console.WriteLine("4) SQL: " + sql.Count);

        var again = order.Items;
        Console.WriteLine("5) Eyni kolleksiya: " +
            ReferenceEquals(items, again));
        Console.WriteLine("5) SQL: " + sql.Count);

        if (plain.GetType() != typeof(Order) || plain.Items.Count != 0 ||
            !order.GetType().IsSubclassOf(typeof(Order)) ||
            items.Count != 2 || sql.Count != 2 ||
            !ReferenceEquals(items, again) ||
            sql[1].Contains("COUNT(", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Lazy loading behavior mismatch");

        foreach (var command in sql) Console.WriteLine(command);
    }
}
