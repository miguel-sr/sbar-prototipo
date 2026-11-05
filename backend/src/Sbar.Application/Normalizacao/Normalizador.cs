using Sbar.Domain.ValueObjects;

namespace Sbar.Application.Normalizacao;

public static class Normalizador
{
    // Normalização (0 a 5) — docs/dominio/formulas-metricas.md:
    // Norm_i = 5 × (M_i − min) / (max − min), resultado limitado a [0, 5].
    public static double Normalizar(double valorBruto, LimitesDeEixo limites)
    {
        var normalizado = 5 * (valorBruto - limites.Minimo) / (limites.Maximo - limites.Minimo);
        return Math.Clamp(normalizado, 0, 5);
    }
}
