namespace Maliev.DeliveryService.Application.DTOs;

/// <summary>
/// Represents a paginated response.
/// </summary>
/// <typeparam name="T">The type of the items in the response.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
