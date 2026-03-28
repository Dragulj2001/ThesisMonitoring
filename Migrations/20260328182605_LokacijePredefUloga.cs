using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class LokacijePredefUloga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uloga",
                table: "lokacije_predef",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uloga",
                table: "lokacije_predef");
        }
    }
}
