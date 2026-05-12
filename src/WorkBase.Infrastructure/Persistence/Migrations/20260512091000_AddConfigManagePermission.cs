using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigManagePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add config.manage permission (deterministic GUID following seed pattern)
            var permissionId = "20000000-0000-0000-0000-000000000100";
            migrationBuilder.Sql($"""
                INSERT INTO iam_permissions (id, module, action, scope, description)
                VALUES ('{permissionId}', 'config', 'manage', NULL, 'Zarządzanie konfiguracją systemu (wynagrodzenia, branding)')
                ON CONFLICT (id) DO NOTHING;
                """);

            // Assign to Super Admin role
            var superAdminRoleId = "10000000-0000-0000-0000-000000000001";
            migrationBuilder.Sql($"""
                INSERT INTO iam_role_permissions (id, role_id, permission_id)
                VALUES ('30000000-0000-0000-0000-000000000901', '{superAdminRoleId}', '{permissionId}')
                ON CONFLICT (id) DO NOTHING;
                """);

            // Assign to Admin role
            var adminRoleId = "10000000-0000-0000-0000-000000000002";
            migrationBuilder.Sql($"""
                INSERT INTO iam_role_permissions (id, role_id, permission_id)
                VALUES ('30000000-0000-0000-0000-000000000902', '{adminRoleId}', '{permissionId}')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM iam_role_permissions WHERE permission_id = '20000000-0000-0000-0000-000000000100';
                DELETE FROM iam_permissions WHERE id = '20000000-0000-0000-0000-000000000100';
                """);
        }
    }
}
