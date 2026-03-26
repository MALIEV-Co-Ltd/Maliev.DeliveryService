using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.DeliveryService.Application.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.DeliveryService.Application.Services;

/// <summary>
/// Registers Delivery Service permissions and roles with the centralized IAM service on startup.
/// </summary>
public class DeliveryIAMRegistrationService(
    IConfiguration configuration,
    ILogger<DeliveryIAMRegistrationService> logger) : IAMRegistrationService(configuration, logger, "delivery")
{
    /// <inheritdoc />
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return DeliveryPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc />
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return DeliveryPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = [.. r.Permissions],
            IsCustom = false
        });
    }
}
