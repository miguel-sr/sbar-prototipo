using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sbar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NomesComLimiteEOrdemDeCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes da API validar o tamanho, qualquer texto cabia (a coluna era "text"). Corta o que já
            // estiver acima dos novos limites — senão o AlterColumn abaixo falha e a API não sobe.
            migrationBuilder.Sql("""UPDATE "Nos" SET "Nome" = LEFT("Nome", 100) WHERE LENGTH("Nome") > 100;""");
            migrationBuilder.Sql("""UPDATE "EstudosDeCaso" SET "Nome" = LEFT("Nome", 120) WHERE LENGTH("Nome") > 120;""");
            migrationBuilder.Sql("""UPDATE "EstudosDeCaso" SET "DominioDescricao" = LEFT("DominioDescricao", 1000) WHERE LENGTH("DominioDescricao") > 1000;""");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Nos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "Nos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "EstudosDeCaso",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DominioDescricao",
                table: "EstudosDeCaso",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "Arestas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "Nos");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "Arestas");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Nos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "EstudosDeCaso",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "DominioDescricao",
                table: "EstudosDeCaso",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
