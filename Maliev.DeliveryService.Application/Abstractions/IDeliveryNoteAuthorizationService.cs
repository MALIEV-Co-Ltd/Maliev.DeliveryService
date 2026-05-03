namespace Maliev.DeliveryService.Application.Abstractions;

/// <summary>
/// Service for authorization checks on delivery notes.
/// </summary>
public interface IDeliveryNoteAuthorizationService
{
    /// <summary>
    /// Checks if a principal can access a specific customer's delivery notes.
    /// </summary>
    Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Gets all customer IDs that a principal is authorized to access.
    /// </summary>
    Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default);
}
