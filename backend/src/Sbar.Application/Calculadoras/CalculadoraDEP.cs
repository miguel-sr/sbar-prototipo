using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public class CalculadoraDEP : ICalculadoraDeEixo
{
    public Eixo Eixo => Eixo.DEP;

    // Eixo 10 — Dependência Organizacional (DEP), TCC revisado seção 4.4.2:
    // DEP = [Σ_{f∈F} bandas_percorridas(f) / |F|] / 5, com bandas_percorridas(f) = nº de bandas distintas do
    // caminho do fluxo, incluindo origem e destino. Caso especial: F = ∅ → não aplicável. F é ENTRADA: os
    // fluxos principais são cadastrados pelo usuário (RF05) e só os de caminho dirigido válido entram (4.2.7);
    // não há mais derivação automática a partir do grafo.
    public ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto)
    {
        if (contexto.FluxosPrincipais.Count == 0)
            return new ResultadoDeMetricaBruta(false, null, "Nenhum fluxo principal válido cadastrado (F = ∅)");

        var mediaDeBandas = contexto.FluxosPrincipais.Average(f => f.BandasPercorridas);

        return new ResultadoDeMetricaBruta(true, mediaDeBandas / 5.0, null);
    }
}
