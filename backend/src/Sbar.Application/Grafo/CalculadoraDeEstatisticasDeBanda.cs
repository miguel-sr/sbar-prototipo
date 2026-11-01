using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Grafo;

public static class CalculadoraDeEstatisticasDeBanda
{
    // μ_b, σ_b — TCC seção 4.2.6. σ é o desvio padrão POPULACIONAL (não amostral) — é o que faz
    // uma banda de 1 único nó resultar em σ=0 (POP-06). Bandas sem nenhum nó não aparecem no
    // resultado (banda vazia).
    public static IReadOnlyDictionary<Banda, EstatisticasDeBanda> Calcular(
        EstudoDeCaso estudoDeCaso, IReadOnlyDictionary<Guid, int> cargas)
    {
        var resultado = new Dictionary<Banda, EstatisticasDeBanda>();

        foreach (var grupo in estudoDeCaso.Nos.GroupBy(n => n.Banda))
        {
            var cargasDaBanda = grupo.Select(n => (double)cargas[n.Id]).ToList();
            var media = cargasDaBanda.Average();
            var variancia = cargasDaBanda.Select(c => Math.Pow(c - media, 2)).Average();

            resultado[grupo.Key] = new EstatisticasDeBanda(media, Math.Sqrt(variancia));
        }

        return resultado;
    }
}
