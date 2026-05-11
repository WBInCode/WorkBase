using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssigneesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_tasks_tenant_id_co_assignee_id",
                table: "task_tasks");

            migrationBuilder.DropColumn(
                name: "co_assignee_id",
                table: "task_tasks");

            migrationBuilder.CreateTable(
                name: "task_task_assignees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_task_assignees", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_task_assignees_task_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "task_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_task_assignees_employee_id",
                table: "task_task_assignees",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_task_assignees_task_id_employee_id",
                table: "task_task_assignees",
                columns: new[] { "task_id", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_task_assignees");

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
    }
}
