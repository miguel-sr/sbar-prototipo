using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.Comparacao;

public static class ComparadorDePerfis
{
    // Os eixos vão de 0 a 5 e Σ p'_i = 1 (os pesos dos eixos aplicáveis são renormalizados, RN07), então a maior distância
    // possível é 5 (TCC 4.6): cada distância pode ser lida também como percentual desse máximo.
    public const double DistanciaMaxima = 5.0;

    // Pesos efetivos — RN07/RN08. Um eixo "não aplicável" (denominador zero, p. ex. INT com M = 0) não pode ser medido e
    // fica fora da soma; para que a soma dos pesos continue igual a 1 (RN07) e a distância máxima continue 5, os pesos
    // dos eixos aplicáveis são renormalizados: p'_i = p_i / Σ_{j aplicável} p_j. Com os dez eixos aplicáveis, p'_i = p_i
    // (nada muda). O TCC não detalha esse caso; esta é a convenção do projeto (DV-11).
    public static PesosDaComparacao CalcularPesosEfetivos(
        IReadOnlyList<ResultadoDeEixo> resultadosDoServico,
        IReadOnlyDictionary<Eixo, double> pesos)
    {
        var aplicaveis = resultadosDoServico.Where(r => r.Aplicavel && r.ValorNormalizado is not null).ToList();
        var omitidos = resultadosDoServico.Where(r => !(r.Aplicavel && r.ValorNormalizado is not null)).Select(r => r.Eixo).ToList();

        var somaDosPesos = aplicaveis.Sum(r => pesos[r.Eixo]);
        if (aplicaveis.Count == 0 || somaDosPesos <= 0)
            return new PesosDaComparacao([], [.. resultadosDoServico.Select(r => r.Eixo)]);

        // Nenhum eixo omitido: os pesos configurados já somam 1 (RN07, validado antes do cálculo) e valem como estão —
        // sem dividir, para que o resultado seja idêntico ao dos Quadros 26/34 (sem ruído de ponto flutuante).
        if (omitidos.Count == 0)
            return new PesosDaComparacao([.. aplicaveis.Select(r => new PesoEfetivoDeEixo(r.Eixo, pesos[r.Eixo], pesos[r.Eixo]))], []);

        var efetivos = aplicaveis
            .Select(r => new PesoEfetivoDeEixo(r.Eixo, pesos[r.Eixo], pesos[r.Eixo] / somaDosPesos))
            .ToList();

        return new PesosDaComparacao(efetivos, omitidos);
    }

    // Distância euclidiana ponderada — docs/dominio/formulas-metricas.md:
    // d(Servico, Estilo) = √(Σ_i p'_i × (Norm_i^Servico − Norm_i^Estilo)²), com p'_i os pesos efetivos (renormalizados
    // entre os eixos aplicáveis; iguais a p_i quando os dez eixos são aplicáveis).
    public static double CalcularDistancia(
        IReadOnlyList<ResultadoDeEixo> resultadosDoServico,
        PerfilArquitetural perfil,
        IReadOnlyDictionary<Eixo, double> pesos) =>
        Comparar(resultadosDoServico, perfil.ParaAssinatura(), pesos).Distancia;

    // Distância + contribuição de cada eixo (Quadros 26/34): contribuição_i = p'_i × (Norm_i^Servico − Norm_i^Estilo)²,
    // calculada com os normalizados NÃO arredondados; a soma das contribuições é a distância ao quadrado.
    // Sem nenhum eixo aplicável com peso não há o que comparar: quem chama deve checar PodeComparar antes.
    public static DistanciaPerfil Comparar(
        IReadOnlyList<ResultadoDeEixo> resultadosDoServico,
        AssinaturaDeReferencia assinatura,
        IReadOnlyDictionary<Eixo, double> pesos)
    {
        var pesosDaComparacao = CalcularPesosEfetivos(resultadosDoServico, pesos);
        if (!pesosDaComparacao.PodeComparar)
            throw new InvalidOperationException("Nenhum eixo aplicável com peso: não há distância a calcular.");

        var efetivos = pesosDaComparacao.Efetivos.ToDictionary(p => p.Eixo, p => p.PesoEfetivo);
        var contribuicoes = new List<ContribuicaoDeEixo>();

        foreach (var resultado in resultadosDoServico)
        {
            if (!efetivos.TryGetValue(resultado.Eixo, out var pesoEfetivo))
                continue;

            var diferenca = resultado.ValorNormalizado!.Value - assinatura.Valores[resultado.Eixo];
            contribuicoes.Add(new ContribuicaoDeEixo(resultado.Eixo, pesoEfetivo * Math.Pow(diferenca, 2)));
        }

        var distancia = Math.Sqrt(contribuicoes.Sum(c => c.Contribuicao));

        return new DistanciaPerfil(assinatura.Id, assinatura.Nome, distancia, distancia / DistanciaMaxima * 100.0, contribuicoes);
    }
}
