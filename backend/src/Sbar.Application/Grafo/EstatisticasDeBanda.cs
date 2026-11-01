namespace Sbar.Application.Grafo;

// μ_b, σ_b — TCC seção 4.2.6 (σ é o desvio padrão POPULACIONAL da banda)
public sealed record EstatisticasDeBanda(double Media, double DesvioPadrao);
