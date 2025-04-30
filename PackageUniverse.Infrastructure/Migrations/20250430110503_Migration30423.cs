using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageUniverse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Migration30423 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "NugetId",
                table: "Packages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_NugetId",
                table: "Packages",
                column: "NugetId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Packages_NugetId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "NugetId",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Packages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
