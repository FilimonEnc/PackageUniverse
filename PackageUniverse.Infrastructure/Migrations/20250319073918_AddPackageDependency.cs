using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PackageUniverse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsRecomended",
                table: "Packages",
                newName: "IsRecommended");

            migrationBuilder.RenameColumn(
                name: "Discription",
                table: "Packages",
                newName: "Description");

            migrationBuilder.CreateTable(
                name: "PackageDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    PackageId = table.Column<int>(type: "integer", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDependencies_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_PackageId",
                table: "PackageDependencies",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageDependencies");

            migrationBuilder.RenameColumn(
                name: "IsRecommended",
                table: "Packages",
                newName: "IsRecomended");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Packages",
                newName: "Discription");
        }
    }
}
