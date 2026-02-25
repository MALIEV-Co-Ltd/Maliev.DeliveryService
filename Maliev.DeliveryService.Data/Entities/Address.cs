namespace Maliev.DeliveryService.Data.Entities;

/// <summary>
/// Represents a physical address for delivery.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the unique identifier for the address.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company name associated with the address.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the contact name at this address.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets or sets the first line of the address.
    /// </summary>
    public string AddressLine1 { get; set; } = null!;

    /// <summary>
    /// Gets or sets the second line of the address.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = null!;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string StateProvince { get; set; } = null!;

    /// <summary>
    /// Gets or sets the postal or zip code.
    /// </summary>
    public string PostalCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = null!;

    /// <summary>
    /// Gets or sets the contact phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that created the address record.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the address record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system that last updated the address record.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
