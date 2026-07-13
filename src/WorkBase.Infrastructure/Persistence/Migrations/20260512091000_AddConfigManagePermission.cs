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

            // Assign to Super Admin role (only if that fixed-id role exists — on a fresh DB the
            // roles are created by IamSeeder with random GUIDs AFTER migrations run, so guard the
            // insert to avoid an FK violation; the seeder grants config.manage to the real roles).
            var superAdminRoleId = "10000000-0000-0000-0000-000000000001";
            migrationBuilder.Sql($"""
                INSERT INTO iam_role_permissions (id, role_id, permission_id)
                SELECT '30000000-0000-0000-0000-000000000901', '{superAdminRoleId}', '{permissionId}'
                WHERE EXISTS (SELECT 1 FROM iam_roles WHERE id = '{superAdminRoleId}')
                ON CONFLICT (id) DO NOTHING;
                """);

            // Assign to Admin role (same guard as above)
            var adminRoleId = "10000000-0000-0000-0000-000000000002";
            migrationBuilder.Sql($"""
                INSERT INTO iam_role_permissions (id, role_id, permission_id)
                SELECT '30000000-0000-0000-0000-000000000902', '{adminRoleId}', '{permissionId}'
                WHERE EXISTS (SELECT 1 FROM iam_roles WHERE id = '{adminRoleId}')
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
