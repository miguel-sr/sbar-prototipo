using FluentAssertions;
using Sbar.Application.Comparacao;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.Comparacao;

// Assinaturas provisórias de referência — TCC, Quadro 7 (seção 4.6). Mesmos valores do seed do produto,
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
    // normalizados sem arredondar (as contribuições dos Quadros 26/34 usam o normalizado não arredondado).
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

    // Isola o comparador: alimenta os normalizados publicados (Quadros 25 e 33) diretamente, sem depender dos eixos calculados
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

    // ---- RN07/RN08: eixo "não aplicável" sai da distância e os pesos dos demais são renormalizados (DV-11) ----

    // Serviço com normalizado 3,0 nos eixos aplicáveis; assinatura = Monólito Modular do Quadro 7
    // (CO 2,0; ESC 1,5; INT 1,0; UX 2,5; DISP 3,0; FLEX 1,5; TR 1,5; AUT 1,0; VOL 3,0; DEP 3,0).
    private static List<ResultadoDeEixo> ServicoComNormalizado3(params Eixo[] naoAplicaveis) =>
        Enum.GetValues<Eixo>()
            .Select(e => naoAplicaveis.Contains(e)
                ? new ResultadoDeEixo(e, false, null, null, "Denominador zero")
                : new ResultadoDeEixo(e, true, null, 3.0, null))
            .ToList();

    [Fact]
    [Trait("Requisito", "CT12")]
    [Trait("Requisito", "RF10")]
    [Trait("Requisito", "RN07")]
    [Trait("Requisito", "RN08")]
    public void Comparar_DeveRenormalizarOsPesos_QuandoUmEixoNaoEAplicavel()
    {
        // DEP não aplicável: 9 eixos aplicáveis, pesos 0,1 cada → p' = 0,1 / 0,9 = 1/9, soma dos efetivos = 1.
        // Diferenças (3,0 − assinatura): CO 1,0; ESC 1,5; INT 2,0; UX 0,5; DISP 0; FLEX 1,5; TR 1,5; AUT 2,0; VOL 0.
        // Soma dos quadrados = 1 + 2,25 + 4 + 0,25 + 0 + 2,25 + 2,25 + 4 + 0 = 16,0 → d = √(16 / 9) = 4/3.
        // (Sem renormalizar, d = √(16 × 0,1) = 1,2649 — a distância encolhia ~5%.)
        var eixos = ServicoComNormalizado3(Eixo.DEP);

        var distancia = ComparadorDePerfis.Comparar(eixos, AssinaturasDoQuadro8.MonolitoModular.ParaAssinatura(), PesosIguais);
        var pesos = ComparadorDePerfis.CalcularPesosEfetivos(eixos, PesosIguais);

        distancia.Distancia.Should().BeApproximately(4.0 / 3.0, 1e-12);
        distancia.Contribuicoes.Should().HaveCount(9);
        distancia.Contribuicoes.Sum(c => c.Contribuicao).Should().BeApproximately(16.0 / 9.0, 1e-12); // soma = d²
        pesos.Efetivos.Sum(p => p.PesoEfetivo).Should().BeApproximately(1.0, 1e-12);
        pesos.Efetivos.Should().OnlyContain(p => Math.Abs(p.PesoConfigurado - 0.1) < 1e-12 && Math.Abs(p.PesoEfetivo - 1.0 / 9) < 1e-12);
        pesos.Omitidos.Should().Equal(Eixo.DEP);
    }

    [Fact]
    [Trait("Requisito", "RN07")]
    [Trait("Requisito", "RN08")]
    public void Comparar_DeveRenormalizarOsPesos_QuandoDoisEixosNaoSaoAplicaveis()
    {
        // INT e UX não aplicáveis: 8 eixos, p' = 0,1 / 0,8 = 0,125.
        // Diferenças: CO 1,0; ESC 1,5; DISP 0; FLEX 1,5; TR 1,5; AUT 2,0; VOL 0; DEP 0 → soma dos quadrados
        // = 1 + 2,25 + 0 + 2,25 + 2,25 + 4 + 0 + 0 = 11,75 → d = √(11,75 × 0,125) = √1,46875 ≈ 1,21191.
        // (Sem renormalizar, d = √(11,75 × 0,1) ≈ 1,0840.)
        var eixos = ServicoComNormalizado3(Eixo.INT, Eixo.UX);

        var distancia = ComparadorDePerfis.Comparar(eixos, AssinaturasDoQuadro8.MonolitoModular.ParaAssinatura(), PesosIguais);
        var pesos = ComparadorDePerfis.CalcularPesosEfetivos(eixos, PesosIguais);

        distancia.Distancia.Should().BeApproximately(Math.Sqrt(1.46875), 1e-12);
        distancia.Distancia.Should().BeApproximately(1.21191, 0.00001);
        pesos.Efetivos.Should().HaveCount(8).And.OnlyContain(p => Math.Abs(p.PesoEfetivo - 0.125) < 1e-12);
        pesos.Efetivos.Sum(p => p.PesoEfetivo).Should().BeApproximately(1.0, 1e-12);
        pesos.Omitidos.Should().BeEquivalentTo(new[] { Eixo.INT, Eixo.UX });
    }

    [Fact]
    [Trait("Requisito", "RN07")]
    public void Comparar_DeveManterDistanciaEntreZeroECinco_ComEixosOmitidosEPesosDesiguais()
    {
        // Pesos desiguais e dois eixos omitidos: a distância máxima continua 5 porque Σ p' = 1.
        var pesosDesiguais = new Dictionary<Eixo, double>
        {
            [Eixo.CO] = 0.3, [Eixo.ESC] = 0.2, [Eixo.INT] = 0.1, [Eixo.UX] = 0.05, [Eixo.DISP] = 0.05,
            [Eixo.FLEX] = 0.1, [Eixo.TR] = 0.05, [Eixo.AUT] = 0.05, [Eixo.VOL] = 0.05, [Eixo.DEP] = 0.05,
        };
        var eixos = Enum.GetValues<Eixo>()
            .Select(e => e is Eixo.INT or Eixo.TR
                ? new ResultadoDeEixo(e, false, null, null, "Denominador zero")
                : new ResultadoDeEixo(e, true, null, 0.0, null))
            .ToList();

        var distancia = ComparadorDePerfis.Comparar(eixos, AssinaturasDoQuadro8.Microsservicos.ParaAssinatura(), pesosDesiguais);

        distancia.Distancia.Should().BeInRange(0, ComparadorDePerfis.DistanciaMaxima);
        double.IsNaN(distancia.Distancia).Should().BeFalse();
        ComparadorDePerfis.CalcularPesosEfetivos(eixos, pesosDesiguais).Efetivos.Sum(p => p.PesoEfetivo)
            .Should().BeApproximately(1.0, 1e-12);
    }

    [Fact]
    [Trait("Requisito", "RN07")]
    public void CalcularPesosEfetivos_DeveManterOsPesosConfigurados_QuandoOsDezEixosSaoAplicaveis()
    {
        var pesos = ComparadorDePerfis.CalcularPesosEfetivos(EixosDoQuadro(NormalizadosDoQuadro25), PesosIguais);

        pesos.Omitidos.Should().BeEmpty();
        pesos.Efetivos.Should().HaveCount(10).And.OnlyContain(p => p.PesoEfetivo == p.PesoConfigurado && p.PesoEfetivo == 0.1);
    }

    [Fact]
    [Trait("Requisito", "RN08")]
    public void Comparar_DeveFalharComMensagemClara_QuandoNenhumEixoEAplicavel()
    {
        var eixos = ServicoComNormalizado3(Enum.GetValues<Eixo>());

        var pesos = ComparadorDePerfis.CalcularPesosEfetivos(eixos, PesosIguais);
        var comparar = () => ComparadorDePerfis.Comparar(eixos, AssinaturasDoQuadro8.MonolitoModular.ParaAssinatura(), PesosIguais);

        pesos.PodeComparar.Should().BeFalse();
        pesos.Efetivos.Should().BeEmpty();
        comparar.Should().Throw<InvalidOperationException>().WithMessage("*Nenhum eixo aplicável*"); // nunca devolve NaN nem 0 enganoso
    }

    [Fact]
    [Trait("Requisito", "RN08")]
    public void CalcularPesosEfetivos_NaoDevePoderComparar_QuandoOsEixosAplicaveisTemPesoZero()
    {
        // Só o CO é aplicável e tem peso 0: Σ p_j = 0, a renormalização dividiria por zero.
        var eixos = ServicoComNormalizado3(Enum.GetValues<Eixo>().Where(e => e != Eixo.CO).ToArray());
        var pesos = Enum.GetValues<Eixo>().ToDictionary(e => e, e => e == Eixo.CO ? 0.0 : 0.1111111111111111);

        ComparadorDePerfis.CalcularPesosEfetivos(eixos, pesos).PodeComparar.Should().BeFalse();
    }
}
