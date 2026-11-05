using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraTR : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.TR;

    // Eixo 7 — Tempo Real (TR), TCC seção 4.4.2: TR = Σ_{tipo(e)=CRI} w(e) / Σ_e w(e).
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var arestas = contexto.EstudoDeCaso.Arestas;
        var pesoTotal = arestas.Sum(a => a.Peso.Valor);

        if (pesoTotal == 0)
            return new ResultadoDeMetricaBruta(false, null, "Soma de pesos das arestas é zero");

        var pesoCritico = arestas.Where(a => a.Tipo == TipoDeAresta.Critica).Sum(a => a.Peso.Valor);

        return new ResultadoDeMetricaBruta(true, (double)pesoCritico / pesoTotal, null);
    }
}
