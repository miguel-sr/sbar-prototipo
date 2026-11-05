using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraVOL : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.VOL;

    // Eixo 9 — Volume de Comunicação (VOL), TCC seção 4.4.2: VOL = Σ_e w(e) / M.
    // Caso especial: M = 0 → não aplicável.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        if (contexto.M == 0)
            return new ResultadoDeMetricaBruta(false, null, "M = 0 — sem arestas no grafo");

        var pesoTotal = contexto.EstudoDeCaso.Arestas.Sum(a => a.Peso.Valor);

        return new ResultadoDeMetricaBruta(true, (double)pesoTotal / contexto.M, null);
    }
}
