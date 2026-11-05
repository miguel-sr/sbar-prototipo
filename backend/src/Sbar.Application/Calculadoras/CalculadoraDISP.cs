using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraDISP : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.DISP;

    // Eixo 5 — Disponibilidade (DISP), TCC seção 4.4.2:
    // DISP = 1 − [Σ_{v∈Backstage∪Suporte} gargalo(v)] / (n_Backstage + n_Suporte).
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var nosInternos = contexto.EstudoDeCaso.Nos
            .Where(n => n.Banda == Banda.Backstage || n.Banda == Banda.Suporte)
            .ToList();

        if (nosInternos.Count == 0)
            return new ResultadoDeMetricaBruta(false, null, "Nenhum nó nas bandas Backstage/Suporte");

        var gargalos = nosInternos.Count(n => contexto.Gargalos.Contains(n.Id));

        return new ResultadoDeMetricaBruta(true, 1 - (double)gargalos / nosInternos.Count, null);
    }
}
