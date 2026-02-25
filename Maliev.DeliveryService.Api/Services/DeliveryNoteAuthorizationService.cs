using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.DeliveryService.Api.Services;

public class DeliveryNoteAuthorizationService : IDeliveryNoteAuthorizationService
{
    private readonly IIamServiceClient _iamServiceClient;

    public DeliveryNoteAuthorizationService(IIamServiceClient iamServiceClient)
    {
        _iamServiceClient = iamServiceClient;
    }

    public async Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
    {
        return await _iamServiceClient.CheckPermissionAsync(principalId, "delivery.customer.read", $"customers/{customerId}", ct);
    }

    public async Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
    {
        var authorizedResourceIds = await _iamServiceClient.GetAuthorizedResourcesAsync(principalId, "delivery.customer.read", "customers", ct);
        
        var result = new List<Guid>();
        foreach (var idStr in authorizedResourceIds)
        {
            if (Guid.TryParse(idStr, out var id))
            {
                result.Add(id);
            }
        }
        return result;
    }
}
