using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi1.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadInstructorToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeadInstructorId",
                table: "Courses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadInstructorId",
                table: "Courses");
        }
    }
}
