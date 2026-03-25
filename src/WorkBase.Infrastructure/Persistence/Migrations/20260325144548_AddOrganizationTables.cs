using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    termination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    settings = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_unit_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_unit_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_supervisor_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supervisor_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subordinate_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_supervisor_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_supervisor_relations_org_employees_subordinate_employee",
                        column: x => x.subordinate_employee_id,
                        principalTable: "org_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_org_supervisor_relations_org_employees_supervisor_employee_",
                        column: x => x.supervisor_employee_id,
                        principalTable: "org_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_units_org_units_parent_id",
                        column: x => x.parent_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_org_units_organization_unit_type_type_id",
                        column: x => x.type_id,
                        principalTable: "org_unit_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_employee_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_employee_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_employee_assignments_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "org_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_org_employee_assignments_organization_unit_organization_uni",
                        column: x => x.organization_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_org_employee_assignments_position_position_id",
                        column: x => x.position_id,
                        principalTable: "org_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "org_unit_closure",
                columns: table => new
                {
                    ancestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descendant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_unit_closure", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "fk_org_unit_closure_organization_unit_ancestor_id",
                        column: x => x.ancestor_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_org_unit_closure_organization_unit_descendant_id",
                        column: x => x.descendant_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_employee_assignments_employee_id",
                table: "org_employee_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_employee_assignments_organization_unit_id",
                table: "org_employee_assignments",
                column: "organization_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_employee_assignments_position_id",
                table: "org_employee_assignments",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_employee_assignments_tenant_id",
                table: "org_employee_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_employees_tenant_id",
                table: "org_employees",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_employees_tenant_id_email",
                table: "org_employees",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_employees_tenant_id_employee_number",
                table: "org_employees",
                columns: new[] { "tenant_id", "employee_number" },
                unique: true,
                filter: "employee_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_org_employees_user_id",
                table: "org_employees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_positions_tenant_id",
                table: "org_positions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_positions_tenant_id_name",
                table: "org_positions",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_supervisor_relations_subordinate_employee_id",
                table: "org_supervisor_relations",
                column: "subordinate_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_supervisor_relations_supervisor_employee_id",
                table: "org_supervisor_relations",
                column: "supervisor_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_supervisor_relations_tenant_id",
                table: "org_supervisor_relations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_supervisor_relations_tenant_id_subordinate_employee_id_",
                table: "org_supervisor_relations",
                columns: new[] { "tenant_id", "subordinate_employee_id", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_org_tenants_slug",
                table: "org_tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_closure_ancestor_id",
                table: "org_unit_closure",
                column: "ancestor_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_closure_descendant_id",
                table: "org_unit_closure",
                column: "descendant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_types_tenant_id",
                table: "org_unit_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_types_tenant_id_name",
                table: "org_unit_types",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_units_parent_id",
                table: "org_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_units_tenant_id",
                table: "org_units",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_units_tenant_id_code",
                table: "org_units",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_org_units_type_id",
                table: "org_units",
                column: "type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_employee_assignments");

            migrationBuilder.DropTable(
                name: "org_supervisor_relations");

            migrationBuilder.DropTable(
                name: "org_tenants");

            migrationBuilder.DropTable(
                name: "org_unit_closure");

            migrationBuilder.DropTable(
                name: "org_positions");

            migrationBuilder.DropTable(
                name: "org_employees");

            migrationBuilder.DropTable(
                name: "org_units");

            migrationBuilder.DropTable(
                name: "org_unit_types");
        }
    }
}
