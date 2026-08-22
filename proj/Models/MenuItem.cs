public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Напитки, Горячее, Десерты...
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = new();
}