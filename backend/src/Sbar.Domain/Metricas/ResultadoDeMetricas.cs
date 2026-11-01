using Sbar.Domain.ValueObjects;

namespace Sbar.Domain.Metricas;

// Recomendacao é null quando não há nenhum PerfilArquitetural cadastrado para comparar (RF-08/D1)
// NosGargalo: ids de todos os Nós com gargalo(v) (TCC 4.2.6), de qualquer Banda — a tela do Mapa precisa
// destacá-los mesmo quando não entram em DISP (Ator/Frontstage/Externo).
// Comunidades/ModularidadeQ/Avisos: partição do Mapa (TCC revisado 4.4.1) — o TCC manda registrar Q junto dos
// resultados; ModularidadeQ é null quando o Mapa não tem arestas (Q indefinido). Bandas: μ/σ/limiar de cada
// Banda ocupada (4.2.6). Fluxos: fluxos principais cadastrados, com validade (RF05).
// VersaoDosParametros: versão dos limites, pesos e assinaturas usada nesta análise (RF11, US06).
public sealed record ResultadoDeMetricas(
    IReadOnlyList<ResultadoDeEixo> Eixos,
    Recomendacao? Recomendacao,
    IReadOnlyList<Guid> NosGargalo,
    IReadOnlyList<ComunidadeDoResultado> Comunidades,
    double? ModularidadeQ,
    IReadOnlyList<string> Avisos,
    IReadOnlyList<EstatisticaDaBandaDoResultado> Bandas,
    IReadOnlyList<FluxoDoResultado> Fluxos,
    VersaoDeParametrosInfo VersaoDosParametros);
