namespace Sbar.Domain.ValueObjects;

// M_i_min / M_i_max de um eixo — Quadro 6 do TCC revisado (docs/dominio/formulas-metricas.md)
public sealed record LimitesDeEixo(double Minimo, double Maximo);
