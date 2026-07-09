using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkBase.Infrastructure.Persistence;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Hand-written (no .NET SDK available in the agent environment, see repo memory
    /// backend-build-notes.md). Drops the unused cfg_tenant_branding.custom_css /
    /// footer_html columns (dead code — never rendered by the frontend, latent
    /// stored-XSS risk if ever wired up) and adds font_family for white-label font
    /// selection (docs/AUDIT-KNOWLEDGE-MAP.md — module/branding configuration).
    /// Once the real SDK is available, `dotnet ef migrations add SyncSnapshotCheck`
    /// should produce an EMPTY migration if this file and
    /// WorkBaseDbContextModelSnapshot.cs were updated correctly.
    /// </remarks>
    [DbContext(typeof(WorkBaseDbContext))]
    [Migration("20260709120000_UpdateTenantBrandingFields")]
    public partial class UpdateTenantBrandingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_css",
                table: "cfg_tenant_branding");

            migrationBuilder.DropColumn(
                name: "footer_html",
                table: "cfg_tenant_branding");

            migrationBuilder.AddColumn<string>(
                name: "font_family",
                table: "cfg_tenant_branding",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "font_family",
                table: "cfg_tenant_branding");

            migrationBuilder.AddColumn<string>(
                name: "custom_css",
                table: "cfg_tenant_branding",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "footer_html",
                table: "cfg_tenant_branding",
                type: "text",
                nullable: true);
        }
    }
}
