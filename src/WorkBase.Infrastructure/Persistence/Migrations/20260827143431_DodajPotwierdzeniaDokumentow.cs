using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DodajPotwierdzeniaDokumentow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "wymaga_potwierdzenia",
                table: "doc_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "doc_potwierdzenia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    potwierdzono_dnia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_potwierdzenia", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_doc_potwierdzenia_tenant_id_document_id_employee_id",
                table: "doc_potwierdzenia",
                columns: new[] { "tenant_id", "document_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_doc_potwierdzenia_tenant_id_employee_id",
                table: "doc_potwierdzenia",
                columns: new[] { "tenant_id", "employee_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doc_potwierdzenia");

            migrationBuilder.DropColumn(
                name: "wymaga_potwierdzenia",
                table: "doc_documents");
        }
    }
}
