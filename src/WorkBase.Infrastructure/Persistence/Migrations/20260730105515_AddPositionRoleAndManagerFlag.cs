using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkBase.Infrastructure.Persistence;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Stanowisko wskazuje domyślną rolę WorkBase i to, czy jest kierownicze. Napisana ręcznie,
    /// bo `dotnet ef migrations add` dołożyłby też 17 tabel niezmigrowanego dryfu modelu z main.
    /// </summary>
    [DbContext(typeof(WorkBaseDbContext))]
    [Migration("20260730105515_AddPositionRoleAndManagerFlag")]
    public partial class AddPositionRoleAndManagerFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_role_id",
                table: "org_positions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_managerial",
                table: "org_positions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "default_role_id", table: "org_positions");
            migrationBuilder.DropColumn(name: "is_managerial", table: "org_positions");
        }
    }
}
