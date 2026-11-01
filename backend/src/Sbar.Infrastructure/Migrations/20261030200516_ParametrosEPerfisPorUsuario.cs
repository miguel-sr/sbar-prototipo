using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParametrosEPerfisPorUsuario : Migration
    {
        /// <inheritdoc />
        // Os parametros, perfis e versoes que ja existem (semeados pela InitialCreate e editados ate hoje) ficam como
        // legado SEM dono: nada e apagado aqui. O dono do legado os reivindica ao se cadastrar (ReivindicadorDeCasosLegados);
        // contas novas recebem copias dos padroes (PadroesDoSistema). O Id das configuracoes ganha um uuid por linha.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VersoesDeParametros_Numero",
                table: "VersoesDeParametros");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConfiguracoesDeEixo",
                table: "ConfiguracoesDeEixo");

            migrationBuilder.AddColumn<Guid>(
                name: "DonoId",
                table: "VersoesDeParametros",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DonoId",
                table: "PerfisArquiteturais",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ConfiguracoesDeEixo",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "DonoId",
                table: "ConfiguracoesDeEixo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConfiguracoesDeEixo",
                table: "ConfiguracoesDeEixo",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDeParametros_DonoId_Numero",
                table: "VersoesDeParametros",
                columns: new[] { "DonoId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfisArquiteturais_DonoId",
                table: "PerfisArquiteturais",
                column: "DonoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesDeEixo_DonoId_Eixo",
                table: "ConfiguracoesDeEixo",
                columns: new[] { "DonoId", "Eixo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VersoesDeParametros_DonoId_Numero",
                table: "VersoesDeParametros");

            migrationBuilder.DropIndex(
                name: "IX_PerfisArquiteturais_DonoId",
                table: "PerfisArquiteturais");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConfiguracoesDeEixo",
                table: "ConfiguracoesDeEixo");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracoesDeEixo_DonoId_Eixo",
                table: "ConfiguracoesDeEixo");

            migrationBuilder.DropColumn(
                name: "DonoId",
                table: "VersoesDeParametros");

            migrationBuilder.DropColumn(
                name: "DonoId",
                table: "PerfisArquiteturais");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ConfiguracoesDeEixo");

            migrationBuilder.DropColumn(
                name: "DonoId",
                table: "ConfiguracoesDeEixo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConfiguracoesDeEixo",
                table: "ConfiguracoesDeEixo",
                column: "Eixo");

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
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Monólito Modular (provisório)", 1.0, 2.0, 3.0, 3.0, 1.5, 1.5, 1.0, 1.5, 2.5, 3.0 },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Microsserviços (provisório)", 4.5, 4.0, 4.0, 4.0, 4.0, 4.0, 3.5, 2.5, 2.5, 2.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VersoesDeParametros_Numero",
                table: "VersoesDeParametros",
                column: "Numero",
                unique: true);
        }
    }
}
