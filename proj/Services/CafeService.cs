using Cafe.Dto;

namespace Cafe.Services;

using Microsoft.EntityFrameworkCore;

public class CafeService
{
    private readonly IDbContextFactory<CafeDbContext> _factory;

    public CafeService(IDbContextFactory<CafeDbContext> factory)
    {
        _factory = factory;
    }
    
    public async Task<List<MenuItemDto>> GetMenuAsync(string? category = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.MenuItems.AsNoTracking(); 

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(m => m.Category == category); 
        return await query
            .OrderBy(m => m.Category).ThenBy(m => m.Name)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Category, m.Price, m.IsAvailable))
            .ToListAsync();
    }

    public async Task AddMenuItemAsync(string name, string category, decimal price)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.MenuItems.Add(new MenuItem { Name = name, Category = category, Price = price, IsAvailable = true });
        await db.SaveChangesAsync();
    }

    public async Task UpdateMenuItemAsync(MenuItemDto dto)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.MenuItems.FirstAsync(m => m.Id == dto.Id);
        item.Name = dto.Name;
        item.Category = dto.Category;
        item.Price = dto.Price;
        item.IsAvailable = dto.IsAvailable;
        await db.SaveChangesAsync();
    }

    public async Task DeleteMenuItemAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.MenuItems.Where(m => m.Id == id).ExecuteDeleteAsync();
    }

    public async Task<List<Table>> GetTablesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Tables.AsNoTracking()
            .OrderBy(t => t.Number)
            .ToListAsync();
    }

    public async Task<List<Waiter>> GetWaitersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Waiters.AsNoTracking()
            .OrderBy(w => w.FullName)
            .ToListAsync();
    }
    
    public async Task<int> CreateOrderAsync(int tableId, int waiterId, List<(int menuItemId, int qty, decimal price)> items)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var order = new Order
        {
            TableId = tableId,
            WaiterId = waiterId,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.Open
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(); 

        foreach (var (menuItemId, qty, price) in items)
        {
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = menuItemId,
                Quantity = qty,
                PriceAtOrder = price
            });
        }
        await db.SaveChangesAsync();

        await tx.CommitAsync();
        return order.Id;
    }

    public async Task<List<OrderSummaryDto>> GetOrdersAsync(DateTime? from = null, DateTime? to = null, OrderStatus? status = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Orders.AsNoTracking().AsQueryable();

        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.Table.Number,          
                o.Waiter.FullName,
                o.CreatedAt,
                o.Items.Sum(i => i.PriceAtOrder * i.Quantity), 
                o.Status.ToString()))
            .ToListAsync();
    }
    
    public async Task<List<OrderItemDetailDto>> GetOrderItemsAsync(int orderId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .Select(i => new OrderItemDetailDto(i.MenuItem.Name, i.Quantity, i.PriceAtOrder, i.Quantity * i.PriceAtOrder))
            .ToListAsync();
    }

    public async Task SetOrderStatusAsync(int orderId, OrderStatus status)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Orders.Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Status, status));
    }

    public async Task<List<PopularDishDto>> GetPopularDishesAsync(int top = 10)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var grouped = await db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.MenuItemId)
            .Select(g => new
            {
                MenuItemId = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.PriceAtOrder)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(top)
            .ToListAsync();

        var menuNames = await db.MenuItems.AsNoTracking()
            .Where(m => grouped.Select(g => g.MenuItemId).Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        return grouped
            .Select(g => new PopularDishDto(menuNames[g.MenuItemId], g.TotalQuantity, g.TotalRevenue))
            .ToList();
    }

    public async Task<List<RevenueDto>> GetRevenueByDayAsync(DateTime from, DateTime to)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // выручка по дням: джойн OrderItems -> Orders, группировка по дате, SUM в SQL
        var revenueByDay = await db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.Status == OrderStatus.Paid
                         && oi.Order.CreatedAt >= from
                         && oi.Order.CreatedAt <= to)
            .GroupBy(oi => oi.Order.CreatedAt.Date)
            .Select(grp => new
            {
                Day = grp.Key,
                Revenue = grp.Sum(x => x.PriceAtOrder * x.Quantity)
            })
            .ToListAsync();

        // количество заказов по дням: отдельный простой запрос, COUNT в SQL
        var ordersByDay = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(grp => new
            {
                Day = grp.Key,
                Count = grp.Count()
            })
            .ToListAsync();

        var ordersDict = ordersByDay.ToDictionary(x => x.Day, x => x.Count);

        // объединяем два маленьких набора данных (по дням) в памяти — это дёшево,
        // тяжёлые агрегаты уже посчитаны в БД
        return revenueByDay
            .Select(r => new RevenueDto(
                DateOnly.FromDateTime(r.Day),
                r.Revenue,
                ordersDict.TryGetValue(r.Day, out var cnt) ? cnt : 0))
            .OrderBy(r => r.Day)
            .ToList();
    }
}