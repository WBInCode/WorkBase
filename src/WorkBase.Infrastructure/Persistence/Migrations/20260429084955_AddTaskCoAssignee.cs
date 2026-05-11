using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCoAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "co_assignee_id",
                table: "task_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_tasks_tenant_id_co_assignee_id",
                table: "task_tasks",
                columns: new[] { "tenant_id", "co_assignee_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_tasks_tenant_id_co_assignee_id",
                table: "task_tasks");

            migrationBuilder.DropColumn(
                name: "co_assignee_id",
                table: "task_tasks");
        }
    }
}
