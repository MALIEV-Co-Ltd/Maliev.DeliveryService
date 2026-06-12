using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.DeliveryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveOrderDeliveryNoteUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_delivery_notes_order_id",
                table: "delivery_notes");

            migrationBuilder.CreateIndex(
                name: "ux_delivery_notes_active_order_id",
                table: "delivery_notes",
                column: "order_id",
                unique: true,
                filter: "order_id IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_delivery_notes_active_order_id",
                table: "delivery_notes");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_order_id",
                table: "delivery_notes",
                column: "order_id");
        }
    }
}
