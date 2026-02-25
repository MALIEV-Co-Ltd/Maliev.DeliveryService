namespace Maliev.DeliveryService.Data.Entities;

/// <summary>
/// Defines the possible statuses for a delivery.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Delivery is scheduled but not yet in transit.
    /// </summary>
    Pending,

    /// <summary>
    /// Delivery is currently in transit to the customer.
    /// </summary>
    InTransit,

    /// <summary>
    /// Delivery has been successfully completed and received.
    /// </summary>
    Delivered,

    /// <summary>
    /// Only some items from the delivery note have been received.
    /// </summary>
    PartiallyDelivered,

    /// <summary>
    /// Delivery has been cancelled.
    /// </summary>
    Cancelled
}
