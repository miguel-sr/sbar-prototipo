using System.Globalization;

namespace Sbar.Domain.Services;

// Números em texto pt-BR (vírgula decimal) sem depender da cultura do servidor nem de ICU: a mesma análise gera o
// mesmo texto no Windows (cultura pt-BR) e no container Linux (cultura invariante) — RNF02.
public static class FormatadorDeNumeros
{
    public static string Formatar(double valor, int casas) =>
        valor.ToString("F" + casas, CultureInfo.InvariantCulture).Replace('.', ',');

    // Até 'casasMaximas' casas, sem zeros à direita (ex.: 0,95 | 1 | 0,333333).
    public static string FormatarSemZerosAFrente(double valor, int casasMaximas) =>
        valor.ToString("0." + new string('#', casasMaximas), CultureInfo.InvariantCulture).Replace('.', ',');
}
