using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkBase.Infrastructure.Persistence;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Hand-written because the local agent environment has no .NET SDK. A subsequent
    /// `dotnet ef migrations add SyncSnapshotCheck` should generate an empty migration.
    /// </remarks>
    [DbContext(typeof(WorkBaseDbContext))]
    [Migration("20260721120000_AddHubTenantProvisioning")]
    public partial class AddHubTenantProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hub_organization_id",
                table: "org_tenants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hub_product_instance_id",
                table: "org_tenants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_tenants_hub_organization_id",
                table: "org_tenants",
                column: "hub_organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_tenants_hub_product_instance_id",
                table: "org_tenants",
                column: "hub_product_instance_id",
                unique: true);

                        // Customer tenants used to receive a Super Admin role. Preserve those users'
                        // company-management access by moving the assignment to the tenant's Admin role,
                        // then remove the forbidden role and its cascading permissions/data scopes.
                        migrationBuilder.Sql(
                                """
                                DELETE FROM iam_user_roles AS super_assignment
                                USING iam_roles AS super_role,
                                            iam_roles AS admin_role,
                                            iam_user_roles AS admin_assignment
                                WHERE super_assignment.role_id = super_role.id
                                    AND lower(btrim(super_role.name)) = 'super admin'
                                    AND super_role.tenant_id <> '00000000-0000-0000-0000-000000000001'::uuid
                                    AND admin_role.tenant_id = super_role.tenant_id
                                    AND admin_role.name = 'Admin'
                                    AND admin_assignment.user_id = super_assignment.user_id
                                    AND admin_assignment.role_id = admin_role.id;

                                UPDATE iam_user_roles AS assignment
                                SET role_id = admin_role.id,
                                        assigned_by = 'system'
                                FROM iam_roles AS super_role,
                                         iam_roles AS admin_role
                                WHERE assignment.role_id = super_role.id
                                    AND lower(btrim(super_role.name)) = 'super admin'
                                    AND super_role.tenant_id <> '00000000-0000-0000-0000-000000000001'::uuid
                                    AND admin_role.tenant_id = super_role.tenant_id
                                    AND admin_role.name = 'Admin';

                                DELETE FROM iam_roles
                                WHERE lower(btrim(name)) = 'super admin'
                                    AND tenant_id <> '00000000-0000-0000-0000-000000000001'::uuid;

                                WITH ranked_admins AS (
                                        SELECT assignment.id,
                                                     row_number() OVER (
                                                             PARTITION BY assignment.tenant_id
                                                             ORDER BY assignment.assigned_at, assignment.id) AS position
                                        FROM iam_user_roles AS assignment
                                        JOIN iam_roles AS role ON role.id = assignment.role_id
                                        WHERE role.name = 'Admin'
                                            AND role.tenant_id <> '00000000-0000-0000-0000-000000000001'::uuid
                                )
                                DELETE FROM iam_user_roles AS assignment
                                USING ranked_admins
                                WHERE assignment.id = ranked_admins.id
                                    AND ranked_admins.position > 1;
                                """);

                        migrationBuilder.AddCheckConstraint(
                                name: "ck_iam_roles_super_admin_operator_only",
                                table: "iam_roles",
                                sql: "lower(btrim(name)) <> 'super admin' OR tenant_id = '00000000-0000-0000-0000-000000000001'::uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_iam_roles_super_admin_operator_only",
                table: "iam_roles");

            migrationBuilder.DropIndex(
                name: "ix_org_tenants_hub_organization_id",
                table: "org_tenants");

            migrationBuilder.DropIndex(
                name: "ix_org_tenants_hub_product_instance_id",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "hub_organization_id",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "hub_product_instance_id",
                table: "org_tenants");
        }
    }
}