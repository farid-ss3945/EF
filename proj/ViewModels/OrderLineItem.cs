public class OrderLineItem : ViewModelBase
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { Set(ref _isSelected, value); OnPropertyChanged(nameof(LineTotal)); }
    }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { Set(ref value, value); Set(ref _quantity, value); OnPropertyChanged(nameof(LineTotal)); }
    }

    public decimal LineTotal => IsSelected ? Price * Quantity : 0m;
}