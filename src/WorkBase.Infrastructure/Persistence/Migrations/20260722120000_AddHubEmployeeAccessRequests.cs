using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkBase.Infrastructure.Persistence;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(WorkBaseDbContext))]
    [Migration("20260722120000_AddHubEmployeeAccessRequests")]
    public partial class AddHubEmployeeAccessRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hub_employee_access_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hub_organization_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    hub_product_instance_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    hub_invitation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    hub_membership_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    hub_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hub_employee_access_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hub_employee_access_requests_status_next_attempt_at",
                table: "hub_employee_access_requests",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_hub_employee_access_requests_tenant_id_employee_id",
                table: "hub_employee_access_requests",
                columns: new[] { "tenant_id", "employee_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "hub_employee_access_requests");
        }
    }
}