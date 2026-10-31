public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = "";
    public virtual Order Order { get; set; } = null!;
    public int? ProductId { get; set; }
    public virtual Product? Product { get; set; }
}
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int Amount { get; set; }
    public virtual Order Order { get; set; } = null!;
}
