namespace Cafe.Dto;

public record RevenueDto(DateOnly Day, decimal Revenue, int OrdersCount);