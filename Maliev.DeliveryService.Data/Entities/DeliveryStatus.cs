namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Defines the possible states of a delivery note.</summary>
public enum DeliveryStatus
{
    /// <summary>Delivery note created but not yet shipped.</summary>
    Pending,
    /// <summary>Package is with the carrier and moving to the destination.</summary>
    InTransit,
    /// <summary>Delivery completed successfully.</summary>
    Delivered,
    /// <summary>Some items delivered, others remaining or returned.</summary>
    PartiallyDelivered,
    /// <summary>Delivery note has been cancelled.</summary>
    Cancelled
}
