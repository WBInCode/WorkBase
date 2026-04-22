using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "context_json",
                table: "wf_instances",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "cfg_custom_field_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "billing_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_invoice_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    amount_due = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    pdf_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "billing_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    max_users = table.Column<int>(type: "integer", nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_card_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    section_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_collapsed_by_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_card_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_department_module_forms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_department_module_forms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_department_module_workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_department_module_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_department_modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_department_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_onboarding_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    admin_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    admin_full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    plan_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_onboarding_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_saved_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    filters_json = table.Column<string>(type: "jsonb", nullable: false),
                    sort_json = table.Column<string>(type: "jsonb", nullable: false),
                    columns_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_saved_views", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_tenant_branding",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    favicon_url = table.Column<string>(type: "text", nullable: true),
                    primary_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    secondary_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    accent_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    app_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    custom_domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    custom_css = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    login_background_url = table.Column<string>(type: "text", nullable: true),
                    footer_html = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_tenant_branding", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dash_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dash_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dash_widgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dashboard_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    widget_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    column = table.Column<int>(type: "integer", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dash_widgets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    data_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    filters_json = table.Column<string>(type: "jsonb", nullable: true),
                    columns_json = table.Column<string>(type: "jsonb", nullable: true),
                    group_by_json = table.Column<string>(type: "jsonb", nullable: true),
                    aggregations_json = table.Column<string>(type: "jsonb", nullable: true),
                    chart_config_json = table.Column<string>(type: "jsonb", nullable: true),
                    sort_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infra_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allowed_ips = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    scopes_json = table.Column<string>(type: "jsonb", nullable: true),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infra_sync_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    device_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    operation_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    client_timestamp = table.Column<long>(type: "bigint", nullable: false),
                    server_timestamp = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    conflict_resolution = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_sync_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infra_tenant_schemas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    isolation_level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    connection_string = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    is_migrated = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_migrated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_tenant_schemas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infra_webhook_delivery_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_webhook_delivery_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "infra_webhook_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    events_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    last_delivery_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_delivery_status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_webhook_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notif_push_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    auth = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    device_info = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notif_push_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_biometric_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    biometric_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    template_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    enrolled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_biometric_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_geofence_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    geofence_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    latitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    time_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_geofence_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_geofence_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    latitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    radius_meters = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    auto_clock_in = table.Column<bool>(type: "boolean", nullable: false),
                    auto_clock_out = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_geofence_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_nfc_badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_uid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_nfc_badges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_step_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_step_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_branches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_employees_custom_fields",
                table: "org_employees",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_custom_fields",
                table: "leave_requests",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_billing_invoices_tenant_id",
                table: "billing_invoices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_subscriptions_stripe_subscription_id",
                table: "billing_subscriptions",
                column: "stripe_subscription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_subscriptions_tenant_id",
                table: "billing_subscriptions",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cfg_card_sections_tenant_id_entity_type",
                table: "cfg_card_sections",
                columns: new[] { "tenant_id", "entity_type" });

            migrationBuilder.CreateIndex(
                name: "ix_cfg_department_module_forms_department_module_id",
                table: "cfg_department_module_forms",
                column: "department_module_id");

            migrationBuilder.CreateIndex(
                name: "ix_cfg_department_module_workflows_department_module_id",
                table: "cfg_department_module_workflows",
                column: "department_module_id");

            migrationBuilder.CreateIndex(
                name: "ix_cfg_department_modules_tenant_id_org_unit_id",
                table: "cfg_department_modules",
                columns: new[] { "tenant_id", "org_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cfg_onboarding_requests_admin_email",
                table: "cfg_onboarding_requests",
                column: "admin_email");

            migrationBuilder.CreateIndex(
                name: "ix_cfg_saved_views_tenant_id_user_id_entity_type",
                table: "cfg_saved_views",
                columns: new[] { "tenant_id", "user_id", "entity_type" });

            migrationBuilder.CreateIndex(
                name: "ix_cfg_tenant_branding_custom_domain",
                table: "cfg_tenant_branding",
                column: "custom_domain",
                unique: true,
                filter: "custom_domain IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cfg_tenant_branding_tenant_id",
                table: "cfg_tenant_branding",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dash_configs_tenant_id",
                table: "dash_configs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_dash_configs_tenant_id_user_id",
                table: "dash_configs",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dash_configs_tenant_id_user_id_is_default",
                table: "dash_configs",
                columns: new[] { "tenant_id", "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_dash_widgets_tenant_id",
                table: "dash_widgets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_dash_widgets_tenant_id_dashboard_config_id",
                table: "dash_widgets",
                columns: new[] { "tenant_id", "dashboard_config_id" });

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_reports_created_by_user_id",
                table: "dashboard_reports",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_reports_tenant_id",
                table: "dashboard_reports",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_infra_api_keys_key_hash",
                table: "infra_api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_infra_api_keys_tenant_id",
                table: "infra_api_keys",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_infra_sync_queue_device_id_client_timestamp",
                table: "infra_sync_queue",
                columns: new[] { "device_id", "client_timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_infra_sync_queue_tenant_id_user_id_status",
                table: "infra_sync_queue",
                columns: new[] { "tenant_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_infra_tenant_schemas_schema_name",
                table: "infra_tenant_schemas",
                column: "schema_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_infra_tenant_schemas_tenant_id",
                table: "infra_tenant_schemas",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_infra_webhook_delivery_logs_subscription_id",
                table: "infra_webhook_delivery_logs",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_infra_webhook_delivery_logs_tenant_id",
                table: "infra_webhook_delivery_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_infra_webhook_subscriptions_tenant_id",
                table: "infra_webhook_subscriptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_notif_push_subscriptions_tenant_id_user_id",
                table: "notif_push_subscriptions",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notif_push_subscriptions_tenant_id_user_id_endpoint",
                table: "notif_push_subscriptions",
                columns: new[] { "tenant_id", "user_id", "endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_biometric_templates_employee_id",
                table: "time_biometric_templates",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_biometric_templates_tenant_id_template_hash",
                table: "time_biometric_templates",
                columns: new[] { "tenant_id", "template_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_geofence_events_employee_id_occurred_at",
                table: "time_geofence_events",
                columns: new[] { "employee_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_time_geofence_zones_tenant_id",
                table: "time_geofence_zones",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_nfc_badges_employee_id",
                table: "time_nfc_badges",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_nfc_badges_tenant_id_badge_uid",
                table: "time_nfc_badges",
                columns: new[] { "tenant_id", "badge_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wf_branches_instance_id_gateway_step_name",
                table: "wf_branches",
                columns: new[] { "instance_id", "gateway_step_name" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_branches_tenant_id",
                table: "wf_branches",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_invoices");

            migrationBuilder.DropTable(
                name: "billing_subscriptions");

            migrationBuilder.DropTable(
                name: "cfg_card_sections");

            migrationBuilder.DropTable(
                name: "cfg_department_module_forms");

            migrationBuilder.DropTable(
                name: "cfg_department_module_workflows");

            migrationBuilder.DropTable(
                name: "cfg_department_modules");

            migrationBuilder.DropTable(
                name: "cfg_onboarding_requests");

            migrationBuilder.DropTable(
                name: "cfg_saved_views");

            migrationBuilder.DropTable(
                name: "cfg_tenant_branding");

            migrationBuilder.DropTable(
                name: "dash_configs");

            migrationBuilder.DropTable(
                name: "dash_widgets");

            migrationBuilder.DropTable(
                name: "dashboard_reports");

            migrationBuilder.DropTable(
                name: "infra_api_keys");

            migrationBuilder.DropTable(
                name: "infra_sync_queue");

            migrationBuilder.DropTable(
                name: "infra_tenant_schemas");

            migrationBuilder.DropTable(
                name: "infra_webhook_delivery_logs");

            migrationBuilder.DropTable(
                name: "infra_webhook_subscriptions");

            migrationBuilder.DropTable(
                name: "notif_push_subscriptions");

            migrationBuilder.DropTable(
                name: "time_biometric_templates");

            migrationBuilder.DropTable(
                name: "time_geofence_events");

            migrationBuilder.DropTable(
                name: "time_geofence_zones");

            migrationBuilder.DropTable(
                name: "time_nfc_badges");

            migrationBuilder.DropTable(
                name: "wf_branches");

            migrationBuilder.DropIndex(
                name: "ix_org_employees_custom_fields",
                table: "org_employees");

            migrationBuilder.DropIndex(
                name: "ix_leave_requests_custom_fields",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "context_json",
                table: "wf_instances");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "cfg_custom_field_definitions");
        }
    }
}
