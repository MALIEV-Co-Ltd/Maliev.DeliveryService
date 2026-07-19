using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.DeliveryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryStatusAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "delivery_status_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_note_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    previous_status = table.Column<string>(type: "text", nullable: false),
                    new_status = table.Column<string>(type: "text", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_status_audits", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_status_audits_delivery_notes_delivery_note_id",
                        column: x => x.delivery_note_id,
                        principalTable: "delivery_notes",
                        principalColumn: "delivery_note_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_status_audits_changed_at",
                table: "delivery_status_audits",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_status_audits_delivery_note_id",
                table: "delivery_status_audits",
                column: "delivery_note_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_status_audits");
        }
    }
}
