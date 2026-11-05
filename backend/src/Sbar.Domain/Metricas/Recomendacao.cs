using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// RF09/RN09 — o perfil de referência de menor distância é um SUBSÍDIO à decisão do arquiteto, não uma decisão (TCC 4.6, 5.1).
// Margem = segunda menor distância − menor distância (diferença pequena = perfil intermediário; null com menos de 2 perfis).
// DistanciaMaxima = 5, para ler cada distância também como percentual do máximo.
// PesosEfetivos = pesos p'_i usados na distância (renormalizados entre os eixos aplicáveis, somam 1); EixosOmitidos = eixos
// "não aplicáveis" que ficaram fora dela (RN07/RN08).
public sealed record Recomendacao(
    string EstiloRecomendado,
    IReadOnlyList<DistanciaPerfil> Distancias,
    double? Margem,
    double DistanciaMaxima,
    IReadOnlyList<PesoEfetivoDeEixo> PesosEfetivos,
    IReadOnlyList<Eixo> EixosOmitidos);
