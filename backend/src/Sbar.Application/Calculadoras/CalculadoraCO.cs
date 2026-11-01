using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraCO : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.CO;

    // Eixo 1 — Complexidade Operacional (CO), TCC seção 4.4.2: CO = N + 2 × |bandas ocupadas|
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var valor = contexto.N + 2 * contexto.BandasOcupadas.Count;
        return new ResultadoDeMetricaBruta(true, valor, null);
    }
}
