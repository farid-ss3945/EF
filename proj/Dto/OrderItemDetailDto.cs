namespace Cafe.Dto;

public record OrderItemDetailDto(string Name, int Quantity, decimal Price, decimal LineTotal);