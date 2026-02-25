using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.DeliveryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeDeliveryService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    company_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    state_province = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_notes",
                columns: table => new
                {
                    delivery_note_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actual_delivery_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    shipping_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_address_line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_address_line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    shipping_province = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    shipping_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    shipping_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    delivery_contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    delivery_contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    carrier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    shipping_cost_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    received_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    signature_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    delivery_instructions = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_notes", x => x.delivery_note_id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_note_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    delivery_note_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    storage_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    uploaded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_note_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_note_files_delivery_notes_delivery_note_id",
                        column: x => x.delivery_note_id,
                        principalTable: "delivery_notes",
                        principalColumn: "delivery_note_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_note_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_note_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_id = table.Column<string>(type: "text", nullable: true),
                    purchase_order_item_id = table.Column<int>(type: "integer", nullable: true),
                    product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    product_description = table.Column<string>(type: "text", nullable: true),
                    quantity_ordered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity_manufactured = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    item_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_note_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_note_items_delivery_notes_delivery_note_id",
                        column: x => x.delivery_note_id,
                        principalTable: "delivery_notes",
                        principalColumn: "delivery_note_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_note_files_dn_id",
                table: "delivery_note_files",
                column: "delivery_note_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_note_items_dn_id",
                table: "delivery_note_items",
                column: "delivery_note_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_note_items_product",
                table: "delivery_note_items",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_customer_date",
                table: "delivery_notes",
                columns: new[] { "customer_id", "delivery_date" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_customer_id",
                table: "delivery_notes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_delivery_date",
                table: "delivery_notes",
                column: "delivery_date");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_order_id",
                table: "delivery_notes",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notes_status",
                table: "delivery_notes",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "delivery_note_files");

            migrationBuilder.DropTable(
                name: "delivery_note_items");

            migrationBuilder.DropTable(
                name: "delivery_notes");
        }
    }
}
