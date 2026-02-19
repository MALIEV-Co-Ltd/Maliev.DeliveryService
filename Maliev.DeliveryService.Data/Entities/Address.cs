namespace Maliev.DeliveryService.Data.Entities;

public class Address
{
    public Guid Id { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string AddressLine1 { get; set; } = null!;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = null!;
    public string StateProvince { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
