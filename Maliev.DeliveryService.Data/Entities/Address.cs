namespace Maliev.DeliveryService.Data.Entities;

/// <summary>Represents a postal address for shipping or billing.</summary>
public class Address
{
    /// <summary>Unique identifier for the address.</summary>
    public Guid Id { get; set; }
    /// <summary>Name of the company at this address.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Name of the contact person at this address.</summary>
    public string? ContactName { get; set; }
    /// <summary>Primary address line.</summary>
    public string AddressLine1 { get; set; } = null!;
    /// <summary>Secondary address line.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>City or locality.</summary>
    public string City { get; set; } = null!;
    /// <summary>State, province, or region.</summary>
    public string StateProvince { get; set; } = null!;
    /// <summary>Postal or zip code.</summary>
    public string PostalCode { get; set; } = null!;
    /// <summary>Country name.</summary>
    public string Country { get; set; } = null!;
    /// <summary>Phone number for delivery contact.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Email address for delivery notifications.</summary>
    public string? EmailAddress { get; set; }
    /// <summary>Timestamp when the address was created.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>User who created the address record.</summary>
    public string CreatedBy { get; set; } = null!;
    /// <summary>Timestamp when the address was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>User who last updated the address record.</summary>
    public string? UpdatedBy { get; set; }
}
