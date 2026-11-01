using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraAUT : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.AUT;

    // Eixo 8 — Autonomia dos Módulos (AUT), TCC revisado seção 4.4.2 (comunidades: 4.4.1): para cada
    // comunidade c, saidas_externas(c) = arestas que PARTEM de nós de c para nós de outra comunidade
    // (a direção importa); AUT = |{c : saidas_externas(c) ≤ 1}| / |C|. Caso especial: C = ∅ → não
    // aplicável. Com uma única comunidade ninguém tem saída externa e AUT = 1 (degenerado — o
    // DetectorDeComunidades emite aviso).
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        var comunidades = contexto.Comunidades.IndicePorNo;
        var indices = Enumerable.Range(1, contexto.Comunidades.Quantidade).ToList();

        if (indices.Count == 0)
            return new ResultadoDeMetricaBruta(false, null, "Nenhuma comunidade (grafo sem nós)");

        var estudoDeCaso = contexto.EstudoDeCaso;
        var autonomas = indices.Count(comunidade =>
        {
            var saidasExternas = estudoDeCaso.Arestas.Count(a =>
                comunidades[a.NoOrigemId] == comunidade && comunidades[a.NoDestinoId] != comunidade);
            return saidasExternas <= 1;
        });

        return new ResultadoDeMetricaBruta(true, (double)autonomas / indices.Count, null);
    }
}
