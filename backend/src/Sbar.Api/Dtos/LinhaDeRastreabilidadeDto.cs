using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Api.Dtos;

/// <summary>Uma linha da Matriz de Rastreabilidade: um elemento do Blueprint e como ele contribui pro Radar.</summary>
/// <param name="Elemento">Descrição do elemento (ex.: "Validação do pedido (Backstage)").</param>
/// <param name="NoOuAresta">Identificação do Nó/Aresta de origem, com a categoria entre parênteses ("(gargalo)", "(crítica)", "(externa)", "(fluxo)", "(touchpoint)") ou "Comunidade Ci (n nós)".</param>
/// <param name="Metrica">Métrica bruta envolvida (ex.: "carga(v), gargalo(v)").</param>
/// <param name="Eixos">Eixo(s) do Radar que esse elemento alimenta (vazio para um gargalo em banda que não entra em nenhum eixo).</param>
/// <param name="Contribuicao">Explicação textual de como o elemento afeta o eixo, com os valores calculados.</param>
/// <param name="Categoria">gargalo | aresta-critica | aresta-externa | comunidade | fluxo | no-frontstage.</param>
/// <param name="NosIds">Ids dos Nós do Mapa que originam a linha.</param>
/// <param name="ArestasIds">Ids das Arestas do Mapa que originam a linha.</param>
public record LinhaDeRastreabilidadeDto(
    string Elemento, string NoOuAresta, string Metrica, IReadOnlyList<Eixo> Eixos, string Contribuicao,
    string Categoria, IReadOnlyList<Guid> NosIds, IReadOnlyList<Guid> ArestasIds)
{
    public static LinhaDeRastreabilidadeDto DeLinha(LinhaDeRastreabilidade linha) => new(
        linha.Elemento, linha.NoOuAresta, linha.Metrica, linha.Eixos, linha.Contribuicao,
        linha.Categoria, linha.NosIds, linha.ArestasIds);
}
