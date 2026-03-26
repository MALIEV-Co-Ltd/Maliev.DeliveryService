namespace Maliev.DeliveryService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Delivery Service.
/// </summary>
public static class DeliveryPredefinedRoles
{
    public const string Admin = "roles.delivery.admin";
    public const string Operator = "roles.delivery.operator";
    public const string Viewer = "roles.delivery.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Delivery Administrator with full access",
            new[]
            {
                DeliveryPermissions.DeliveryNoteCreate,
                DeliveryPermissions.DeliveryNoteRead,
                DeliveryPermissions.DeliveryNoteUpdate,
                DeliveryPermissions.DeliveryNoteDelete,
                DeliveryPermissions.DeliveryNoteGenerate,
                DeliveryPermissions.DeliveryNoteFileCreate,
                DeliveryPermissions.DeliveryNoteFileRead,
            }
        ),
        (
            Operator,
            "Delivery Operator with create, read, and generate access",
            new[]
            {
                DeliveryPermissions.DeliveryNoteCreate,
                DeliveryPermissions.DeliveryNoteRead,
                DeliveryPermissions.DeliveryNoteUpdate,
                DeliveryPermissions.DeliveryNoteGenerate,
                DeliveryPermissions.DeliveryNoteFileCreate,
                DeliveryPermissions.DeliveryNoteFileRead,
            }
        ),
        (
            Viewer,
            "Delivery Viewer with read-only access",
            new[]
            {
                DeliveryPermissions.DeliveryNoteRead,
                DeliveryPermissions.DeliveryNoteFileRead,
            }
        ),
    };
}
