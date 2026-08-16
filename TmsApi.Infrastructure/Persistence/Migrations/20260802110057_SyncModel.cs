using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi1.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnrolleAdt",
                table: "Enrollments",
                newName: "EnrolledAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnrolledAt",
                table: "Enrollments",
                newName: "EnrolleAdt");
        }
    }
}


