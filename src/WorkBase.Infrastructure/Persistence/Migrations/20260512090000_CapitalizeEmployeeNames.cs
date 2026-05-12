using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CapitalizeEmployeeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE org_employees
                SET first_name = CONCAT(UPPER(LEFT(first_name, 1)), LOWER(SUBSTRING(first_name FROM 2))),
                    last_name  = CONCAT(UPPER(LEFT(last_name, 1)),  LOWER(SUBSTRING(last_name FROM 2)))
                WHERE first_name IS NOT NULL AND last_name IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot revert — original casing is unknown
        }
    }
}
