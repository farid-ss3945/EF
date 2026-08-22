using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Cafe.Dto;
using Cafe.Services;

public class MainViewModel : ViewModelBase
{
    private readonly CafeService _service;

    // ---------- Меню ----------
    public ObservableCollection<MenuItemDto> Menu { get; } = new();

    private MenuItemDto? _selectedMenuItem;
    public MenuItemDto? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            Set(ref _selectedMenuItem, value);
            if (value != null)
            {
                EditItemName = value.Name;
                EditItemCategory = value.Category;
                EditItemPrice = value.Price;
                EditItemIsAvailable = value.IsAvailable;
            }
        }
    }

    private string _editItemName = "";
    public string EditItemName { get => _editItemName; set => Set(ref _editItemName, value); }

    private string _editItemCategory = "";
    public string EditItemCategory { get => _editItemCategory; set => Set(ref _editItemCategory, value); }

    private decimal _editItemPrice;
    public decimal EditItemPrice { get => _editItemPrice; set => Set(ref _editItemPrice, value); }

    private bool _editItemIsAvailable = true;
    public bool EditItemIsAvailable { get => _editItemIsAvailable; set => Set(ref _editItemIsAvailable, value); }

    public ICommand AddMenuItemCommand { get; }
    public ICommand SaveMenuItemCommand { get; }
    public ICommand DeleteMenuItemCommand { get; }
    public ICommand ClearMenuFormCommand { get; }

    // ---------- Новый заказ ----------
    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<Waiter> Waiters { get; } = new();
    public ObservableCollection<OrderLineItem> NewOrderLines { get; } = new();

    private Table? _selectedTable;
    public Table? SelectedTable { get => _selectedTable; set => Set(ref _selectedTable, value); }

    private Waiter? _selectedWaiter;
    public Waiter? SelectedWaiter { get => _selectedWaiter; set => Set(ref _selectedWaiter, value); }

    private decimal _newOrderTotal;
    public decimal NewOrderTotal { get => _newOrderTotal; set => Set(ref _newOrderTotal, value); }

    public ICommand CreateOrderCommand { get; }

    // ---------- Заказы ----------
    public ObservableCollection<OrderSummaryDto> Orders { get; } = new();
    public ObservableCollection<OrderItemDetailDto> SelectedOrderItems { get; } = new();

    private OrderSummaryDto? _selectedOrder;
    public OrderSummaryDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            Set(ref _selectedOrder, value);
            NewStatus = value?.Status ?? "Open";
            _ = LoadSelectedOrderItemsAsync();
        }
    }

    private string _newStatus = "Open";
    public string NewStatus { get => _newStatus; set => Set(ref _newStatus, value); }

    public ICommand RefreshOrdersCommand { get; }
    public ICommand SaveStatusCommand { get; }

    // ---------- Статистика ----------
    public ObservableCollection<PopularDishDto> PopularDishes { get; } = new();
    public ObservableCollection<RevenueDto> Revenue { get; } = new();
    public ICommand LoadStatsCommand { get; }

    public MainViewModel(CafeService service)
    {
        _service = service;

        AddMenuItemCommand = new RelayCommand(async void (_) => await AddMenuItemAsync());
        SaveMenuItemCommand = new RelayCommand(async void (_) => await SaveMenuItemAsync(), _ => SelectedMenuItem != null);
        DeleteMenuItemCommand = new RelayCommand(async void (_) => await DeleteMenuItemAsync(), _ => SelectedMenuItem != null);
        ClearMenuFormCommand = new RelayCommand(_ => ClearMenuForm());

        CreateOrderCommand = new RelayCommand(async void (_) => await CreateOrderAsync(),
            _ => SelectedTable != null && SelectedWaiter != null && NewOrderLines.Any(l => l.IsSelected && l.Quantity > 0));

        RefreshOrdersCommand = new RelayCommand(async void (_) => await LoadOrdersAsync());
        SaveStatusCommand = new RelayCommand(async void (_) => await SaveStatusAsync(), _ => SelectedOrder != null);

        LoadStatsCommand = new RelayCommand(async void (_) => await LoadStatsAsync());

        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await LoadMenuAsync();
        await LoadOrdersAsync();
        await LoadTablesAndWaitersAsync();
    }

    // ---------- Меню: CRUD ----------

    private async Task LoadMenuAsync()
    {
        Menu.Clear();
        foreach (var item in await _service.GetMenuAsync())
            Menu.Add(item);

        // отдельно собираем строки для формы нового заказа
        NewOrderLines.Clear();
        foreach (var item in Menu.Where(m => m.IsAvailable))
        {
            var line = new OrderLineItem { MenuItemId = item.Id, Name = item.Name, Price = item.Price };
            line.PropertyChanged += (_, __) => RecalcNewOrderTotal();
            NewOrderLines.Add(line);
        }
    }

    private async Task AddMenuItemAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName)) return;
        await _service.AddMenuItemAsync(EditItemName, EditItemCategory, EditItemPrice);
        ClearMenuForm();
        await LoadMenuAsync();
    }

    private async Task SaveMenuItemAsync()
    {
        if (SelectedMenuItem == null) return;
        var updated = SelectedMenuItem with
        {
            Name = EditItemName,
            Category = EditItemCategory,
            Price = EditItemPrice,
            IsAvailable = EditItemIsAvailable
        };
        await _service.UpdateMenuItemAsync(updated);
        await LoadMenuAsync();
    }

    private async Task DeleteMenuItemAsync()
    {
        if (SelectedMenuItem == null) return;
        await _service.DeleteMenuItemAsync(SelectedMenuItem.Id);
        ClearMenuForm();
        await LoadMenuAsync();
    }

    private void ClearMenuForm()
    {
        SelectedMenuItem = null;
        EditItemName = "";
        EditItemCategory = "";
        EditItemPrice = 0;
        EditItemIsAvailable = true;
    }

    // ---------- Новый заказ ----------

    private async Task LoadTablesAndWaitersAsync()
    {
        Tables.Clear();
        foreach (var t in await _service.GetTablesAsync()) Tables.Add(t);

        Waiters.Clear();
        foreach (var w in await _service.GetWaitersAsync()) Waiters.Add(w);
    }

    private void RecalcNewOrderTotal()
    {
        NewOrderTotal = NewOrderLines.Where(l => l.IsSelected).Sum(l => l.LineTotal);
    }

    private async Task CreateOrderAsync()
    {
        if (SelectedTable == null || SelectedWaiter == null) return;

        var items = NewOrderLines
            .Where(l => l.IsSelected && l.Quantity > 0)
            .Select(l => (l.MenuItemId, l.Quantity, l.Price))
            .ToList();

        if (items.Count == 0) return;

        await _service.CreateOrderAsync(SelectedTable.Id, SelectedWaiter.Id, items);

        // сброс формы
        foreach (var line in NewOrderLines)
        {
            line.IsSelected = false;
            line.Quantity = 1;
        }
        NewOrderTotal = 0;

        await LoadOrdersAsync();
    }

    // ---------- Заказы ----------

    private async Task LoadOrdersAsync()
    {
        Orders.Clear();
        foreach (var o in await _service.GetOrdersAsync())
            Orders.Add(o);
    }

    private async Task LoadSelectedOrderItemsAsync()
    {
        SelectedOrderItems.Clear();
        if (SelectedOrder == null) return;
        foreach (var i in await _service.GetOrderItemsAsync(SelectedOrder.OrderId))
            SelectedOrderItems.Add(i);
    }

    private async Task SaveStatusAsync()
    {
        if (SelectedOrder == null) return;
        if (!Enum.TryParse<OrderStatus>(NewStatus, out var status)) return;

        await _service.SetOrderStatusAsync(SelectedOrder.OrderId, status);
        await LoadOrdersAsync();
    }

    // ---------- Статистика ----------

    private async Task LoadStatsAsync()
    {
        PopularDishes.Clear();
        foreach (var d in await _service.GetPopularDishesAsync())
            PopularDishes.Add(d);

        Revenue.Clear();
        foreach (var r in await _service.GetRevenueByDayAsync(DateTime.Today.AddDays(-30), DateTime.Today))
            Revenue.Add(r);
    }
}