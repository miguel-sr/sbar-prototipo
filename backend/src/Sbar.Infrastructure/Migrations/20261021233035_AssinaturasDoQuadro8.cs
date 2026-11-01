using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssinaturasDoQuadro8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PerfisArquiteturais",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "Nome",
                value: "Monólito Modular (provisório)");

            migrationBuilder.UpdateData(
                table: "PerfisArquiteturais",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "Nome", "ValorVOL" },
                values: new object[] { "Microsserviços (provisório)", 2.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PerfisArquiteturais",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "Nome",
                value: "Monólito Modular (reconstruído)");

            migrationBuilder.UpdateData(
                table: "PerfisArquiteturais",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "Nome", "ValorVOL" },
                values: new object[] { "Microsserviços (reconstruído)", 4.2000000000000002 });
        }
    }
}
