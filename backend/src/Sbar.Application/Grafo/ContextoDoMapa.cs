using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Grafo;

// Dados derivados do Mapa de Interações, calculados uma única vez e compartilhados pelas
// dez calculadoras de eixo (ARQUITETURA.md seção 3.1) — evita recalcular a mesma coisa dez
// vezes e evita definições divergentes de carga/comunidade/gargalo entre calculadoras.
// Carrega o próprio EstudoDeCaso porque algumas fórmulas (ESC, INT, TR...) precisam
// examinar nós/arestas crus além dos dados já derivados.
public sealed record ContextoDoMapa(
    EstudoDeCaso EstudoDeCaso,
    int N,
    int M,
    IReadOnlySet<Banda> BandasOcupadas,
    IReadOnlyDictionary<Guid, int> Cargas,
    IReadOnlyDictionary<Guid, int> GrausEntrada,
    IReadOnlyDictionary<Guid, int> GrausSaida,
    ResultadoDeComunidades Comunidades,
    IReadOnlyDictionary<Banda, EstatisticasDeBanda> EstatisticasPorBanda,
    IReadOnlySet<Guid> Gargalos,
    IReadOnlyList<FluxoPrincipal> Fluxos)
{
    // F — conjunto de fluxos principais VALIDADOS no Mapa (TCC 4.4.1): só os cadastrados cujo caminho dirigido existe.
    public IReadOnlyList<FluxoPrincipal> FluxosPrincipais => Fluxos.Where(f => f.Valido).ToList();
}
