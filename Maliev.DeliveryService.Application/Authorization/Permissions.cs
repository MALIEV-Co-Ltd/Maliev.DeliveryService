namespace Maliev.DeliveryService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Delivery Service.
/// </summary>
public static class DeliveryPermissions
{
    public const string DeliveryNoteCreate = "delivery.deliverynotes.create";
    public const string DeliveryNoteRead = "delivery.deliverynotes.read";
    public const string DeliveryNoteUpdate = "delivery.deliverynotes.update";
    public const string DeliveryNoteDelete = "delivery.deliverynotes.delete";
    public const string DeliveryNoteGenerate = "delivery.deliverynotes.generate";

    public const string DeliveryNoteFileCreate = "delivery.deliverynotefiles.create";
    public const string DeliveryNoteFileRead = "delivery.deliverynotefiles.read";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { DeliveryNoteCreate, "Create delivery notes" },
        { DeliveryNoteRead, "Read delivery notes" },
        { DeliveryNoteUpdate, "Update delivery notes" },
        { DeliveryNoteDelete, "Delete delivery notes" },
        { DeliveryNoteGenerate, "Generate delivery notes" },
        { DeliveryNoteFileCreate, "Create delivery note files" },
        { DeliveryNoteFileRead, "Read delivery note files" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
