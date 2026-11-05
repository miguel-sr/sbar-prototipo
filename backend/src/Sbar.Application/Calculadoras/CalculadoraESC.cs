using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraESC : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.ESC;

    // Eixo 2 — Escalabilidade (ESC), TCC seção 4.4.2:
    // ESC = Σ_b [C(n_b,2) − arestas_intra(b)] / Σ_b C(n_b,2), somando as 5 bandas.
    // Caso especial: denominador 0 (nenhuma banda com ≥2 nós) → ESC = 1.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var estudoDeCaso = contexto.EstudoDeCaso;
        var bandaPorNo = estudoDeCaso.Nos.ToDictionary(n => n.Id, n => n.Banda);

        double denominador = 0;
        double numerador = 0;

        foreach (var banda in Enum.GetValues<Banda>())
        {
            var nDaBanda = estudoDeCaso.Nos.Count(n => n.Banda == banda);
            var combinacoes = nDaBanda * (nDaBanda - 1) / 2.0;
            denominador += combinacoes;

            // arestas_intra(b) — pares distintos de nós da banda com pelo menos uma aresta entre
            // eles (não a contagem bruta de arestas, para não contar duas vezes um par com
            // arestas paralelas/nos dois sentidos).
            var paresComAresta = estudoDeCaso.Arestas
                .Where(a => bandaPorNo[a.NoOrigemId] == banda && bandaPorNo[a.NoDestinoId] == banda)
                .Select(a => ParOrdenado(a.NoOrigemId, a.NoDestinoId))
                .Distinct()
                .Count();

            numerador += combinacoes - paresComAresta;
        }

        if (denominador == 0)
            return new ResultadoDeMetricaBruta(true, 1.0, null);

        return new ResultadoDeMetricaBruta(true, numerador / denominador, null);
    }

    private static (Guid, Guid) ParOrdenado(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
