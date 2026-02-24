namespace Maliev.DeliveryService.Api.Services;

/// <summary>Initializes or represents a public member.</summary>
/// <summary>Initializes or represents a public member.</summary>
public class DeliveryNoteAuthorizationService : IDeliveryNoteAuthorizationService
{
    // TODO: Integrate with IAMService for actual customer assignments
    // For now, this is a stub implementation

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
    {
        // Stub: Always return true for now
        // In production, this would call IAMService to check customer assignments
        return Task.FromResult(true);
    }

    /// <summary>Initializes or represents a public member.</summary>
    /// <summary>Initializes or represents a public member.</summary>
    public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
    {
        // Stub: Return empty list for now
        // In production, this would call IAMService to get assigned customer IDs
        return Task.FromResult(new List<Guid>());
    }
}
