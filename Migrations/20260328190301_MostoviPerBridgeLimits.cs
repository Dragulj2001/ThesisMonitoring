using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisWebApp.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class MostoviPerBridgeLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "limit_alarma",
                table: "mostovi",
                type: "double precision",
                nullable: false,
                defaultValue: 0.15);

            migrationBuilder.AddColumn<double>(
                name: "limit_srel",
                table: "mostovi",
                type: "double precision",
                nullable: false,
                defaultValue: 0.02);

            migrationBuilder.Sql("""
                UPDATE mostovi mo
                SET limit_alarma = COALESCE(
                    (SELECT NULLIF(trim(p.vrednost), '')::double precision FROM postavke p WHERE p.kljuc = 'LimitAlarma' LIMIT 1),
                    mo.limit_alarma),
                    limit_srel = COALESCE(
                    (SELECT NULLIF(trim(p.vrednost), '')::double precision FROM postavke p WHERE p.kljuc = 'LimitSrel' LIMIT 1),
                    mo.limit_srel);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "limit_alarma",
                table: "mostovi");

            migrationBuilder.DropColumn(
                name: "limit_srel",
                table: "mostovi");
        }
    }
}
