using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaJustificativaDaAresta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Justificativa",
                table: "Arestas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                // Backfill (decisão D11, opção 1): as arestas já gravadas recebem o texto-padrão e a interface as
                // marca como pendentes de revisão. Só vale para as linhas existentes: o mapeamento não declara
                // valor padrão, então toda aresta nova precisa informar a justificativa (RN03).
                defaultValue: "Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Justificativa",
                table: "Arestas");
        }
    }
}
