using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// Uma linha da Matriz de Rastreabilidade — TCC seção 4.6.1 (RF12), RNF-01: todo resultado
// exibido deve poder ser rastreado até o Nó/Aresta de origem. Categoria: gargalo | aresta-critica |
// aresta-externa | comunidade | fluxo | no-frontstage | banda-ocupada | pares-da-banda | volume-da-aresta. NosIds/ArestasIds: os elementos do Mapa que originam a linha.
public sealed record LinhaDeRastreabilidade(
    string Elemento,
    string NoOuAresta,
    string Metrica,
    IReadOnlyList<Eixo> Eixos,
    string Contribuicao,
    string Categoria,
    IReadOnlyList<Guid> NosIds,
    IReadOnlyList<Guid> ArestasIds);

public static class CategoriaDeRastreabilidade
{
    public const string Gargalo = "gargalo";
    public const string ArestaCritica = "aresta-critica";
    public const string ArestaExterna = "aresta-externa";
    public const string Comunidade = "comunidade";
    public const string Fluxo = "fluxo";
    public const string NoFrontstage = "no-frontstage";
    public const string BandaOcupada = "banda-ocupada";
    public const string ParesDaBanda = "pares-da-banda";
    public const string VolumeDaAresta = "volume-da-aresta";
}
