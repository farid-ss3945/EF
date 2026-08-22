public class Waiter
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public List<Order> Orders { get; set; } = new();
}