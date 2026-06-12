using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.DeliveryService.Application.Abstractions;
using Maliev.DeliveryService.Application.Authorization;

namespace Maliev.DeliveryService.Infrastructure.Authorization;

/// <summary>
/// Implementation of delivery note authorization service using IAM.
/// </summary>
public class DeliveryNoteAuthorizationService : IDeliveryNoteAuthorizationService
{
    private const string SystemAutoPrincipal = "system-auto";
    private readonly IIamServiceClient _iamServiceClient;

    /// <summary>
    /// Initializes a new instance of DeliveryNoteAuthorizationService.
    /// </summary>
    public DeliveryNoteAuthorizationService(IIamServiceClient iamServiceClient)
    {
        _iamServiceClient = iamServiceClient;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessCustomerAsync(string principalId, Guid customerId, CancellationToken ct = default)
    {
        if (IsSystemAutoPrincipal(principalId))
        {
            return true;
        }

        return await _iamServiceClient.CheckPermissionAsync(principalId, "delivery.customer.read", $"customers/{customerId}", ct);
    }

    /// <inheritdoc />
    public async Task<bool> HasUnrestrictedAccessAsync(string principalId, CancellationToken ct = default)
    {
        if (IsSystemAutoPrincipal(principalId))
        {
            return true;
        }

        return await _iamServiceClient.CheckPermissionAsync(
            principalId,
            DeliveryPermissions.DeliveryNoteRead,
            "delivery-notes/*",
            ct);
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetAuthorizedCustomerIdsAsync(string principalId, CancellationToken ct = default)
    {
        if (IsSystemAutoPrincipal(principalId))
        {
            return [];
        }

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

    private static bool IsSystemAutoPrincipal(string principalId) =>
        string.Equals(principalId, SystemAutoPrincipal, StringComparison.Ordinal);
}
