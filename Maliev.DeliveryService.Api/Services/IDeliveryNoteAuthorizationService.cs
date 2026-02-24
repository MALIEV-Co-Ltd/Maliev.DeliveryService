namespace Maliev.DeliveryService.Api.Services;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public interface IDeliveryNoteAuthorizationService
{
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default);
    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default);
}
