namespace Maliev.DeliveryService.Api.Authorization;

/// <summary>
/// Permission constants for Delivery Service endpoints.
/// </summary>
public static class DeliveryPermissions
{
    /// <summary>
    /// Permissions for delivery note operations.
    /// </summary>
    public static class DeliveryNotes
    {
        /// <summary>
        /// Permission to create delivery notes.
        /// </summary>
        public const string Create = "delivery.deliverynotes.create";

        /// <summary>
        /// Permission to read delivery notes.
        /// </summary>
        public const string Read = "delivery.deliverynotes.read";

        /// <summary>
        /// Permission to update delivery notes.
        /// </summary>
        public const string Update = "delivery.deliverynotes.update";

        /// <summary>
        /// Permission to delete delivery notes.
        /// </summary>
        public const string Delete = "delivery.deliverynotes.delete";

        /// <summary>
        /// Permission to request delivery note PDF generation.
        /// </summary>
        public const string GeneratePdf = "delivery.deliverynotes.generate";
    }

    /// <summary>
    /// Permissions for delivery note file operations.
    /// </summary>
    public static class DeliveryNoteFiles
    {
        /// <summary>
        /// Permission to upload delivery note files.
        /// </summary>
        public const string Create = "delivery.deliverynotefiles.create";

        /// <summary>
        /// Permission to read delivery note files.
        /// </summary>
        public const string Read = "delivery.deliverynotefiles.read";
    }
}
