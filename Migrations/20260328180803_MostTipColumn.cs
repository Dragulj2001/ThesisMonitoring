using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class MostTipColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tip",
                table: "mostovi",
                type: "text",
                nullable: false,
                defaultValue: "Dva stuba");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tip",
                table: "mostovi");
        }
    }
}
