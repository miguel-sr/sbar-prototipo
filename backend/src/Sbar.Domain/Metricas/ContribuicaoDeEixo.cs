using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// p_i × (Norm_i^Serviço − Norm_i^Estilo)² — contribuição do eixo i para o quadrado da distância (Quadros 26/34, TCC 6.10.1/7.5).
public sealed record ContribuicaoDeEixo(Eixo Eixo, double Contribuicao);
