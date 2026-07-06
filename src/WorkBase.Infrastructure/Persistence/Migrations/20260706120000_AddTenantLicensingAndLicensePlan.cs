using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkBase.Infrastructure.Persistence;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Hand-written (no .NET SDK available in the agent environment to run
    /// `dotnet ef migrations add`, see docs/05-module-licensing-architecture.md step 2 note).
    /// Adds exactly the schema needed for the Tenant/LicensePlan changes from that step:
    /// org_tenants gets 4 new nullable/defaulted columns, and a brand new cfg_license_plans
    /// table. Once someone has the SDK available, running
    /// `dotnet ef migrations add SyncSnapshotCheck` should produce an EMPTY migration if this
    /// file and WorkBaseDbContextModelSnapshot.cs were updated correctly — if it isn't empty,
    /// that diff points at whatever this hand-written migration missed.
    ///
    /// Deliberately does NOT attempt to create tables for the Integration/Cases/Contacts/
    /// Forms/Sales/AI modules, even though WorkBaseDbContext.GetModuleInfrastructureAssemblies
    /// now includes their EF configurations in the model — that is a much larger, higher-risk
    /// migration that still needs to be generated properly with the real SDK/tooling before
    /// those 6 modules can be used. Those modules' endpoints will 500 if called today; nothing
    /// at startup depends on their tables existing, so this does not block deployment.
    /// </remarks>
    [DbContext(typeof(WorkBaseDbContext))]
    [Migration("20260706120000_AddTenantLicensingAndLicensePlan")]
    public partial class AddTenantLicensingAndLicensePlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "keycloak_realm_name",
                table: "org_tenants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "license_plan_id",
                table: "org_tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "org_tenants",
                type: "integer",
                nullable: false,
                defaultValue: 1); // TenantStatus.Active

            migrationBuilder.AddColumn<DateTime>(
                name: "trial_expires_at",
                table: "org_tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_tenants_keycloak_realm_name",
                table: "org_tenants",
                column: "keycloak_realm_name",
                unique: true);

            migrationBuilder.CreateTable(
                name: "cfg_license_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    included_modules = table.Column<string[]>(type: "text[]", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cfg_license_plans", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cfg_license_plans_name",
                table: "cfg_license_plans",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cfg_license_plans");

            migrationBuilder.DropIndex(
                name: "ix_org_tenants_keycloak_realm_name",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "keycloak_realm_name",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "license_plan_id",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "status",
                table: "org_tenants");

            migrationBuilder.DropColumn(
                name: "trial_expires_at",
                table: "org_tenants");
        }
    }
}
