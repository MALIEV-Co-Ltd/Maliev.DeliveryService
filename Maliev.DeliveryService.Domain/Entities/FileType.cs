namespace Maliev.DeliveryService.Domain.Entities;

/// <summary>
/// Defines the types of files that can be attached to a delivery note.
/// </summary>
public enum FileType
{
    /// <summary>
    /// A digital signature captured upon delivery.
    /// </summary>
    Signature,

    /// <summary>
    /// A photo taken as proof of delivery or condition.
    /// </summary>
    Photo,

    /// <summary>
    /// A packing list document.
    /// </summary>
    PackingList,

    /// <summary>
    /// An invoice document.
    /// </summary>
    Invoice,

    /// <summary>
    /// Any other type of file.
    /// </summary>
    Other
}
