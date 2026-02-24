namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Categorizes the types of files attached to a delivery note.</summary>
public enum FileType
{
    /// <summary>Digital signature from the recipient.</summary>
    Signature,
    /// <summary>Photo of the delivered package or proof of delivery.</summary>
    Photo,
    /// <summary>Detailed packing list document.</summary>
    PackingList,
    /// <summary>Commercial invoice for the delivery.</summary>
    Invoice,
    /// <summary>Other miscellaneous attachments.</summary>
    Other
}
