using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPostavke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "postavke",
                columns: table => new
                {
                    kljuc = table.Column<string>(type: "text", nullable: false),
                    vrednost = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_postavke", x => x.kljuc);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "postavke");
        }
    }
}
