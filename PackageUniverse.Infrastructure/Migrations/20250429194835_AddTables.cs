using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PackageUniverse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageDependencies_Packages_PackageId",
                table: "PackageDependencies");

            migrationBuilder.DropIndex(
                name: "IX_PackageDependencies_PackageId",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PackageDependencies");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Packages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Packages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecommended",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SourceVersionId",
                table: "PackageDependencies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetFramework",
                table: "PackageDependencies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TargetPackageId",
                table: "PackageDependencies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetVersionRange",
                table: "PackageDependencies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PackageVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "boolean", nullable: false),
                    TargetFramework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PackageUrl = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageVersions_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_SourceVersionId",
                table: "PackageDependencies",
                column: "SourceVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_TargetPackageId",
                table: "PackageDependencies",
                column: "TargetPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_PackageId",
                table: "PackageVersions",
                column: "PackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDependencies_PackageVersions_SourceVersionId",
                table: "PackageDependencies",
                column: "SourceVersionId",
                principalTable: "PackageVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDependencies_Packages_TargetPackageId",
                table: "PackageDependencies",
                column: "TargetPackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageDependencies_PackageVersions_SourceVersionId",
                table: "PackageDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageDependencies_Packages_TargetPackageId",
                table: "PackageDependencies");

            migrationBuilder.DropTable(
                name: "PackageVersions");

            migrationBuilder.DropIndex(
                name: "IX_PackageDependencies_SourceVersionId",
                table: "PackageDependencies");

            migrationBuilder.DropIndex(
                name: "IX_PackageDependencies_TargetPackageId",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "IsRecommended",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SourceVersionId",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "TargetFramework",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "TargetPackageId",
                table: "PackageDependencies");

            migrationBuilder.DropColumn(
                name: "TargetVersionRange",
                table: "PackageDependencies");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Packages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Packages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageId",
                table: "Packages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PackageDependencies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PackageId",
                table: "PackageDependencies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "PackageDependencies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_PackageId",
                table: "PackageDependencies",
                column: "PackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageDependencies_Packages_PackageId",
                table: "PackageDependencies",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id");
        }
    }
}
