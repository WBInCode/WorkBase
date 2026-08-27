using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajListyKontrolne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_listy_kontrolne",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nazwa = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    wyzwalacz = table.Column<int>(type: "integer", nullable: false),
                    aktywna = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_listy_kontrolne", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "org_listy_kontrolne_pozycje",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tytul = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    dni_od_zdarzenia = table.Column<int>(type: "integer", nullable: false),
                    wykonawca = table.Column<int>(type: "integer", nullable: false),
                    osoba_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kolejnosc = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_listy_kontrolne_pozycje", x => x.id);
                    table.ForeignKey(
                        name: "fk_org_listy_kontrolne_pozycje_org_listy_kontrolne_lista_id",
                        column: x => x.lista_id,
                        principalTable: "org_listy_kontrolne",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_listy_kontrolne_tenant_id_wyzwalacz_aktywna",
                table: "org_listy_kontrolne",
                columns: new[] { "tenant_id", "wyzwalacz", "aktywna" });

            migrationBuilder.CreateIndex(
                name: "ix_org_listy_kontrolne_pozycje_lista_id",
                table: "org_listy_kontrolne_pozycje",
                column: "lista_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_listy_kontrolne_pozycje");

            migrationBuilder.DropTable(
                name: "org_listy_kontrolne");
        }
    }
}
