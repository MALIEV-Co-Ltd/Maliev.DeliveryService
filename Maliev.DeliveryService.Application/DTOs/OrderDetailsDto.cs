namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Data transfer object for order details from the Order Service.
/// </summary>
public class OrderDetailsDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the order.
    /// </summary>
    public string OrderId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the human-readable order number.
    /// </summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of items in the order.
    /// </summary>
    public List<OrderLineItemDto> Items { get; set; } = new();
}

/// <summary>
/// Data transfer object for an order line item.
/// </summary>
public class OrderLineItemDto
{
    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    public decimal QuantityOrdered { get; set; }

    /// <summary>
    /// Gets or sets the quantity manufactured.
    /// </summary>
    public decimal QuantityManufactured { get; set; }

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = null!;
}
