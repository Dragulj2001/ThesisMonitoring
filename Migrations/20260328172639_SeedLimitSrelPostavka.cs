using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisWebApp.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class SeedLimitSrelPostavka : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO postavke (kljuc, vrednost) VALUES ('LimitSrel', '0.02') ON CONFLICT (kljuc) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM postavke WHERE kljuc = 'LimitSrel';");
        }
    }
}
