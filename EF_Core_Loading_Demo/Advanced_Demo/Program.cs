using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var sql = new List<string>();
var options = new DbContextOptionsBuilder<CafeContext>()
    .UseSqlite(connection)
    .LogTo(sql.Add, new[] { RelationalEventId.CommandExecuted }).Options;
await SeedData.CreateAsync(options);
await SeedExtras.AddAsync(options);
var results = new List<object>();
void Check(bool value, string message) { if (!value) throw new Exception(message); }
async Task Run(string name, Func<Task<object>> work)
{
    sql.Clear(); var detail = await work();
    results.Add(new { name, passed = true, detail, commands = sql.ToArray() });
    Console.WriteLine(name + " OK SQL=" + sql.Count);
    await File.WriteAllTextAsync("results.json", JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
}
await Run("then_include_product", async () =>
{
    await using var db = new CafeContext(options);
    var order = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).SingleAsync(o => o.Id == 42);
    var names = order.Items.OrderBy(i => i.Id).Select(i => i.Product!.Name).ToArray();
    Check(sql.Count == 1 && names.SequenceEqual(new[] { "Cay", "Tort" }), "ThenInclude mismatch");
    return new { names, queries = sql.Count };
});
await Run("sibling_collections_single", async () =>
{
    await using var db = new CafeContext(options);
    var order = await db.Orders.Include(o => o.Items).Include(o => o.Payments).AsSingleQuery().SingleAsync(o => o.Id == 42);
    using var count = connection.CreateCommand();
    count.CommandText = "SELECT COUNT(*) FROM Orders o LEFT JOIN OrderItems i ON o.Id=i.OrderId LEFT JOIN Payments p ON o.Id=p.OrderId WHERE o.Id=42";
    var rows = Convert.ToInt32(await count.ExecuteScalarAsync());
    Check(sql.Count == 1 && order.Items.Count == 2 && order.Payments.Count == 3 && rows == 6, "Sibling single mismatch");
    return new { items = order.Items.Count, payments = order.Payments.Count, joinedRows = rows, queries = sql.Count };
});
await Run("sibling_collections_split", async () =>
{
    await using var db = new CafeContext(options);
    var order = await db.Orders.Include(o => o.Items).Include(o => o.Payments).AsSplitQuery().SingleAsync(o => o.Id == 42);
    Check(sql.Count == 3 && order.Items.Count == 2 && order.Payments.Count == 3, "Sibling split mismatch");
    return new { items = order.Items.Count, payments = order.Payments.Count, queries = sql.Count };
});
await Run("root_pagination_split", async () =>
{
    await using var db = new CafeContext(options);
    var orders = await db.Orders.OrderBy(o => o.Id).Take(2).Include(o => o.Items).AsSplitQuery().ToListAsync();
    Check(sql.Count == 2 && orders.Select(o => o.Id).SequenceEqual(new[] { 42, 43 }), "Pagination mismatch");
    return new { orders = orders.Select(o => o.Id).ToArray(), queries = sql.Count };
});
Console.WriteLine("ADVANCED PASSED " + results.Count);
