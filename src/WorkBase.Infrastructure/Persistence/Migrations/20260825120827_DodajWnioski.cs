using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajWnioski : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wf_typy_wnioskow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nazwa = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    opis = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    pola_json = table.Column<string>(type: "jsonb", nullable: false),
                    wymaga_akceptacji = table.Column<bool>(type: "boolean", nullable: false),
                    aktywny = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_typy_wnioskow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wf_wnioski",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    typ_wniosku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wartosci_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    zlozony_o = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rozstrzygniety_o = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wf_wnioski", x => x.id);
                    table.ForeignKey(
                        name: "fk_wf_wnioski_wf_typy_wnioskow_typ_wniosku_id",
                        column: x => x.typ_wniosku_id,
                        principalTable: "wf_typy_wnioskow",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wf_typy_wnioskow_tenant_id_kod",
                table: "wf_typy_wnioskow",
                columns: new[] { "tenant_id", "kod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wf_wnioski_tenant_id_employee_id_zlozony_o",
                table: "wf_wnioski",
                columns: new[] { "tenant_id", "employee_id", "zlozony_o" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_wnioski_tenant_id_status",
                table: "wf_wnioski",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_wf_wnioski_typ_wniosku_id",
                table: "wf_wnioski",
                column: "typ_wniosku_id");

            migrationBuilder.CreateIndex(
                name: "ix_wf_wnioski_workflow_instance_id",
                table: "wf_wnioski",
                column: "workflow_instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wf_wnioski");

            migrationBuilder.DropTable(
                name: "wf_typy_wnioskow");
        }
    }
}
