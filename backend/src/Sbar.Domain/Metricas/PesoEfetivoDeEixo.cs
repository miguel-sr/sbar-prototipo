using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// Peso de um eixo na distância euclidiana ponderada quando algum eixo é "não aplicável" (RN07/RN08): os pesos dos eixos
// aplicáveis são renormalizados para somar 1, p'_i = p_i / Σ p_j (j aplicável). PesoConfigurado é o p_i do conjunto de
// parâmetros; PesoEfetivo é o p'_i realmente usado. Com os dez eixos aplicáveis, os dois são iguais.
public sealed record PesoEfetivoDeEixo(Eixo Eixo, double PesoConfigurado, double PesoEfetivo);
