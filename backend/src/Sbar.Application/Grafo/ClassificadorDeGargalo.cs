using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Grafo;

public static class ClassificadorDeGargalo
{
    // gargalo(v) — TCC seção 4.2.6: carga(v) > μ_b + σ_b (estritamente maior — carga igual ao
    // limiar não conta como gargalo). Para bandas de exatamente 2 nós, μ+σ é sempre igual ao
    // maior dos dois valores (identidade algébrica), então
    // nenhum dos dois nunca é gargalo — comportamento esperado, não um bug.
    public static IReadOnlySet<Guid> Classificar(
        EstudoDeCaso estudoDeCaso,
        IReadOnlyDictionary<Guid, int> cargas,
        IReadOnlyDictionary<Banda, EstatisticasDeBanda> estatisticasPorBanda)
    {
        var gargalos = new HashSet<Guid>();

        foreach (var no in estudoDeCaso.Nos)
        {
            var estatisticas = estatisticasPorBanda[no.Banda];
            var limiar = estatisticas.Media + estatisticas.DesvioPadrao;

            if (cargas[no.Id] > limiar)
                gargalos.Add(no.Id);
        }

        return gargalos;
    }
}
