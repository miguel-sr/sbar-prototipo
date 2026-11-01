using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraUX : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.UX;

    // Eixo 4 — Experiência do Usuário (UX), TCC seção 4.4.2:
    // UX = Σ_{v∈V_Frontstage} grau⁻(v) / n_Frontstage. Caso especial: n_Frontstage=0 → não aplicável.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var nosFrontstage = contexto.EstudoDeCaso.Nos.Where(n => n.Banda == Banda.Frontstage).ToList();

        if (nosFrontstage.Count == 0)
            return new ResultadoDeMetricaBruta(false, null, "Nenhum nó na banda Frontstage");

        var somaGrauEntrada = nosFrontstage.Sum(n => contexto.GrausEntrada[n.Id]);

        return new ResultadoDeMetricaBruta(true, (double)somaGrauEntrada / nosFrontstage.Count, null);
    }
}
