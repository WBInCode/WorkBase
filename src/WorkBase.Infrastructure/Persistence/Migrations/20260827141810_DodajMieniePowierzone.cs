using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajMieniePowierzone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "org_mienie_powierzone",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rodzaj = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nazwa = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    numer_seryjny = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    wartosc = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    wydano_dnia = table.Column<DateOnly>(type: "date", nullable: false),
                    zwrocono_dnia = table.Column<DateOnly>(type: "date", nullable: true),
                    potwierdzono_odbior = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notatka = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_mienie_powierzone", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_org_mienie_powierzone_niezwrocone",
                table: "org_mienie_powierzone",
                columns: new[] { "tenant_id", "employee_id" },
                filter: "zwrocono_dnia IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "org_mienie_powierzone");
        }
    }
}
