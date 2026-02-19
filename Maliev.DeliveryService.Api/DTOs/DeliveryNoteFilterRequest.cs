namespace Maliev.DeliveryService.Api.DTOs;

public class DeliveryNoteFilterRequest
{
    public string? OrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime? DeliveryDateFrom { get; set; }
    public DateTime? DeliveryDateTo { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "delivery_date";
    public string? SortOrder { get; set; } = "desc";
}
