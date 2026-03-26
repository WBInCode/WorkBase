using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIamTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    enabled_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iam_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iam_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iam_data_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_filter = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_data_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_iam_data_scopes_role_role_id",
                        column: x => x.role_id,
                        principalTable: "iam_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "iam_role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_iam_role_permissions_iam_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "iam_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_iam_role_permissions_iam_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "iam_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "iam_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iam_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_iam_user_roles_iam_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "iam_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_iam_user_roles_iam_users_user_id",
                        column: x => x.user_id,
                        principalTable: "iam_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_iam_data_scopes_role_id_module",
                table: "iam_data_scopes",
                columns: new[] { "role_id", "module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_data_scopes_tenant_id",
                table: "iam_data_scopes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_feature_flags_tenant_id",
                table: "iam_feature_flags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_feature_flags_tenant_id_module",
                table: "iam_feature_flags",
                columns: new[] { "tenant_id", "module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_permissions_module_action_scope",
                table: "iam_permissions",
                columns: new[] { "module", "action", "scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_role_permissions_permission_id",
                table: "iam_role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_role_permissions_role_id_permission_id",
                table: "iam_role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_roles_tenant_id",
                table: "iam_roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_roles_tenant_id_name",
                table: "iam_roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_roles_role_id",
                table: "iam_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_roles_tenant_id",
                table: "iam_user_roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_iam_user_roles_user_id_role_id",
                table: "iam_user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_data_scopes");

            migrationBuilder.DropTable(
                name: "iam_feature_flags");

            migrationBuilder.DropTable(
                name: "iam_role_permissions");

            migrationBuilder.DropTable(
                name: "iam_user_roles");

            migrationBuilder.DropTable(
                name: "iam_permissions");

            migrationBuilder.DropTable(
                name: "iam_roles");
        }
    }
}
