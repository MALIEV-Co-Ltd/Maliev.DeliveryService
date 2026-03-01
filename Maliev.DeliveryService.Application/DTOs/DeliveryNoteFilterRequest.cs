namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Filter request for searching delivery notes.
/// </summary>
public class DeliveryNoteFilterRequest
{
    /// <summary>
    /// Gets or sets the order ID to filter by.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the customer ID to filter by.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the start date for filtering by delivery date.
    /// </summary>
    public DateTime? DeliveryDateFrom { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering by delivery date.
    /// </summary>
    public DateTime? DeliveryDateTo { get; set; }

    /// <summary>
    /// Gets or sets the status to filter by.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort order (asc or desc).
    /// </summary>
    public string? SortOrder { get; set; }
}
