using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "break_type",
                table: "time_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "time_break_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    break_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    max_per_day = table.Column<int>(type: "integer", nullable: false),
                    max_minutes_per_break = table.Column<int>(type: "integer", nullable: false),
                    max_minutes_per_day = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_break_policies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_break_policies_tenant_id",
                table: "time_break_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_break_policies_tenant_id_break_type",
                table: "time_break_policies",
                columns: new[] { "tenant_id", "break_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_break_policies");

            migrationBuilder.DropColumn(
                name: "break_type",
                table: "time_entries");
        }
    }
}
