using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThesisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class MostoviSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mostovi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    naziv = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mostovi", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "mostovi",
                columns: new[] { "naziv" },
                values: new object[] { "Podrazumevani most" });

            migrationBuilder.AddColumn<int>(
                name: "most_id",
                table: "lokacije_predef",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE lokacije_predef SET most_id = (SELECT id FROM mostovi ORDER BY id LIMIT 1)
                WHERE most_id IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "most_id",
                table: "lokacije_predef",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lokacije_predef_most_id",
                table: "lokacije_predef",
                column: "most_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lokacije_predef_mostovi_most_id",
                table: "lokacije_predef",
                column: "most_id",
                principalTable: "mostovi",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lokacije_predef_mostovi_most_id",
                table: "lokacije_predef");

            migrationBuilder.DropTable(
                name: "mostovi");

            migrationBuilder.DropIndex(
                name: "IX_lokacije_predef_most_id",
                table: "lokacije_predef");

            migrationBuilder.DropColumn(
                name: "most_id",
                table: "lokacije_predef");
        }
    }
}
