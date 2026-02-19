namespace Maliev.DeliveryService.Api.Services;

public interface IDeliveryNoteAuthorizationService
{
    Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default);
    Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default);
}
