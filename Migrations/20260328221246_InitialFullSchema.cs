using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThesisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialFullSchema : Migration
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
                    naziv = table.Column<string>(type: "text", nullable: false),
                    tip = table.Column<string>(type: "text", nullable: false),
                    limit_alarma = table.Column<double>(type: "double precision", nullable: false),
                    limit_srel = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mostovi", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "lokacije_predef",
                columns: table => new
                {
                    ime = table.Column<string>(type: "text", nullable: false),
                    y_koord = table.Column<double>(type: "double precision", nullable: true),
                    x_koord = table.Column<double>(type: "double precision", nullable: true),
                    z_koord = table.Column<double>(type: "double precision", nullable: true),
                    most_id = table.Column<int>(type: "integer", nullable: false),
                    uloga = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lokacije_predef", x => x.ime);
                    table.ForeignKey(
                        name: "FK_lokacije_predef_mostovi_most_id",
                        column: x => x.most_id,
                        principalTable: "mostovi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "merenja",
                columns: table => new
                {
                    ime = table.Column<string>(type: "text", nullable: false),
                    dt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    y = table.Column<double>(type: "double precision", nullable: true),
                    x = table.Column<double>(type: "double precision", nullable: true),
                    z = table.Column<double>(type: "double precision", nullable: true),
                    dy = table.Column<double>(type: "double precision", nullable: true),
                    dx = table.Column<double>(type: "double precision", nullable: true),
                    dz = table.Column<double>(type: "double precision", nullable: true),
                    d3d = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merenja", x => new { x.ime, x.dt });
                    table.ForeignKey(
                        name: "FK_merenja_lokacije_predef_ime",
                        column: x => x.ime,
                        principalTable: "lokacije_predef",
                        principalColumn: "ime",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lokacije_predef_most_id",
                table: "lokacije_predef",
                column: "most_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merenja");

            migrationBuilder.DropTable(
                name: "postavke");

            migrationBuilder.DropTable(
                name: "lokacije_predef");

            migrationBuilder.DropTable(
                name: "mostovi");
        }
    }
}
