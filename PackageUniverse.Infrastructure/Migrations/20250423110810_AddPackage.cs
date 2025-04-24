using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageUniverse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "PackageId",
                table: "Packages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "Packages");

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
