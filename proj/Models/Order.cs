public enum OrderStatus
{
    Open,
    Paid,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public int WaiterId { get; set; }
    public Waiter Waiter { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public List<OrderItem> Items { get; set; } = new();
}