using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajZastepstwa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_zastepstwa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zastepowany_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zastepca_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    od_kiedy = table.Column<DateOnly>(type: "date", nullable: false),
                    do_kiedy = table.Column<DateOnly>(type: "date", nullable: false),
                    powod = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    odwolane = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_zastepstwa", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_zastepstwa_org_employees_zastepca_employee_id",
                        column: x => x.zastepca_employee_id,
                        principalTable: "org_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_org_zastepstwa_org_employees_zastepowany_employee_id",
                        column: x => x.zastepowany_employee_id,
                        principalTable: "org_employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_zastepstwa_tenant_id_zastepowany_employee_id_od_kiedy_d",
                table: "org_zastepstwa",
                columns: new[] { "tenant_id", "zastepowany_employee_id", "od_kiedy", "do_kiedy" });

            migrationBuilder.CreateIndex(
                name: "ix_org_zastepstwa_zastepca_employee_id",
                table: "org_zastepstwa",
                column: "zastepca_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_org_zastepstwa_zastepowany_employee_id",
                table: "org_zastepstwa",
                column: "zastepowany_employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_zastepstwa");
        }
    }
}
