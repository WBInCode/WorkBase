using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSourceAndOrgUnitSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "time_org_unit_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    week_pattern = table.Column<string>(type: "jsonb", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_org_unit_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_org_unit_schedules_tenant_id",
                table: "time_org_unit_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_org_unit_schedules_tenant_id_org_unit_id",
                table: "time_org_unit_schedules",
                columns: new[] { "tenant_id", "org_unit_id" },
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "time_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "org_unit_schedule_id",
                table: "time_schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_org_unit_schedule_id",
                table: "time_schedules",
                column: "org_unit_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_tenant_id_org_unit_schedule_id_source",
                table: "time_schedules",
                columns: new[] { "tenant_id", "org_unit_schedule_id", "source" });

            migrationBuilder.AddForeignKey(
                name: "fk_time_schedules_time_org_unit_schedules_org_unit_schedule_id",
                table: "time_schedules",
                column: "org_unit_schedule_id",
                principalTable: "time_org_unit_schedules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_time_schedules_time_org_unit_schedules_org_unit_schedule_id",
                table: "time_schedules");

            migrationBuilder.DropIndex(
                name: "ix_time_schedules_org_unit_schedule_id",
                table: "time_schedules");

            migrationBuilder.DropIndex(
                name: "ix_time_schedules_tenant_id_org_unit_schedule_id_source",
                table: "time_schedules");

            migrationBuilder.DropColumn(
                name: "source",
                table: "time_schedules");

            migrationBuilder.DropColumn(
                name: "org_unit_schedule_id",
                table: "time_schedules");

            migrationBuilder.DropTable(
                name: "time_org_unit_schedules");
        }
    }
}
