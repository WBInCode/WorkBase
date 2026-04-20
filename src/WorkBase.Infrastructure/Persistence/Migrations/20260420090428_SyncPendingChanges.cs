using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "requester_id",
                table: "wf_approval_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "doc_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doc_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    used_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    pending_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    carried_over_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_calendar_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    day_fraction = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_calendar_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_decisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    days_per_year = table.Column<int>(type: "integer", nullable: false),
                    allow_carry_over = table.Column<bool>(type: "boolean", nullable: false),
                    max_carry_over_days = table.Column<int>(type: "integer", nullable: false),
                    max_consecutive_days = table.Column<int>(type: "integer", nullable: true),
                    min_notice_days = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leave_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    default_days_per_year = table.Column<int>(type: "integer", nullable: true),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notif_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    body = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notif_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notif_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    in_app = table.Column<bool>(type: "boolean", nullable: false),
                    email = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notif_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notif_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    title_template = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body_template = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notif_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_attachments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    old_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    new_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_priorities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_priorities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    remind_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_reminders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_status_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_status_transitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_doc_categories_tenant_id",
                table: "doc_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_doc_documents_tenant_id",
                table: "doc_documents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_doc_documents_tenant_id_category_id",
                table: "doc_documents",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_doc_documents_tenant_id_entity_type_entity_id",
                table: "doc_documents",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_tenant_id",
                table: "leave_balances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_tenant_id_employee_id_leave_type_id_year",
                table: "leave_balances",
                columns: new[] { "tenant_id", "employee_id", "leave_type_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leave_calendar_entries_tenant_id",
                table: "leave_calendar_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_calendar_entries_tenant_id_date",
                table: "leave_calendar_entries",
                columns: new[] { "tenant_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_calendar_entries_tenant_id_employee_id_date",
                table: "leave_calendar_entries",
                columns: new[] { "tenant_id", "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_calendar_entries_tenant_id_leave_request_id",
                table: "leave_calendar_entries",
                columns: new[] { "tenant_id", "leave_request_id" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_decisions_tenant_id_leave_request_id",
                table: "leave_decisions",
                columns: new[] { "tenant_id", "leave_request_id" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_policies_tenant_id",
                table: "leave_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_policies_tenant_id_leave_type_id",
                table: "leave_policies",
                columns: new[] { "tenant_id", "leave_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id",
                table: "leave_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id_employee_id_status",
                table: "leave_requests",
                columns: new[] { "tenant_id", "employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id_start_date_end_date",
                table: "leave_requests",
                columns: new[] { "tenant_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_tenant_id_status",
                table: "leave_requests",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_tenant_id",
                table: "leave_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_tenant_id_code",
                table: "leave_types",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notif_notifications_tenant_id",
                table: "notif_notifications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_notif_notifications_tenant_id_recipient_user_id_created_at",
                table: "notif_notifications",
                columns: new[] { "tenant_id", "recipient_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notif_notifications_tenant_id_recipient_user_id_is_read",
                table: "notif_notifications",
                columns: new[] { "tenant_id", "recipient_user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_notif_preferences_tenant_id_user_id_category",
                table: "notif_preferences",
                columns: new[] { "tenant_id", "user_id", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notif_templates_tenant_id_code",
                table: "notif_templates",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_attachments_tenant_id",
                table: "task_attachments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_attachments_tenant_id_task_id",
                table: "task_attachments",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_comments_tenant_id",
                table: "task_comments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_comments_tenant_id_task_id",
                table: "task_comments",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_history_tenant_id",
                table: "task_history",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_history_tenant_id_task_id",
                table: "task_history",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_priorities_tenant_id",
                table: "task_priorities",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_priorities_tenant_id_code",
                table: "task_priorities",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_reminders_is_sent_remind_at",
                table: "task_reminders",
                columns: new[] { "is_sent", "remind_at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_reminders_tenant_id",
                table: "task_reminders",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_reminders_tenant_id_task_id",
                table: "task_reminders",
                columns: new[] { "tenant_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_status_transitions_tenant_id",
                table: "task_status_transitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_status_transitions_tenant_id_from_status_id_to_status_",
                table: "task_status_transitions",
                columns: new[] { "tenant_id", "from_status_id", "to_status_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_statuses_tenant_id",
                table: "task_statuses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_statuses_tenant_id_code",
                table: "task_statuses",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_tasks_tenant_id",
                table: "task_tasks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_tasks_tenant_id_assignee_id",
                table: "task_tasks",
                columns: new[] { "tenant_id", "assignee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_tasks_tenant_id_status_id",
                table: "task_tasks",
                columns: new[] { "tenant_id", "status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doc_categories");

            migrationBuilder.DropTable(
                name: "doc_documents");

            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_calendar_entries");

            migrationBuilder.DropTable(
                name: "leave_decisions");

            migrationBuilder.DropTable(
                name: "leave_policies");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "leave_types");

            migrationBuilder.DropTable(
                name: "notif_notifications");

            migrationBuilder.DropTable(
                name: "notif_preferences");

            migrationBuilder.DropTable(
                name: "notif_templates");

            migrationBuilder.DropTable(
                name: "task_attachments");

            migrationBuilder.DropTable(
                name: "task_comments");

            migrationBuilder.DropTable(
                name: "task_history");

            migrationBuilder.DropTable(
                name: "task_priorities");

            migrationBuilder.DropTable(
                name: "task_reminders");

            migrationBuilder.DropTable(
                name: "task_status_transitions");

            migrationBuilder.DropTable(
                name: "task_statuses");

            migrationBuilder.DropTable(
                name: "task_tasks");

            migrationBuilder.DropColumn(
                name: "requester_id",
                table: "wf_approval_requests");
        }
    }
}
