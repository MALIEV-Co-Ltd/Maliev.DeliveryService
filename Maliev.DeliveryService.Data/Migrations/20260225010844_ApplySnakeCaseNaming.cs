using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.DeliveryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplySnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_delivery_note_files_delivery_notes_delivery_note_id",
                table: "delivery_note_files");

            migrationBuilder.DropForeignKey(
                name: "FK_delivery_note_items_delivery_notes_delivery_note_id",
                table: "delivery_note_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_delivery_notes",
                table: "delivery_notes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_delivery_note_items",
                table: "delivery_note_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_delivery_note_files",
                table: "delivery_note_files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_addresses",
                table: "addresses");

            migrationBuilder.AddPrimaryKey(
                name: "pk_delivery_notes",
                table: "delivery_notes",
                column: "delivery_note_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_delivery_note_items",
                table: "delivery_note_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_delivery_note_files",
                table: "delivery_note_files",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_addresses",
                table: "addresses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_delivery_note_files_delivery_notes_delivery_note_id",
                table: "delivery_note_files",
                column: "delivery_note_id",
                principalTable: "delivery_notes",
                principalColumn: "delivery_note_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_delivery_note_items_delivery_notes_delivery_note_id",
                table: "delivery_note_items",
                column: "delivery_note_id",
                principalTable: "delivery_notes",
                principalColumn: "delivery_note_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_delivery_note_files_delivery_notes_delivery_note_id",
                table: "delivery_note_files");

            migrationBuilder.DropForeignKey(
                name: "fk_delivery_note_items_delivery_notes_delivery_note_id",
                table: "delivery_note_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_delivery_notes",
                table: "delivery_notes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_delivery_note_items",
                table: "delivery_note_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_delivery_note_files",
                table: "delivery_note_files");

            migrationBuilder.DropPrimaryKey(
                name: "pk_addresses",
                table: "addresses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_delivery_notes",
                table: "delivery_notes",
                column: "delivery_note_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_delivery_note_items",
                table: "delivery_note_items",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_delivery_note_files",
                table: "delivery_note_files",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_addresses",
                table: "addresses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_note_files_delivery_notes_delivery_note_id",
                table: "delivery_note_files",
                column: "delivery_note_id",
                principalTable: "delivery_notes",
                principalColumn: "delivery_note_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_note_items_delivery_notes_delivery_note_id",
                table: "delivery_note_items",
                column: "delivery_note_id",
                principalTable: "delivery_notes",
                principalColumn: "delivery_note_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
