using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wf_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_approval_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_approval_decisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_approval_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_approval_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_escalation_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    timeout_minutes = table.Column<int>(type: "integer", nullable: false),
                    action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action_payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_escalation_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_step_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    initiated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_instances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_steps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wf_actions_tenant_id_instance_id",
                table: "wf_actions",
                columns: new[] { "tenant_id", "instance_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_actions_tenant_id_step_id",
                table: "wf_actions",
                columns: new[] { "tenant_id", "step_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_approval_decisions_tenant_id_request_id",
                table: "wf_approval_decisions",
                columns: new[] { "tenant_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_approval_requests_tenant_id_approver_id_status",
                table: "wf_approval_requests",
                columns: new[] { "tenant_id", "approver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_approval_requests_tenant_id_instance_id",
                table: "wf_approval_requests",
                columns: new[] { "tenant_id", "instance_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_approval_requests_tenant_id_step_id",
                table: "wf_approval_requests",
                columns: new[] { "tenant_id", "step_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_definitions_tenant_id",
                table: "wf_definitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wf_definitions_tenant_id_name",
                table: "wf_definitions",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wf_escalation_rules_tenant_id_definition_id",
                table: "wf_escalation_rules",
                columns: new[] { "tenant_id", "definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_instances_tenant_id",
                table: "wf_instances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wf_instances_tenant_id_definition_id",
                table: "wf_instances",
                columns: new[] { "tenant_id", "definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_instances_tenant_id_entity_type_entity_id",
                table: "wf_instances",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_instances_tenant_id_status",
                table: "wf_instances",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_steps_tenant_id_instance_id",
                table: "wf_steps",
                columns: new[] { "tenant_id", "instance_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wf_actions");

            migrationBuilder.DropTable(
                name: "wf_approval_decisions");

            migrationBuilder.DropTable(
                name: "wf_approval_requests");

            migrationBuilder.DropTable(
                name: "wf_definitions");

            migrationBuilder.DropTable(
                name: "wf_escalation_rules");

            migrationBuilder.DropTable(
                name: "wf_instances");

            migrationBuilder.DropTable(
                name: "wf_steps");
        }
    }
}
