using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                schema: "auth",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Course",
                schema: "auth",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassName",
                schema: "auth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Course",
                schema: "auth",
                table: "users");
        }
    }
}
