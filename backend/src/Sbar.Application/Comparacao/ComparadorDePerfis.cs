using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.Comparacao;

public static class ComparadorDePerfis
{
    // Os eixos vão de 0 a 5 e Σ p_i = 1, então a maior distância possível é 5 (TCC 4.6): cada distância pode ser
    // lida também como percentual desse máximo.
    public const double DistanciaMaxima = 5.0;

    // Distância euclidiana ponderada — docs/dominio/formulas-metricas.md:
    // d(Servico, Estilo) = √(Σ_i p_i × (Norm_i^Servico − Norm_i^Estilo)²).
    // Eixos "não aplicável" não entram na soma (o TCC não cobre esse caso; convenção nossa:
    // um eixo que não pôde ser medido simplesmente não contribui para a distância).
    public static double CalcularDistancia(
        IReadOnlyList<ResultadoDeEixo> resultadosDoServico,
        PerfilArquitetural perfil,
        IReadOnlyDictionary<Eixo, double> pesos) =>
        Comparar(resultadosDoServico, perfil.ParaAssinatura(), pesos).Distancia;

    // Distância + contribuição de cada eixo (Quadros 26/36): contribuição_i = p_i × (Norm_i^Servico − Norm_i^Estilo)²,
    // calculada com os normalizados NÃO arredondados; a soma das contribuições é a distância ao quadrado.
    public static DistanciaPerfil Comparar(
        IReadOnlyList<ResultadoDeEixo> resultadosDoServico,
        AssinaturaDeReferencia assinatura,
        IReadOnlyDictionary<Eixo, double> pesos)
    {
        var contribuicoes = new List<ContribuicaoDeEixo>();

        foreach (var resultado in resultadosDoServico)
        {
            if (!resultado.Aplicavel || resultado.ValorNormalizado is null)
                continue;

            var diferenca = resultado.ValorNormalizado.Value - assinatura.Valores[resultado.Eixo];
            contribuicoes.Add(new ContribuicaoDeEixo(resultado.Eixo, pesos[resultado.Eixo] * Math.Pow(diferenca, 2)));
        }

        var distancia = Math.Sqrt(contribuicoes.Sum(c => c.Contribuicao));

        return new DistanciaPerfil(assinatura.Id, assinatura.Nome, distancia, distancia / DistanciaMaxima * 100.0, contribuicoes);
    }
}
