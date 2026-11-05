using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sbar.Api.Swagger;

// Sem isto, o Swagger documenta os enums (TipoDeNo, Banda, TipoDeAresta, Eixo) como número —
// o JsonStringEnumConverter registrado em Program.cs faz a API aceitar/devolver o nome
// ("Ator", "Sincrona") em tempo de execução, mas o Swashbuckle não sabe disso por padrão
// (gera o schema por reflexão, sem olhar os JsonConverters configurados). Sem este filtro, quem
// ler só o Swagger tentaria enviar "0" e receberia 400 — pegadinha real, documentada aqui como
// as outras da seção 4.2 do ARQUITETURA.md.
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        schema.Enum.Clear();
        schema.Type = "string";
        schema.Format = null;

        foreach (var nome in Enum.GetNames(context.Type))
            schema.Enum.Add(new OpenApiString(nome));
    }
}
