namespace Sbar.Domain.Metricas;

// d(Servico, Estilo) — TCC seção 4.6. PercentualDoMaximo = distância / 5 × 100 (a distância máxima possível é 5, pois os
// eixos vão de 0 a 5 e Σ p_i = 1). Contribuicoes: p_i × (Norm_i^Serviço − Norm_i^Estilo)² dos eixos aplicáveis, sem
// arredondar; a soma delas é a distância ao quadrado (Quadros 26/34).
public sealed record DistanciaPerfil(
    Guid PerfilId,
    string PerfilNome,
    double Distancia,
    double PercentualDoMaximo,
    IReadOnlyList<ContribuicaoDeEixo> Contribuicoes);
