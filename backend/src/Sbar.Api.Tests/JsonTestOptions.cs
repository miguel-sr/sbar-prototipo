using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sbar.Api.Tests;

// PropertyNameCaseInsensitive é essencial: sem ele, o HttpClient nunca casa o "id" (camelCase, vindo
// do servidor) com a propriedade "Id" (PascalCase) dos records de Dto, e o deserializador devolve o
// valor padrão do tipo (Guid.Empty) em silêncio, sem lançar exceção — já foi um bug real nesta sessão.
public static class JsonTestOptions
{
    public static readonly JsonSerializerOptions Padrao = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
