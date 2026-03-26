using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "time_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_qr_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    location_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_qr_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_schedule_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    definition = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_schedule_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_sheets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_worked = table.Column<TimeSpan>(type: "interval", nullable: false),
                    total_breaks = table.Column<TimeSpan>(type: "interval", nullable: false),
                    net_worked = table.Column<TimeSpan>(type: "interval", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_sheets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    planned_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    shift_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_schedules_schedule_template_template_id",
                        column: x => x.template_id,
                        principalTable: "time_schedule_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "time_anomalies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_sheet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_anomalies", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_anomalies_time_sheet_time_sheet_id",
                        column: x => x.time_sheet_id,
                        principalTable: "time_sheets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "time_corrections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_sheet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_clock_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_clock_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    corrected_clock_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    corrected_clock_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    corrected_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_corrections", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_corrections_time_sheet_time_sheet_id",
                        column: x => x.time_sheet_id,
                        principalTable: "time_sheets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_anomalies_tenant_id",
                table: "time_anomalies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_anomalies_tenant_id_employee_id_date",
                table: "time_anomalies",
                columns: new[] { "tenant_id", "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_time_anomalies_tenant_id_status",
                table: "time_anomalies",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_time_anomalies_time_sheet_id",
                table: "time_anomalies",
                column: "time_sheet_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_corrections_tenant_id",
                table: "time_corrections",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_corrections_tenant_id_employee_id_date",
                table: "time_corrections",
                columns: new[] { "tenant_id", "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_time_corrections_time_sheet_id",
                table: "time_corrections",
                column: "time_sheet_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_employee_id_entry_time",
                table: "time_entries",
                columns: new[] { "employee_id", "entry_time" });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_tenant_id",
                table: "time_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_tenant_id_employee_id_entry_time",
                table: "time_entries",
                columns: new[] { "tenant_id", "employee_id", "entry_time" });

            migrationBuilder.CreateIndex(
                name: "ix_time_qr_tokens_tenant_id",
                table: "time_qr_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_qr_tokens_tenant_id_expires_at",
                table: "time_qr_tokens",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_time_qr_tokens_token",
                table: "time_qr_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_schedule_templates_tenant_id",
                table: "time_schedule_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_schedule_templates_tenant_id_name",
                table: "time_schedule_templates",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_employee_id_date",
                table: "time_schedules",
                columns: new[] { "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_template_id",
                table: "time_schedules",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_tenant_id",
                table: "time_schedules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_schedules_tenant_id_employee_id_date",
                table: "time_schedules",
                columns: new[] { "tenant_id", "employee_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_sheets_employee_id_date",
                table: "time_sheets",
                columns: new[] { "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_time_sheets_tenant_id",
                table: "time_sheets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_sheets_tenant_id_employee_id_date",
                table: "time_sheets",
                columns: new[] { "tenant_id", "employee_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_anomalies");

            migrationBuilder.DropTable(
                name: "time_corrections");

            migrationBuilder.DropTable(
                name: "time_entries");

            migrationBuilder.DropTable(
                name: "time_qr_tokens");

            migrationBuilder.DropTable(
                name: "time_schedules");

            migrationBuilder.DropTable(
                name: "time_sheets");

            migrationBuilder.DropTable(
                name: "time_schedule_templates");
        }
    }
}
