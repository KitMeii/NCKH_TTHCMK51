using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminService.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "role_change_audits",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldRole = table.Column<string>(type: "text", nullable: false),
                    NewRole = table.Column<string>(type: "text", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_change_audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                schema: "admin",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configs", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_change_audits_ChangedAtUtc",
                schema: "admin",
                table: "role_change_audits",
                column: "ChangedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_change_audits",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "system_configs",
                schema: "admin");
        }
    }
}
