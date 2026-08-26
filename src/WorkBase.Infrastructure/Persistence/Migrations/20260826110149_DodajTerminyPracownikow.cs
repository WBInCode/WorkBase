using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajTerminyPracownikow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_terminy_pracownikow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    typ_terminu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wazny_do = table.Column<DateOnly>(type: "date", nullable: false),
                    wykonany_dnia = table.Column<DateOnly>(type: "date", nullable: true),
                    notatka = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    dokument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archiwalny = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_terminy_pracownikow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_typy_terminow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nazwa = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    opis = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    dni_ostrzezenia = table.Column<int>(type: "integer", nullable: false),
                    aktywny = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_typy_terminow", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_terminy_pracownikow_tenant_id_archiwalny_wazny_do",
                table: "org_terminy_pracownikow",
                columns: new[] { "tenant_id", "archiwalny", "wazny_do" });

            migrationBuilder.CreateIndex(
                name: "ix_org_terminy_pracownikow_tenant_id_employee_id",
                table: "org_terminy_pracownikow",
                columns: new[] { "tenant_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_org_typy_terminow_tenant_id_kod",
                table: "org_typy_terminow",
                columns: new[] { "tenant_id", "kod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_terminy_pracownikow");

            migrationBuilder.DropTable(
                name: "org_typy_terminow");
        }
    }
}
