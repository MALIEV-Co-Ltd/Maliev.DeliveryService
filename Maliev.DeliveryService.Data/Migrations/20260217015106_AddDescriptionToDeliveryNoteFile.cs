using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.DeliveryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToDeliveryNoteFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "delivery_note_files",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "delivery_note_files");
        }
    }
}
