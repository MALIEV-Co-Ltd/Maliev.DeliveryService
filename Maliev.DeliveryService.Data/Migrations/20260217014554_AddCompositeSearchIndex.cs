using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.DeliveryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_customer_delivery_date",
                table: "delivery_notes",
                columns: new[] { "customer_id", "delivery_date" },
                descending: new[] { false, true },
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_delivery_notes_customer_delivery_date",
                table: "delivery_notes");
        }
    }
}
