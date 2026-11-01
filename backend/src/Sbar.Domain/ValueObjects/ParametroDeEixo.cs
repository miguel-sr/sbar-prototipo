using Sbar.Domain.Enums;

namespace Sbar.Domain.ValueObjects;

// Limites de normalização (Quadro 7) e peso p_i (4.5.1) de um eixo, como parte de um conjunto de parâmetros versionado (RF11).
public sealed record ParametroDeEixo(Eixo Eixo, double Minimo, double Maximo, double Peso);
