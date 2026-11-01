using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaVersoesDeParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VersoesDeParametros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersoesDeParametros", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDeParametros_Numero",
                table: "VersoesDeParametros",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VersoesDeParametros");
        }
    }
}
