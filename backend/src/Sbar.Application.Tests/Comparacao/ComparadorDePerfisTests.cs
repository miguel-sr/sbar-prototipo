using FluentAssertions;
using Sbar.Application.Comparacao;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.Comparacao;

// Assinaturas provisórias de referência — TCC, Quadro 8 (seção 4.6). Mesmos valores do seed do produto,
// replicados aqui como golden data local para não acoplar este teste ao conteúdo do banco.
internal static class AssinaturasDoQuadro8
{
    public static readonly PerfilArquitetural MonolitoModular = PerfilArquitetural.Criar(new DadosDoPerfilArquitetural(
        "Monólito Modular (provisório)",
        ValorCO: 2.0, ValorESC: 1.5, ValorINT: 1.0, ValorUX: 2.5, ValorDISP: 3.0,
        ValorFLEX: 1.5, ValorTR: 1.5, ValorAUT: 1.0, ValorVOL: 3.0, ValorDEP: 3.0));

    public static readonly PerfilArquitetural Microsservicos = PerfilArquitetural.Criar(new DadosDoPerfilArquitetural(
        "Microsserviços (provisório)",
        ValorCO: 4.0, ValorESC: 4.0, ValorINT: 3.5, ValorUX: 2.5, ValorDISP: 4.0,
        ValorFLEX: 4.0, ValorTR: 2.5, ValorAUT: 4.5, ValorVOL: 2.0, ValorDEP: 4.0));
}

public class ComparadorDePerfisTests
{
    private static readonly IReadOnlyDictionary<Eixo, double> PesosIguais =
        Enum.GetValues<Eixo>().ToDictionary(e => e, _ => 0.1);

    // Normalizados dos Quadros 25 (Estudo de Caso 1) e 35 (Estudo de Caso 2), com os valores BRUTOS do TCC
    // normalizados sem arredondar (as contribuições dos Quadros 26/36 usam o normalizado não arredondado).
    private static IReadOnlyList<ResultadoDeEixo> EixosDoQuadro(IReadOnlyDictionary<Eixo, double> normalizados) =>
        normalizados.Select(p => new ResultadoDeEixo(p.Key, true, null, p.Value, null)).ToList();

    private static readonly IReadOnlyDictionary<Eixo, double> NormalizadosDoQuadro25 = new Dictionary<Eixo, double>
    {
        [Eixo.CO] = 5.0 * (25 - 6) / (40 - 6), [Eixo.ESC] = 5 * 0.65, [Eixo.INT] = 5 * 4.0 / 21, [Eixo.UX] = 5 * 1.0 / 3,
        [Eixo.DISP] = 5 * 0.75, [Eixo.FLEX] = 5 * 0.575, [Eixo.TR] = 5 * 3.0 / 47, [Eixo.AUT] = 5 * 0.5,
        [Eixo.VOL] = 5 * (47.0 / 21 - 1) / 2, [Eixo.DEP] = 5 * (0.6 - 0.2) / 0.8,
    };

    private static readonly IReadOnlyDictionary<Eixo, double> NormalizadosDoQuadro35 = new Dictionary<Eixo, double>
    {
        [Eixo.CO] = 5.0 * (25 - 6) / (40 - 6), [Eixo.ESC] = 5 * 0.75, [Eixo.INT] = 5 * 5.0 / 21, [Eixo.UX] = 5 * 1.25 / 3,
        [Eixo.DISP] = 5 * 0.75, [Eixo.FLEX] = 5 * (4.9166666666666667 / 8), [Eixo.TR] = 5 * 3.0 / 45, [Eixo.AUT] = 5 * 0.5,
        [Eixo.VOL] = 5 * (45.0 / 21 - 1) / 2, [Eixo.DEP] = 5 * (0.8 - 0.2) / 0.8,
    };

    // Isola o comparador: alimenta os normalizados publicados (Quadros 25 e 35) diretamente, sem depender dos eixos calculados
    // pelo produto — uma falha aqui é do comparador, não de FLEX/AUT/DEP (o pipeline inteiro é CT04 em ServicoDeMetricasTests).
    [Theory]
    [Trait("Requisito", "CT04")]
    [InlineData(1, 1.037, 1.508)]
    [InlineData(2, 1.146, 1.321)]
    public void CalcularDistancia_DeveReproduzirAsDistanciasDosQuadros27E37_ComAsAssinaturasDoQuadro8(
        int estudoDeCaso, double distanciaEsperadaMonolito, double distanciaEsperadaMicro)
    {
        var eixos = EixosDoQuadro(estudoDeCaso == 1 ? NormalizadosDoQuadro25 : NormalizadosDoQuadro35);

        var distanciaMonolito = ComparadorDePerfis.CalcularDistancia(eixos, AssinaturasDoQuadro8.MonolitoModular, PesosIguais);
        var distanciaMicro = ComparadorDePerfis.CalcularDistancia(eixos, AssinaturasDoQuadro8.Microsservicos, PesosIguais);

        distanciaMonolito.Should().BeApproximately(distanciaEsperadaMonolito, 0.0005);
        distanciaMicro.Should().BeApproximately(distanciaEsperadaMicro, 0.0005);
        distanciaMonolito.Should().BeLessThan(distanciaMicro); // perfil mais próximo = Monólito Modular nos dois casos
    }

    [Fact]
    public void CalcularDistancia_DeveIgnorarEixosNaoAplicaveis_QuandoServicoTemMetricaNaoAplicavel()
    {
        var eixos = new List<ResultadoDeEixo>
        {
            new(Eixo.CO, Aplicavel: true, ValorBruto: 25, ValorNormalizado: 2.79, Motivo: null),
            new(Eixo.ESC, Aplicavel: false, ValorBruto: null, ValorNormalizado: null, Motivo: "Banda vazia"),
        };
        var pesos = new Dictionary<Eixo, double> { [Eixo.CO] = 0.5, [Eixo.ESC] = 0.5 };
        var perfil = PerfilArquitetural.Criar(new DadosDoPerfilArquitetural("Perfil de teste", 2.79, 0, 0, 0, 0, 0, 0, 0, 0, 0));

        var distancia = ComparadorDePerfis.CalcularDistancia(eixos, perfil, pesos);

        distancia.Should().Be(0);
    }
}
