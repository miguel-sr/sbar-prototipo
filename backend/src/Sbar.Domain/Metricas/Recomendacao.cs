namespace Sbar.Domain.Metricas;

// RF09/RN09 — o perfil de referência de menor distância é um SUBSÍDIO à decisão do arquiteto, não uma decisão (TCC 4.6, 5.1).
// Margem = segunda menor distância − menor distância (diferença pequena = perfil intermediário; null com menos de 2 perfis).
// DistanciaMaxima = 5, para ler cada distância também como percentual do máximo.
public sealed record Recomendacao(
    string EstiloRecomendado,
    IReadOnlyList<DistanciaPerfil> Distancias,
    double? Margem,
    double DistanciaMaxima);
