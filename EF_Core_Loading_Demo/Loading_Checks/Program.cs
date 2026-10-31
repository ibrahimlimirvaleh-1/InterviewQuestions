using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var sql = new List<string>();
DbContextOptions<CafeContext> Options(bool lazy = false)
{
    var builder = new DbContextOptionsBuilder<CafeContext>()
        .UseSqlite(connection)
        .LogTo(sql.Add, new[] { RelationalEventId.CommandExecuted });
    if (lazy) builder.UseLazyLoadingProxies();
    return builder.Options;
}
await SeedData.CreateAsync(Options());
var results = new List<object>();
void Check(bool condition, string reason)
{
    if (!condition) throw new Exception(reason);
}
async Task Run(string name, Func<Task<object>> work)
{
    sql.Clear();
    var detail = await work();
    results.Add(new { name, passed = true, detail, commands = sql.ToArray() });
    Console.WriteLine(name + " OK SQL=" + sql.Count);
    await File.WriteAllTextAsync("results.json",
        JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
}

await Run("deferred_execution", async () =>
{
    await using var db = new CafeContext(Options());
    var query = db.Orders.Where(o => o.Id == 42);
    Check(sql.Count == 0, "Building query executed SQL");
    var order = await query.SingleAsync();
    Check(sql.Count == 1, "Single did not execute exactly one command");
    return new { before = 0, after = sql.Count, order.Id };
});
await Run("no_loading", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    var loaded = db.Entry(order).Collection(o => o.Items).IsLoaded;
    Check(order.Items.Count == 0 && !loaded && sql.Count == 1, "Baseline mismatch");
    return new { items = order.Items.Count, loaded };
});
await Run("eager_one", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == 42);
    var before = sql.Count;
    var count = order.Items.Count;
    Check(before == 1 && sql.Count == 1 && count == 2, "Eager mismatch");
    Check(db.Entry(order).Collection(o => o.Items).IsLoaded, "Eager not loaded");
    return new { before, items = count, after = sql.Count };
});
await Run("explicit_one_and_repeat", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    var before = sql.Count;
    await db.Entry(order).Collection(o => o.Items).LoadAsync();
    var after = sql.Count;
    await db.Entry(order).Collection(o => o.Items).LoadAsync();
    Check(before == 1 && after == 2 && sql.Count == 2 && order.Items.Count == 2, "Explicit mismatch");
    return new { before, after, repeat = sql.Count, items = order.Items.Count };
});
await Run("reference_loading", async () =>
{
    await using var db = new CafeContext(Options());
    var item = await db.OrderItems.SingleAsync(i => i.Id == 101);
    Check(!db.Entry(item).Reference(i => i.Order).IsLoaded, "Reference already loaded");
    await db.Entry(item).Reference(i => i.Order).LoadAsync();
    Check(sql.Count == 2 && item.Order.Id == 42, "Reference mismatch");
    return new { item.OrderId, loaded = db.Entry(item).Reference(i => i.Order).IsLoaded };
});
await Run("lazy_one_and_repeat", async () =>
{
    await using var db = new CafeContext(Options(true));
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    var before = sql.Count;
    Check(!db.Entry(order).Collection(o => o.Items).IsLoaded, "Lazy already loaded");
    var count = order.Items.Count;
    var after = sql.Count;
    var again = order.Items.Count;
    Check(before == 1 && after == 2 && sql.Count == 2 && count == 2 && again == 2, "Lazy mismatch");
    return new { before, after, repeat = sql.Count, items = count };
});
await Run("empty_loaded_vs_unloaded", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 44);
    var before = db.Entry(order).Collection(o => o.Items).IsLoaded;
    Check(order.Items.Count == 0 && !before, "Empty initial mismatch");
    await db.Entry(order).Collection(o => o.Items).LoadAsync();
    var after = db.Entry(order).Collection(o => o.Items).IsLoaded;
    Check(order.Items.Count == 0 && after && sql.Count == 2, "Empty loaded mismatch");
    return new { items = order.Items.Count, before, after };
});
await Run("lazy_n_plus_one", async () =>
{
    await using var db = new CafeContext(Options(true));
    var orders = await db.Orders.OrderBy(o => o.Id).ToListAsync();
    Check(sql.Count == 1 && orders.Count == 3, "N+1 first query mismatch");
    var counts = orders.Select(o => o.Items.Count).ToArray();
    Check(sql.Count == 4 && counts.SequenceEqual(new[] { 2, 1, 0 }), "N+1 mismatch");
    return new { orderCount = orders.Count, counts, queries = sql.Count };
});
await Run("explicit_n_plus_one", async () =>
{
    await using var db = new CafeContext(Options());
    var orders = await db.Orders.OrderBy(o => o.Id).ToListAsync();
    foreach (var order in orders) await db.Entry(order).Collection(o => o.Items).LoadAsync();
    Check(sql.Count == 4, "Explicit N+1 mismatch");
    return new { counts = orders.Select(o => o.Items.Count).ToArray(), queries = sql.Count };
});
await Run("eager_list_single", async () =>
{
    await using var db = new CafeContext(Options());
    var orders = await db.Orders.Include(o => o.Items).AsSingleQuery().OrderBy(o => o.Id).ToListAsync();
    var counts = orders.Select(o => o.Items.Count).ToArray();
    Check(sql.Count == 1 && counts.SequenceEqual(new[] { 2, 1, 0 }), "Eager list mismatch");
    return new { counts, queries = sql.Count };
});
await Run("eager_list_split", async () =>
{
    await using var db = new CafeContext(Options());
    var orders = await db.Orders.Include(o => o.Items).AsSplitQuery().OrderBy(o => o.Id).ToListAsync();
    Check(sql.Count == 2 && orders.Count == 3, "Split mismatch");
    return new { counts = orders.Select(o => o.Items.Count).ToArray(), queries = sql.Count };
});
await Run("projection_count", async () =>
{
    await using var db = new CafeContext(Options());
    var rows = await db.Orders.OrderBy(o => o.Id)
        .Select(o => new { o.Id, ItemCount = o.Items.Count }).ToListAsync();
    Check(sql.Count == 1 && !db.ChangeTracker.Entries().Any(), "Projection mismatch");
    Check(rows.Select(o => o.ItemCount).SequenceEqual(new[] { 2, 1, 0 }), "Projection counts mismatch");
    return new { rows, tracked = db.ChangeTracker.Entries().Count() };
});
await Run("count_without_loading_collection", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    var count = await db.Entry(order).Collection(o => o.Items).Query().CountAsync();
    Check(count == 2 && order.Items.Count == 0 && sql.Count == 2, "Count query mismatch");
    Check(!db.Entry(order).Collection(o => o.Items).IsLoaded, "Count loaded navigation");
    return new { count, inMemoryItems = order.Items.Count, loaded = false };
});
await Run("navigation_fixup_partial", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    await db.OrderItems.Where(i => i.Id == 101).ToListAsync();
    Check(order.Items.Count == 1 && !db.Entry(order).Collection(o => o.Items).IsLoaded, "Fixup mismatch");
    var partial = order.Items.Count;
    await db.Entry(order).Collection(o => o.Items).LoadAsync();
    Check(order.Items.Count == 2 && sql.Count == 3, "Fixup completion mismatch");
    return new { partial, complete = order.Items.Count, queries = sql.Count };
});
await Run("filtered_include_fresh", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.Include(o => o.Items.Where(i => i.Id == 101)).SingleAsync(o => o.Id == 42);
    Check(order.Items.Count == 1 && db.Entry(order).Collection(o => o.Items).IsLoaded, "Filtered fresh mismatch");
    await db.Entry(order).Collection(o => o.Items).LoadAsync();
    Check(order.Items.Count == 1 && sql.Count == 1, "Filtered loaded did re-query");
    return new { items = order.Items.Count, loaded = true, queries = sql.Count };
});
await Run("filtered_include_with_fixup", async () =>
{
    await using var db = new CafeContext(Options());
    await db.OrderItems.Where(i => i.OrderId == 42).ToListAsync();
    var order = await db.Orders.Include(o => o.Items.Where(i => i.Id == 101)).SingleAsync(o => o.Id == 42);
    Check(order.Items.Count == 2 && sql.Count == 2, "Filtered fixup mismatch");
    return new { items = order.Items.Count, loaded = db.Entry(order).Collection(o => o.Items).IsLoaded };
});
await Run("eager_no_tracking", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.Id == 42);
    Check(order.Items.Count == 2 && !db.ChangeTracker.Entries().Any(), "No tracking mismatch");
    order.CustomerName = "Unsaved";
    var written = await db.SaveChangesAsync();
    Check(written == 0 && sql.Count == 1, "Untracked changes saved");
    return new { items = order.Items.Count, tracked = 0, written };
});
await Run("lazy_no_tracking_current_version", async () =>
{
    await using var db = new CafeContext(Options(true));
    var order = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == 42);
    Check(sql.Count == 1, "Lazy no-tracking root mismatch");
    var count = order.Items.Count;
    Check(count == 2 && sql.Count == 2 && !db.ChangeTracker.Entries().Any(), "Lazy no tracking mismatch");
    return new { items = count, tracked = 0, queries = sql.Count };
});
await Run("lazy_disposed_context", async () =>
{
    Order order;
    await using (var db = new CafeContext(Options(true)))
        order = await db.Orders.SingleAsync(o => o.Id == 42);
    string? error = null;
    try { _ = order.Items.Count; }
    catch (InvalidOperationException e) { error = e.Message; }
    Check(error is not null && sql.Count == 1, "Disposed lazy access did not fail as expected");
    return new { error, queries = sql.Count };
});
await Run("tracked_changes_saved", async () =>
{
    await using var db = new CafeContext(Options());
    var order = await db.Orders.SingleAsync(o => o.Id == 42);
    order.CustomerName = "Changed";
    var written = await db.SaveChangesAsync();
    Check(written == 1, "Tracked change not saved");
    return new { written };
});
Console.WriteLine("CORE PASSED " + results.Count);
