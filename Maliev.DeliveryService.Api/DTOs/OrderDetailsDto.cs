namespace Maliev.DeliveryService.Api.DTOs;

public class OrderDetailsDto
{
    public string OrderId { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public List<OrderLineItemDto> Items { get; set; } = new();
}

public class OrderLineItemDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityManufactured { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
}
