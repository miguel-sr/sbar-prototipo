using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Grafo;

public static class ConstrutorDeContextoDoMapa
{
    public static ContextoDoMapa Construir(EstudoDeCaso estudoDeCaso)
    {
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);
        var comunidades = DetectorDeComunidades.Detectar(estudoDeCaso);
        var estatisticasPorBanda = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);
        var gargalos = ClassificadorDeGargalo.Classificar(estudoDeCaso, cargas, estatisticasPorBanda);
        var fluxos = estudoDeCaso.Fluxos
            .Select(f =>
            {
                var avaliacao = estudoDeCaso.AvaliarFluxo(f);
                return new FluxoPrincipal(f.Id, f.Nome, f.NosIds, avaliacao.BandasPercorridas, avaliacao.Valido, avaliacao.EtapaAusente);
            })
            .ToList();

        var grausEntrada = estudoDeCaso.Nos.ToDictionary(n => n.Id, _ => 0);
        var grausSaida = estudoDeCaso.Nos.ToDictionary(n => n.Id, _ => 0);

        foreach (var aresta in estudoDeCaso.Arestas)
        {
            grausSaida[aresta.NoOrigemId]++;
            grausEntrada[aresta.NoDestinoId]++;
        }

        var bandasOcupadas = estudoDeCaso.Nos.Select(n => n.Banda).ToHashSet();

        return new ContextoDoMapa(
            EstudoDeCaso: estudoDeCaso,
            N: estudoDeCaso.Nos.Count,
            M: estudoDeCaso.Arestas.Count,
            BandasOcupadas: bandasOcupadas,
            Cargas: cargas,
            GrausEntrada: grausEntrada,
            GrausSaida: grausSaida,
            Comunidades: comunidades,
            EstatisticasPorBanda: estatisticasPorBanda,
            Gargalos: gargalos,
            Fluxos: fluxos);
    }
}
