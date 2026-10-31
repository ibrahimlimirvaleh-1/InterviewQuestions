using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var mode = args.Length == 0 ? "eager" : args[0];
using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var sql = new List<string>();

var builder = new DbContextOptionsBuilder<CafeContext>()
    .UseSqlite(connection)
    .LogTo(sql.Add, new[] { RelationalEventId.CommandExecuted });
if (mode is "lazy" or "lazy-steps" or "nplus1-lazy")
    builder.UseLazyLoadingProxies();
var options = builder.Options;

await SeedData.CreateAsync(options);
sql.Clear();

await using var db = new CafeContext(options);
if (mode == "lazy-steps")
{
    await LazySteps.RunAsync(db, sql);
    return;
}
if (mode is "nplus1-lazy" or "nplus1-explicit" or "nplus1-eager")
{
    await LoadingComparison.RunAsync(mode, db, sql);
    return;
}
if (await QueryLessons.RunAsync(mode, db, sql)) return;
if (mode is not ("eager" or "explicit" or "lazy"))
    throw new ArgumentException("Unknown lesson: " + mode);
Order order;
if (mode == "eager")
    order = await db.Orders.Include(o => o.Items)
        .SingleAsync(o => o.Id == 42);
else
    order = await db.Orders.SingleAsync(o => o.Id == 42);

Console.WriteLine("Once SQL: " + sql.Count);
if (mode == "explicit")
    await db.Entry(order).Collection(o => o.Items).LoadAsync();

Console.WriteLine("Items: " + order.Items.Count);
Console.WriteLine("Sonra SQL: " + sql.Count);
foreach (var command in sql) Console.WriteLine(command);
