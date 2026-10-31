using Microsoft.EntityFrameworkCore;

public static class QueryLessons
{
    public static async Task<bool> RunAsync(
        string mode, CafeContext db, List<string> sql)
    {
        switch (mode)
        {
            case "query":
            {
                IQueryable<Order> query = db.Orders
                    .Where(o => o.Id >= 43);
                query = query.Where(o => o.Id == 43);
                Console.WriteLine("Quruldu SQL: " + sql.Count);
                Console.WriteLine(query.ToQueryString());
                Console.WriteLine("SQL gosterildi: " + sql.Count);
                var rows = await query.ToListAsync();
                PrintRows(rows, db, sql);
                break;
            }
            case "enumerable":
            {
                IEnumerable<Order> source = db.Orders
                    .Where(o => o.Id >= 43);
                var query = source.Where(o => o.Id == 43);
                Console.WriteLine("Quruldu SQL: " + sql.Count);
                var rows = query.ToList();
                PrintRows(rows, db, sql);
                break;
            }
            case "boundary":
            {
                var query = db.Orders.Where(o => o.Id >= 43)
                    .AsEnumerable().Where(o => o.Id == 43);
                Console.WriteLine("Quruldu SQL: " + sql.Count);
                var rows = query.ToList();
                PrintRows(rows, db, sql);
                break;
            }
            case "foreach":
            {
                var query = db.Orders.Where(o => o.Id == 42);
                Console.WriteLine("Evvel SQL: " + sql.Count);
                foreach (var order in query)
                    Console.WriteLine("Id: " + order.Id);
                Console.WriteLine("Sonra SQL: " + sql.Count);
                break;
            }
            case "repeat":
            {
                var query = db.Orders.Where(o => o.Id >= 43);
                foreach (var order in query) { }
                Console.WriteLine("Ilk dovr SQL: " + sql.Count);
                foreach (var order in query) { }
                Console.WriteLine("Ikinci dovr SQL: " + sql.Count);
                var list = await query.ToListAsync();
                foreach (var order in list) { }
                foreach (var order in list) { }
                Console.WriteLine("List dovrleri SQL: " + sql.Count);
                break;
            }
            case "scalar":
            {
                var query = db.Orders.Where(o => o.Id >= 43);
                var count = await query.CountAsync();
                Console.WriteLine("Count: " + count);
                Console.WriteLine("Count SQL: " + sql.Count);
                Console.WriteLine("Entity: " +
                    db.ChangeTracker.Entries<Order>().Count());
                var exists = await query.AnyAsync();
                Console.WriteLine("Any: " + exists);
                Console.WriteLine("Any SQL: " + sql.Count);
                break;
            }
            case "asqueryable":
            {
                var list = await db.Orders.ToListAsync();
                var query = list.AsQueryable()
                    .Where(o => o.Id == 43);
                Console.WriteLine("Netice: " + query.Count());
                Console.WriteLine("SQL: " + sql.Count);
                Console.WriteLine("Yaddasdaki Order: " + list.Count);
                break;
            }
            case "local":
            {
                var query = db.Orders.Where(o => o.Id >= 43)
                    .AsEnumerable()
                    .Where(o => IsMurad(o.CustomerName));
                Console.WriteLine("Quruldu SQL: " + sql.Count);
                PrintRows(query.ToList(), db, sql);
                break;
            }
            case "translation":
            {
                try
                {
                    await db.Orders
                        .Where(o => IsMurad(o.CustomerName))
                        .ToListAsync();
                    throw new Exception("Expected translation failure");
                }
                catch (InvalidOperationException ex)
                    when (ex.Message.Contains("translated"))
                {
                    Console.WriteLine("SQL-e cevrilmedi");
                    Console.WriteLine("SQL: " + sql.Count);
                }
                break;
            }
            case "list-deferred":
            {
                var numbers = new List<int> { 1, 2 };
                IEnumerable<int> query = numbers.Where(n => n > 1);
                numbers.Add(3);
                Console.WriteLine("Netice: " + string.Join(",", query));
                Console.WriteLine("SQL: " + sql.Count);
                break;
            }
            case "async-stream":
            {
                var query = db.Orders.Where(o => o.Id >= 43)
                    .AsAsyncEnumerable();
                Console.WriteLine("Quruldu SQL: " + sql.Count);
                await foreach (var order in query)
                    Console.WriteLine("Id: " + order.Id);
                Console.WriteLine("Sonra SQL: " + sql.Count);
                break;
            }
            default:
                return false;
        }
        foreach (var command in sql) Console.WriteLine(command);
        return true;
    }

    private static bool IsMurad(string name) => name == "Murad";

    private static void PrintRows(
        List<Order> rows, CafeContext db, List<string> sql)
    {
        Console.WriteLine("Netice Id: " +
            string.Join(",", rows.Select(o => o.Id)));
        Console.WriteLine("SQL: " + sql.Count);
        Console.WriteLine("Entity: " +
            db.ChangeTracker.Entries<Order>().Count());
    }
}
