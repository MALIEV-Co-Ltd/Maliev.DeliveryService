namespace Maliev.DeliveryService.Api.Services;

public class DeliveryNoteAuthorizationService : IDeliveryNoteAuthorizationService
{
    // TODO: Integrate with IAMService for actual customer assignments
    // For now, this is a stub implementation

    public Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
    {
        // Stub: Always return true for now
        // In production, this would call IAMService to check customer assignments
        return Task.FromResult(true);
    }

    public Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
    {
        // Stub: Return empty list for now
        // In production, this would call IAMService to get assigned customer IDs
        return Task.FromResult(new List<Guid>());
    }
}
