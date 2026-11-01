using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesDeEixo",
                columns: table => new
                {
                    Eixo = table.Column<string>(type: "text", nullable: false),
                    LimiteMinimo = table.Column<double>(type: "double precision", nullable: false),
                    LimiteMaximo = table.Column<double>(type: "double precision", nullable: false),
                    Peso = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesDeEixo", x => x.Eixo);
                });

            migrationBuilder.CreateTable(
                name: "EstudosDeCaso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    DominioDescricao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstudosDeCaso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfisArquiteturais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ValorCO = table.Column<double>(type: "double precision", nullable: false),
                    ValorESC = table.Column<double>(type: "double precision", nullable: false),
                    ValorINT = table.Column<double>(type: "double precision", nullable: false),
                    ValorUX = table.Column<double>(type: "double precision", nullable: false),
                    ValorDISP = table.Column<double>(type: "double precision", nullable: false),
                    ValorFLEX = table.Column<double>(type: "double precision", nullable: false),
                    ValorTR = table.Column<double>(type: "double precision", nullable: false),
                    ValorAUT = table.Column<double>(type: "double precision", nullable: false),
                    ValorVOL = table.Column<double>(type: "double precision", nullable: false),
                    ValorDEP = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisArquiteturais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Arestas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoOrigemId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Peso = table.Column<int>(type: "integer", nullable: false),
                    EstudoDeCasoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arestas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arestas_EstudosDeCaso_EstudoDeCasoId",
                        column: x => x.EstudoDeCasoId,
                        principalTable: "EstudosDeCaso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Banda = table.Column<string>(type: "text", nullable: false),
                    EstudoDeCasoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nos_EstudosDeCaso_EstudoDeCasoId",
                        column: x => x.EstudoDeCasoId,
                        principalTable: "EstudosDeCaso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracoesDeEixo",
                columns: new[] { "Eixo", "LimiteMaximo", "LimiteMinimo", "Peso" },
                values: new object[,]
                {
                    { "AUT", 1.0, 0.0, 0.10000000000000001 },
                    { "CO", 40.0, 6.0, 0.10000000000000001 },
                    { "DEP", 1.0, 0.20000000000000001, 0.10000000000000001 },
                    { "DISP", 1.0, 0.0, 0.10000000000000001 },
                    { "ESC", 1.0, 0.0, 0.10000000000000001 },
                    { "FLEX", 1.0, 0.0, 0.10000000000000001 },
                    { "INT", 1.0, 0.0, 0.10000000000000001 },
                    { "TR", 1.0, 0.0, 0.10000000000000001 },
                    { "UX", 3.0, 0.0, 0.10000000000000001 },
                    { "VOL", 3.0, 1.0, 0.10000000000000001 }
                });

            migrationBuilder.InsertData(
                table: "PerfisArquiteturais",
                columns: new[] { "Id", "Nome", "ValorAUT", "ValorCO", "ValorDEP", "ValorDISP", "ValorESC", "ValorFLEX", "ValorINT", "ValorTR", "ValorUX", "ValorVOL" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Monólito Modular (reconstruído)", 1.0, 2.0, 3.0, 3.0, 1.5, 1.5, 1.0, 1.5, 2.5, 3.0 },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Microsserviços (reconstruído)", 4.5, 4.0, 4.0, 4.0, 4.0, 4.0, 3.5, 2.5, 2.5, 4.2000000000000002 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Arestas_EstudoDeCasoId",
                table: "Arestas",
                column: "EstudoDeCasoId");

            migrationBuilder.CreateIndex(
                name: "IX_Nos_EstudoDeCasoId",
                table: "Nos",
                column: "EstudoDeCasoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Arestas");

            migrationBuilder.DropTable(
                name: "ConfiguracoesDeEixo");

            migrationBuilder.DropTable(
                name: "Nos");

            migrationBuilder.DropTable(
                name: "PerfisArquiteturais");

            migrationBuilder.DropTable(
                name: "EstudosDeCaso");
        }
    }
}
