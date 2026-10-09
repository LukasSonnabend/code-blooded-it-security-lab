namespace Aviato.Core.DTOs;

public record CreateOrderRequest(List<OrderItemRequest> Items);

public record OrderItemRequest(int ProductId, int Quantity);
