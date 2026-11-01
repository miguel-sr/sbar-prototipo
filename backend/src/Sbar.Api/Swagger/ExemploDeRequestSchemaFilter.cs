using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Sbar.Api.Requests;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sbar.Api.Swagger;

// Exemplos de payload prontos pro Swagger "Try it out" — POP-09
// pede "Swagger com exemplos/descrições", não só a descrição textual de cada campo.
public class ExemploDeRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type switch
        {
            Type t when t == typeof(CriarEstudoDeCasoRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Sistema de Pedidos Digital"),
                ["dominioDescricao"] = new OpenApiString("Pedidos de restaurante feitos pelo app do cliente"),
            },
            Type t when t == typeof(AtualizarEstudoDeCasoRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Sistema de Pedidos Digital (v2)"),
                ["dominioDescricao"] = new OpenApiString("Pedidos de restaurante feitos pelo app do cliente"),
            },
            Type t when t == typeof(AtualizarNoRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Carrinho/Checkout"),
                ["tipo"] = new OpenApiString("Touchpoint"),
            },
            Type t when t == typeof(AtualizarArestaRequest) => new OpenApiObject
            {
                ["noOrigemId"] = new OpenApiString("9d8bed89-2996-41d7-b36c-e46ce6674a5d"),
                ["noDestinoId"] = new OpenApiString("a1b2c3d4-0000-0000-0000-000000000000"),
                ["tipo"] = new OpenApiString("Assincrona"),
                ["peso"] = new OpenApiInteger(2),
                ["justificativa"] = new OpenApiString("Notificação ao cliente não bloqueia o fluxo do pedido (Blueprint, etapa 4)."),
            },
            Type t when t == typeof(CriarFluxoRequest) || t == typeof(AtualizarFluxoRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Pedido e preparo"),
                ["nosIds"] = new OpenApiArray
                {
                    new OpenApiString("9d8bed89-2996-41d7-b36c-e46ce6674a5d"),
                    new OpenApiString("a1b2c3d4-0000-0000-0000-000000000000"),
                },
            },
            Type t when t == typeof(CriarNoRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Carrinho/Checkout"),
                ["tipo"] = new OpenApiString("Touchpoint"),
            },
            Type t when t == typeof(CriarArestaRequest) => new OpenApiObject
            {
                ["noOrigemId"] = new OpenApiString("9d8bed89-2996-41d7-b36c-e46ce6674a5d"),
                ["noDestinoId"] = new OpenApiString("a1b2c3d4-0000-0000-0000-000000000000"),
                ["tipo"] = new OpenApiString("Sincrona"),
                ["peso"] = new OpenApiInteger(3),
                ["justificativa"] = new OpenApiString("Todo pedido passa pela validação antes de seguir (Blueprint, etapa 2)."),
            },
            Type t when t == typeof(CriarPerfilArquiteturalRequest) => new OpenApiObject
            {
                ["nome"] = new OpenApiString("Monólito Modular"),
                ["valorCO"] = new OpenApiDouble(2.0),
                ["valorESC"] = new OpenApiDouble(1.5),
                ["valorINT"] = new OpenApiDouble(1.0),
                ["valorUX"] = new OpenApiDouble(2.5),
                ["valorDISP"] = new OpenApiDouble(3.0),
                ["valorFLEX"] = new OpenApiDouble(1.5),
                ["valorTR"] = new OpenApiDouble(1.5),
                ["valorAUT"] = new OpenApiDouble(1.0),
                ["valorVOL"] = new OpenApiDouble(3.0),
                ["valorDEP"] = new OpenApiDouble(3.0),
            },
            Type t when t == typeof(AtualizarLimitesEPesosRequest) => new OpenApiObject
            {
                ["descricao"] = new OpenApiString("Recalibra o máximo de CO após os estudos de caso"),
                ["eixos"] = new OpenApiArray
                {
                    Eixo("CO", 6, 45, 0.1), Eixo("ESC", 0, 1, 0.1), Eixo("INT", 0, 1, 0.1), Eixo("UX", 0, 3, 0.1), Eixo("DISP", 0, 1, 0.1),
                    Eixo("FLEX", 0, 1, 0.1), Eixo("TR", 0, 1, 0.1), Eixo("AUT", 0, 1, 0.1), Eixo("VOL", 1, 3, 0.1), Eixo("DEP", 0.2, 1, 0.1),
                },
            },
            _ => schema.Example,
        };
    }

    private static OpenApiObject Eixo(string eixo, double minimo, double maximo, double peso) => new()
    {
        ["eixo"] = new OpenApiString(eixo),
        ["minimo"] = new OpenApiDouble(minimo),
        ["maximo"] = new OpenApiDouble(maximo),
        ["peso"] = new OpenApiDouble(peso),
    };
}
