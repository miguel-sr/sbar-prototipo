using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraINT : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.INT;

    // Eixo 3 — Integração (INT), TCC seção 4.4.2: INT = |E_Externo| / M.
    // Caso especial: M = 0 → não aplicável.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        if (contexto.M == 0)
            return new ResultadoDeMetricaBruta(false, null, "M = 0 — sem arestas no grafo");

        var estudoDeCaso = contexto.EstudoDeCaso;
        var bandaPorNo = estudoDeCaso.Nos.ToDictionary(n => n.Id, n => n.Banda);

        var externas = estudoDeCaso.Arestas.Count(a =>
            bandaPorNo[a.NoOrigemId] == Banda.Externo || bandaPorNo[a.NoDestinoId] == Banda.Externo);

        return new ResultadoDeMetricaBruta(true, (double)externas / contexto.M, null);
    }
}
