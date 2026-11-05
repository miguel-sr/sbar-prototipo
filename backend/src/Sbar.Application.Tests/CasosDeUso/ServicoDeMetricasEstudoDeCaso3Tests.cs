using FluentAssertions;
using Moq;
using Sbar.Application.Calculadoras;
using Sbar.Application.CasosDeUso;
using Sbar.Application.Grafo;
using Sbar.Application.Tests.Comparacao;
using Sbar.Application.Tests.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.CasosDeUso;

// Reprodução do Estudo de Caso 3 (Marketplace de varejo online, Capítulo 8 do TCC) pelo serviço inteiro:
// Mapa (33 nós, 49 arestas, 7 fluxos) → cargas e gargalos → comunidades e Q → dez eixos → distâncias.
// Os valores esperados são os do TCC (Quadros 39, 40, 42, 43 e 45 e a seção 9.7, Quadros 53 e 54) e foram conferidos
// contra o harness do backend e contra uma implementação independente em Python antes de entrarem aqui. O cenário foi
// construído pelos autores (seção 8.1): este teste prova que o protótipo reproduz os números publicados, não que o
// Mapa represente um sistema real.
public class ServicoDeMetricasEstudoDeCaso3Tests
{
    private static readonly ICalculadoraDeEixo[] TodasAsCalculadoras =
    [
        new CalculadoraCO(), new CalculadoraESC(), new CalculadoraINT(), new CalculadoraUX(),
        new CalculadoraDISP(), new CalculadoraFLEX(), new CalculadoraTR(), new CalculadoraAUT(),
        new CalculadoraVOL(), new CalculadoraDEP(),
    ];

    private static readonly VersaoDeParametrosInfo VersaoVigente =
        new(1, new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc), "Versão de teste");

    // Quadro 6 (limites) + pesos iguais p_i = 0,1 (4.5.1).
    private static readonly IReadOnlyList<ParametroDeEixo> EixosDoQuadro7 =
    [
        new(Eixo.CO, 6, 40, 0.1), new(Eixo.ESC, 0, 1, 0.1), new(Eixo.INT, 0, 1, 0.1), new(Eixo.UX, 0, 3, 0.1),
        new(Eixo.DISP, 0, 1, 0.1), new(Eixo.FLEX, 0, 1, 0.1), new(Eixo.TR, 0, 1, 0.1), new(Eixo.AUT, 0, 1, 0.1),
        new(Eixo.VOL, 1, 3, 0.1), new(Eixo.DEP, 0.2, 1, 0.1),
    ];

    // Assinaturas revisadas pelos especialistas (TCC 9.7, Quadro 53): diferem das provisórias em ESC, FLEX e DEP do
    // Monólito Modular e em INT de Microsserviços.
    private static readonly PerfilArquitetural MonolitoRevisado = PerfilArquitetural.Criar(new DadosDoPerfilArquitetural(
        "Monólito Modular (revisado)",
        ValorCO: 2.0, ValorESC: 2.0, ValorINT: 1.0, ValorUX: 2.5, ValorDISP: 3.0,
        ValorFLEX: 2.0, ValorTR: 1.5, ValorAUT: 1.0, ValorVOL: 3.0, ValorDEP: 2.5));

    private static readonly PerfilArquitetural MicrosservicosRevisado = PerfilArquitetural.Criar(new DadosDoPerfilArquitetural(
        "Microsserviços (revisado)",
        ValorCO: 4.0, ValorESC: 4.0, ValorINT: 4.0, ValorUX: 2.5, ValorDISP: 4.0,
        ValorFLEX: 4.0, ValorTR: 2.5, ValorAUT: 4.5, ValorVOL: 2.0, ValorDEP: 4.0));

    private readonly Mock<IEstudoDeCasoRepository> _estudoDeCasoRepositorio = new();
    private readonly Mock<IServicoDeParametros> _parametros = new();
    private readonly ServicoDeMetricas _servico;

    public ServicoDeMetricasEstudoDeCaso3Tests()
    {
        DefinirAssinaturas();
        _servico = new ServicoDeMetricas(_estudoDeCasoRepositorio.Object, TodasAsCalculadoras, _parametros.Object);
    }

    private void DefinirAssinaturas(params AssinaturaDeReferencia[] assinaturas) =>
        _parametros.Setup(p => p.ObterAsync(null)).ReturnsAsync(new ConjuntoDeParametros(VersaoVigente, EixosDoQuadro7, assinaturas));

    // Quadro 39 do TCC — carga de cada nó (soma das intensidades das arestas incidentes).
    [Fact]
    [Trait("Requisito", "CT03")]
    public void Calcular_DeveBaterComQuadro48_QuandoForEstudoDeCaso3()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso3();

        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var esperado = new Dictionary<string, int>
        {
            ["A1"] = 7, ["A2"] = 4,
            ["TP1"] = 11, ["TP2"] = 11, ["TP3"] = 4, ["TP4"] = 7, ["TP5"] = 4, ["TP6"] = 5,
            ["P1"] = 13, ["P2"] = 8, ["P3"] = 7, ["P4"] = 18, ["P5"] = 15, ["P6"] = 6,
            ["P7"] = 8, ["P8"] = 9, ["P9"] = 7, ["P10"] = 4, ["P11"] = 8,
            ["S1"] = 4, ["S2"] = 6, ["DS1"] = 6, ["DS2"] = 3, ["DS3"] = 5, ["DS4"] = 3, ["DS5"] = 6,
            ["E1"] = 6, ["E2"] = 3, ["E3"] = 4, ["E4"] = 4, ["E5"] = 2, ["E6"] = 4, ["E7"] = 4,
        };

        esperado.Should().HaveCount(33);
        foreach (var (codigo, valorEsperado) in esperado)
            cargas[nos[codigo].Id].Should().Be(valorEsperado, $"carga de {codigo}");
    }

    // Quadro 39 (μ, σ populacional e nós potencialmente críticos) e Quadro 40 (comunidades, Q e saídas externas).
    [Fact]
    [Trait("Requisito", "CT03")]
    public async Task CalcularAsync_DeveInformarGargalosBandasComunidadesQEFluxos_QuandoEstudoDeCaso3()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso3();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        // Quadro 39: oito nós acima de μ + σ da própria banda (TP1 e TP2 no Frontstage; P4 e P5 no Backstage; S2, DS1 e DS5
        // no Suporte; E1 no Externo).
        var gargalos = new[] { "TP1", "TP2", "P4", "P5", "S2", "DS1", "DS5", "E1" };
        resultado.NosGargalo.Should().BeEquivalentTo(gargalos.Select(c => nos[c].Id));

        resultado.Bandas.Should().HaveCount(5);
        var esperadoPorBanda = new (Banda Banda, int Nos, double Media, double Desvio)[]
        {
            (Banda.Ator, 2, 5.50, 1.50), (Banda.Frontstage, 6, 7.00, 3.00), (Banda.Backstage, 11, 9.36, 4.01),
            (Banda.Suporte, 7, 4.71, 1.28), (Banda.Externo, 7, 3.86, 1.12),
        };
        foreach (var (banda, quantidade, media, desvio) in esperadoPorBanda)
        {
            var estatistica = resultado.Bandas.Single(b => b.Banda == banda);
            estatistica.QuantidadeDeNos.Should().Be(quantidade, banda.ToString());
            estatistica.Media.Should().BeApproximately(media, 0.005, banda.ToString());
            estatistica.DesvioPadrao.Should().BeApproximately(desvio, 0.005, banda.ToString());
        }

        // Quadro 40: cinco comunidades (C1 a C5, numeradas pela menor posição de cadastro), Q = 0,5266 e saídas externas
        // 2, 3, 3, 1 e 3; a única com no máximo uma saída é a C4 (notificações), logo AUT = 1/5.
        resultado.ModularidadeQ.Should().BeApproximately(0.5266, 0.00005);
        resultado.Comunidades.Select(c => c.Indice).Should().Equal(1, 2, 3, 4, 5);
        resultado.Comunidades.Select(c => c.SaidasExternas).Should().Equal(2, 3, 3, 1, 3);
        resultado.Comunidades[3].NosIds.Should().BeEquivalentTo(new[] { nos["TP5"].Id, nos["S2"].Id, nos["E4"].Id });
        resultado.Comunidades.Select(c => c.NosIds.Count).Should().Equal(8, 8, 4, 3, 10);

        // Seção 8.7: sete fluxos principais, todos com caminho dirigido; bandas percorridas 4, 4, 3, 5, 5, 3 e 2.
        resultado.Fluxos.Should().HaveCount(7).And.OnlyContain(f => f.Valido);
        resultado.Fluxos.Select(f => f.BandasPercorridas).Should().Equal(4, 4, 3, 5, 5, 3, 2);
        resultado.Avisos.Should().BeEmpty();
    }

    // Quadro 42 do TCC (Estudo de Caso 3) — CT03: métricas brutas e normalizadas (duas casas). CO satura em 5,00 (RN06).
    [Fact]
    [Trait("Requisito", "CT03")]
    [Trait("Requisito", "RN06")]
    public async Task CalcularAsync_DeveRetornarOsDezEixosDoQuadro51_QuandoEstudoDeCaso3()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso3();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.Eixos.Should().HaveCount(10).And.OnlyContain(e => e.Aplicavel);
        var esperado = new (Eixo Eixo, double Bruto, double Normalizado)[]
        {
            (Eixo.CO, 43, 5.00), (Eixo.ESC, 0.9292, 4.65), (Eixo.INT, 0.2857, 1.43), (Eixo.UX, 1.1667, 1.94),
            (Eixo.DISP, 0.7222, 3.61), (Eixo.FLEX, 0.8419, 4.21), (Eixo.TR, 0.0833, 0.42), (Eixo.AUT, 0.2000, 1.00),
            (Eixo.VOL, 2.2041, 3.01), (Eixo.DEP, 0.7429, 3.39),
        };
        foreach (var (eixo, bruto, normalizado) in esperado)
        {
            var obtido = resultado.Eixos.Single(e => e.Eixo == eixo);
            obtido.ValorBruto.Should().BeApproximately(bruto, 0.00005, $"{eixo} bruto");
            obtido.ValorNormalizado.Should().BeApproximately(normalizado, 0.005, $"{eixo} normalizado");
        }
    }

    // CT04 pelo serviço inteiro com as assinaturas do Quadro 7: distâncias (Quadro 45), contribuições (Quadro 43), % do máximo
    // e margem (8.10.1). Aqui o perfil mais próximo é Microsserviços (cenário construído para isso, seção 8.1).
    [Fact]
    [Trait("Requisito", "CT04")]
    [Trait("Requisito", "RF10")]
    public async Task CalcularAsync_DeveReproduzirDistanciasContribuicoesEMargemDosQuadros52E54_ComAsAssinaturasDoQuadro8()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso3();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);
        DefinirAssinaturas(AssinaturasDoQuadro8.MonolitoModular.ParaAssinatura(), AssinaturasDoQuadro8.Microsservicos.ParaAssinatura());

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        var recomendacao = resultado.Recomendacao!;
        var monolito = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito"));
        var micro = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Microsserviços"));
        recomendacao.EstiloRecomendado.Should().Be(micro.PerfilNome); // diferente dos Estudos de Caso 1 e 2
        monolito.Distancia.Should().BeApproximately(1.686, 0.0005);
        micro.Distancia.Should().BeApproximately(1.555, 0.0005);
        recomendacao.Margem.Should().BeApproximately(0.131, 0.0005);
        monolito.PercentualDoMaximo.Should().BeApproximately(33.7, 0.05);
        micro.PercentualDoMaximo.Should().BeApproximately(31.1, 0.05);

        // Quadro 43: contribuições p_i · (Norm_i − assinatura_i)² dos eixos que mais pesam em cada perfil.
        monolito.Contribuicoes.Single(c => c.Eixo == Eixo.CO).Contribuicao.Should().BeApproximately(0.9000, 0.0001);
        monolito.Contribuicoes.Single(c => c.Eixo == Eixo.ESC).Contribuicao.Should().BeApproximately(0.9897, 0.0001);
        monolito.Contribuicoes.Single(c => c.Eixo == Eixo.FLEX).Contribuicao.Should().BeApproximately(0.7342, 0.0001);
        micro.Contribuicoes.Single(c => c.Eixo == Eixo.INT).Contribuicao.Should().BeApproximately(0.4291, 0.0001);
        micro.Contribuicoes.Single(c => c.Eixo == Eixo.TR).Contribuicao.Should().BeApproximately(0.4340, 0.0001);
        micro.Contribuicoes.Single(c => c.Eixo == Eixo.AUT).Contribuicao.Should().BeApproximately(1.2250, 0.0001);
        Math.Pow(monolito.Distancia, 2).Should().BeApproximately(monolito.Contribuicoes.Sum(c => c.Contribuicao), 1e-9);
        Math.Pow(micro.Distancia, 2).Should().BeApproximately(micro.Contribuicoes.Sum(c => c.Contribuicao), 1e-9);

        // Os dez eixos são aplicáveis: nenhuma renormalização (RN07/RN08), pesos efetivos iguais aos configurados.
        recomendacao.EixosOmitidos.Should().BeEmpty();
        recomendacao.PesosEfetivos.Should().HaveCount(10).And.OnlyContain(p => p.PesoEfetivo == 0.1);
    }

    // Seção 9.7 (Quadro 54): com as assinaturas revisadas pelos especialistas, o mesmo Mapa passa a indicar o Monólito Modular.
    [Fact]
    [Trait("Requisito", "CT04")]
    public async Task CalcularAsync_DeveIndicarOMonolito_QuandoEstudoDeCaso3ComAsAssinaturasRevisadas()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso3();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);
        DefinirAssinaturas(MonolitoRevisado.ParaAssinatura(), MicrosservicosRevisado.ParaAssinatura());

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        var recomendacao = resultado.Recomendacao!;
        var monolito = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito"));
        var micro = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Microsserviços"));
        recomendacao.EstiloRecomendado.Should().Be(monolito.PerfilNome);
        monolito.Distancia.Should().BeApproximately(1.540, 0.0005);
        micro.Distancia.Should().BeApproximately(1.628, 0.0005);
        recomendacao.Margem.Should().BeApproximately(0.088, 0.0005);
    }
}
