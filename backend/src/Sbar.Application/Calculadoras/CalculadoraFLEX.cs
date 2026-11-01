using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraFLEX : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.FLEX;

    // Eixo 6 — Flexibilidade/Evolução (FLEX), TCC revisado seção 4.4.2 (comunidades: 4.4.1): para
    // cada nó interno v (Backstage∪Suporte), isolamento(v) = 1 − arestas_externas(v)/grau_total(v)
    // (= 1 se grau_total(v) = 0), em que arestas_externas(v) conta as arestas (de entrada ou de
    // saída) que ligam v a nós de OUTRA comunidade; FLEX = média de isolamento(v). A comunidade de
    // cada nó vem da detecção por modularidade (DetectorDeComunidades) — com a definição inicial
    // (conectividade fraca) FLEX era sempre 1,0; agora ela distingue a estrutura do serviço.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var nosInternos = contexto.EstudoDeCaso.Nos
            .Where(n => n.Banda == Banda.Backstage || n.Banda == Banda.Suporte)
            .ToList();

        if (nosInternos.Count == 0)
            return new ResultadoDeMetricaBruta(false, null, "Nenhum nó nas bandas Backstage/Suporte");

        var somaIsolamento = nosInternos.Sum(n => Isolamento(n.Id, contexto));

        return new ResultadoDeMetricaBruta(true, somaIsolamento / nosInternos.Count, null);
    }

    private static double Isolamento(Guid noId, ContextoDoMapa contexto)
    {
        var grauTotal = contexto.GrausEntrada[noId] + contexto.GrausSaida[noId];
        if (grauTotal == 0)
            return 1;

        var comunidades = contexto.Comunidades.IndicePorNo;
        var comunidadeDoNo = comunidades[noId];
        var arestasExternas = contexto.EstudoDeCaso.Arestas.Count(a =>
            (a.NoOrigemId == noId && comunidades[a.NoDestinoId] != comunidadeDoNo) ||
            (a.NoDestinoId == noId && comunidades[a.NoOrigemId] != comunidadeDoNo));

        return 1 - (double)arestasExternas / grauTotal;
    }
}
