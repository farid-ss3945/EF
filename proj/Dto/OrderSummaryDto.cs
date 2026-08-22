namespace Cafe.Dto;

public record OrderSummaryDto(int OrderId, int TableNumber, string WaiterName, DateTime CreatedAt, decimal Total, string Status);
