using FluentAssertions;
using Moq;
using Sbar.Application.Calculadoras;
using Sbar.Application.CasosDeUso;
using Sbar.Application.Tests.Comparacao;
using Sbar.Application.Tests.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.CasosDeUso;

public class ServicoDeMetricasTests
{
    private static readonly ICalculadoraDeEixo[] TodasAsCalculadoras =
    [
        new CalculadoraCO(), new CalculadoraESC(), new CalculadoraINT(), new CalculadoraUX(),
        new CalculadoraDISP(), new CalculadoraFLEX(), new CalculadoraTR(), new CalculadoraAUT(),
        new CalculadoraVOL(), new CalculadoraDEP(),
    ];

    private static readonly VersaoDeParametrosInfo VersaoVigente =
        new(3, new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), "Versão de teste");

    // Quadro 7 (limites) + pesos iguais p_i = 0,1 (4.5.1).
    private static readonly IReadOnlyList<ParametroDeEixo> EixosDoQuadro7 =
    [
        new(Eixo.CO, 6, 40, 0.1), new(Eixo.ESC, 0, 1, 0.1), new(Eixo.INT, 0, 1, 0.1), new(Eixo.UX, 0, 3, 0.1),
        new(Eixo.DISP, 0, 1, 0.1), new(Eixo.FLEX, 0, 1, 0.1), new(Eixo.TR, 0, 1, 0.1), new(Eixo.AUT, 0, 1, 0.1),
        new(Eixo.VOL, 1, 3, 0.1), new(Eixo.DEP, 0.2, 1, 0.1),
    ];

    private readonly Mock<IEstudoDeCasoRepository> _estudoDeCasoRepositorio = new();
    private readonly Mock<IServicoDeParametros> _parametros = new();
    private readonly ServicoDeMetricas _servico;

    public ServicoDeMetricasTests()
    {
        DefinirParametros(EixosDoQuadro7, []);

        _servico = new ServicoDeMetricas(_estudoDeCasoRepositorio.Object, TodasAsCalculadoras, _parametros.Object);
    }

    private void DefinirParametros(IReadOnlyList<ParametroDeEixo> eixos, IReadOnlyList<AssinaturaDeReferencia> assinaturas, int? versao = null) =>
        _parametros.Setup(p => p.ObterAsync(versao)).ReturnsAsync(new ConjuntoDeParametros(VersaoVigente, eixos, assinaturas));

    // Quadro 25 do TCC (Estudo de Caso 1) — CT03.
    [Fact]
    [Trait("Requisito", "CT03")]
    public async Task CalcularAsync_DeveRetornarOsDezEixosNormalizadosDoQuadro25_QuandoEstudoDeCaso1()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.Eixos.Should().HaveCount(10);
        resultado.Eixos.Should().OnlyContain(r => r.Aplicavel);
        var esperado = new Dictionary<Eixo, double>
        {
            [Eixo.CO] = 2.79, [Eixo.ESC] = 3.25, [Eixo.INT] = 0.95, [Eixo.UX] = 1.67, [Eixo.DISP] = 3.75,
            [Eixo.FLEX] = 3.50, [Eixo.TR] = 0.32, [Eixo.AUT] = 1.67, [Eixo.VOL] = 3.10, [Eixo.DEP] = 2.50,
        };
        foreach (var (eixo, valor) in esperado)
            resultado.Eixos.Single(r => r.Eixo == eixo).ValorNormalizado.Should().BeApproximately(valor, 0.005, eixo.ToString());
        resultado.Eixos.Single(r => r.Eixo == Eixo.DEP).ValorBruto.Should().BeApproximately(0.6, 0.0001);
    }

    // Quadro 35 do TCC (Estudo de Caso 2) — CT03.
    [Fact]
    [Trait("Requisito", "CT03")]
    public async Task CalcularAsync_DeveRetornarOsDezEixosNormalizadosDoQuadro35_QuandoEstudoDeCaso2()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        var esperado = new Dictionary<Eixo, double>
        {
            [Eixo.CO] = 2.79, [Eixo.ESC] = 3.75, [Eixo.INT] = 1.19, [Eixo.UX] = 2.08, [Eixo.DISP] = 3.75,
            [Eixo.FLEX] = 2.76, [Eixo.TR] = 0.33, [Eixo.AUT] = 2.50, [Eixo.VOL] = 2.86, [Eixo.DEP] = 3.75,
        };
        foreach (var (eixo, valor) in esperado)
            resultado.Eixos.Single(r => r.Eixo == eixo).ValorNormalizado.Should().BeApproximately(valor, 0.005, eixo.ToString());
    }

    [Fact]
    public async Task CalcularAsync_DeveInformarComunidadesQBandasEFluxos_QuandoEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        // Quadro 24: três comunidades, Q = 0,41; saídas externas por comunidade (numeradas pela menor posição de cadastro)
        resultado.ModularidadeQ.Should().BeApproximately(0.4117, 0.00005);
        resultado.Comunidades.Select(c => c.Indice).Should().Equal(1, 2, 3);
        resultado.Comunidades.Select(c => c.SaidasExternas).Should().Equal(2, 2, 1);
        resultado.Comunidades[2].NosIds.Should().Equal(nos["P5"].Id, nos["S2"].Id, nos["E1"].Id);
        resultado.Avisos.Should().BeEmpty();

        // μ, σ e limiar de cada banda ocupada (Quadro 23): Backstage μ = 7,2; σ ≈ 1,47
        resultado.Bandas.Should().HaveCount(5);
        var backstage = resultado.Bandas.Single(b => b.Banda == Banda.Backstage);
        (backstage.QuantidadeDeNos, backstage.Media).Should().Be((5, 7.2));
        backstage.DesvioPadrao.Should().BeApproximately(1.47, 0.005);
        backstage.Limiar.Should().BeApproximately(backstage.Media + backstage.DesvioPadrao, 1e-12);

        resultado.Fluxos.Should().HaveCount(4).And.OnlyContain(f => f.Valido && f.BandasPercorridas == 3);
    }

    [Fact]
    [Trait("Requisito", "CT03")]
    public async Task CalcularAsync_DeveAvisarIdentificandoOsNos_QuandoHaNosSemNenhumaLigacao()
    {
        // Achado F-01 da bateria de testes: nós soltos (com m > 0) distorcem ESC/AUT/FLEX/DISP — no Estudo de Caso 1,
        // dois deles trocam o perfil mais próximo. O valor calculado não muda; o que muda é que o usuário é avisado.
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        estudoDeCaso.AdicionarNo("Relatório gerencial", TipoDeNo.Processo);
        estudoDeCaso.AdicionarNo("Auditoria interna", TipoDeNo.Processo);
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.Avisos.Should().HaveCount(2);
        resultado.Avisos.Should().ContainSingle(a => a.Contains("\"Relatório gerencial\"") && a.Contains("não entra em nenhuma ligação"));
        resultado.Avisos.Should().ContainSingle(a => a.Contains("\"Auditoria interna\""));
    }

    [Fact]
    public async Task CalcularAsync_NaoDeveAvisarDeNoSolto_QuandoOMapaNaoTemArestas()
    {
        // m = 0 já deixa INT/TR/VOL não aplicáveis; todos os nós seriam "soltos" e o aviso seria ruído.
        var estudoDeCaso = EstudoDeCaso.Criar("Sem ligações");
        estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        estudoDeCaso.AdicionarNo("Cadastro", TipoDeNo.Processo);
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.Avisos.Should().NotContain(a => a.Contains("nenhuma ligação"));
    }

    [Fact]
    public async Task CalcularAsync_DeveAvisarEDeixarDepNaoAplicavel_QuandoOUnicoFluxoFicaInvalido()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        foreach (var fluxo in estudoDeCaso.Fluxos.ToList())
            estudoDeCaso.RemoverFluxo(fluxo.Id);
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        // Um fluxo que perde a aresta passa a ser inválido: aparece no resultado, com aviso, mas fora do DEP.
        estudoDeCaso.AdicionarFluxo("Consulta", [nos["A"].Id, nos["TP1"].Id, nos["S1"].Id, nos["DS"].Id]);
        estudoDeCaso.RemoverAresta(estudoDeCaso.Arestas.Single(a => a.NoOrigemId == nos["S1"].Id && a.NoDestinoId == nos["DS"].Id).Id);

        var comInvalido = await _servico.CalcularAsync(estudoDeCaso.Id);

        comInvalido.Fluxos.Should().ContainSingle().Which.Valido.Should().BeFalse();
        comInvalido.Eixos.Single(r => r.Eixo == Eixo.DEP).Aplicavel.Should().BeFalse();
        comInvalido.Avisos.Should().Contain(a => a.Contains("inválido"));
    }

    [Fact]
    public async Task CalcularAsync_DeveListarOsGargalosDeTodasAsBandas_QuandoEstudoDeCaso2()
    {
        // Quadro 33 do TCC (Estudo de Caso 2): TP2 (Frontstage), P2 (Backstage) e DS (Suporte) — o de Frontstage não
        // entra em DISP, mas a tela do Mapa precisa dele.
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.NosGargalo.Should().BeEquivalentTo(new[] { nos["TP2"].Id, nos["P2"].Id, nos["DS"].Id });
    }

    [Fact]
    public async Task CalcularAsync_DeveTerRecomendacaoNula_QuandoNaoHaPerfilArquiteturalCadastrado()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        resultado.Recomendacao.Should().BeNull();
    }

    // Reaplicação (5.4): com a versão 1 o limite máximo de CO é 40 (Quadro 7); com a vigente (45) o normalizado muda.
    [Fact]
    public async Task CalcularAsync_DeveUsarOSnapshotDaVersaoPedida_QuandoReaplicaUmaVersaoAntiga()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);
        var versaoUm = new VersaoDeParametrosInfo(1, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), "Versão inicial");
        _parametros.Setup(p => p.ObterAsync(1)).ReturnsAsync(new ConjuntoDeParametros(versaoUm, EixosDoQuadro7, []));
        var comCoMaior = EixosDoQuadro7.Select(e => e.Eixo == Eixo.CO ? e with { Maximo = 45 } : e).ToList();
        DefinirParametros(comCoMaior, []);

        var antiga = await _servico.CalcularAsync(estudoDeCaso.Id, 1);
        var vigente = await _servico.CalcularAsync(estudoDeCaso.Id);

        antiga.VersaoDosParametros.Numero.Should().Be(1);
        antiga.Eixos.Single(e => e.Eixo == Eixo.CO).ValorNormalizado.Should().BeApproximately(2.79, 0.005);
        vigente.VersaoDosParametros.Numero.Should().Be(3);
        vigente.Eixos.Single(e => e.Eixo == Eixo.CO).ValorNormalizado.Should().BeApproximately(2.436, 0.001);
    }

    // CT04 pelo serviço inteiro (Mapa → comunidades → dez eixos → distâncias) com as assinaturas do Quadro 8: distância até cada
    // perfil (Quadros 27/37), soma das contribuições (Quadros 26/36), % do máximo (distância/5) e margem (6.10.1/7.10.1).
    [Theory]
    [Trait("Requisito", "CT04")]
    [Trait("Requisito", "RF10")]
    [InlineData(1, 1.051, 1.605, 1.1050, 2.5747, 0.553, 21, 32, 0.3063, 0.6490)]
    [InlineData(2, 1.107, 1.346, 1.2248, 1.8115, 0.239, 22, 27, 0.5063, 0.5334)]
    public async Task CalcularAsync_DeveReproduzirDistanciasContribuicoesEMargemDosQuadros_ComAsAssinaturasDoQuadro8(
        int estudo, double distanciaMonolito, double distanciaMicro, double somaMonolito, double somaMicro,
        double margem, double percentualMonolito, double percentualMicro, double contribuicaoDeEscNoMonolito, double contribuicaoDeIntNaMicro)
    {
        var (estudoDeCaso, _) = estudo == 1 ? EstudosDeCasoDeReferencia.CriarEstudoDeCaso1() : EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);
        DefinirParametros(EixosDoQuadro7,
            [AssinaturasDoQuadro8.MonolitoModular.ParaAssinatura(), AssinaturasDoQuadro8.Microsservicos.ParaAssinatura()]);

        var resultado = await _servico.CalcularAsync(estudoDeCaso.Id);

        var recomendacao = resultado.Recomendacao!;
        var monolito = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito"));
        var micro = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Microsserviços"));
        recomendacao.EstiloRecomendado.Should().Be(monolito.PerfilNome); // perfil mais próximo = Monólito Modular nos dois casos
        monolito.Distancia.Should().BeApproximately(distanciaMonolito, 0.0005);
        micro.Distancia.Should().BeApproximately(distanciaMicro, 0.0005);
        monolito.Contribuicoes.Sum(c => c.Contribuicao).Should().BeApproximately(somaMonolito, 0.001);
        micro.Contribuicoes.Sum(c => c.Contribuicao).Should().BeApproximately(somaMicro, 0.001);
        monolito.Contribuicoes.Single(c => c.Eixo == Eixo.ESC).Contribuicao.Should().BeApproximately(contribuicaoDeEscNoMonolito, 0.0001);
        micro.Contribuicoes.Single(c => c.Eixo == Eixo.INT).Contribuicao.Should().BeApproximately(contribuicaoDeIntNaMicro, 0.0001);
        monolito.PercentualDoMaximo.Should().BeApproximately(percentualMonolito, 0.5);
        micro.PercentualDoMaximo.Should().BeApproximately(percentualMicro, 0.5);
        recomendacao.Margem.Should().BeApproximately(margem, 0.001);
    }

    // CT10 — soma dos pesos diferente de 1 bloqueia o cálculo, com mensagem de erro (RN07).
    [Fact]
    [Trait("Requisito", "CT10")]
    [Trait("Requisito", "RN07")]
    public async Task CalcularAsync_DeveBloquearOCalculo_QuandoASomaDosPesosNaoEUm()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        _estudoDeCasoRepositorio.Setup(r => r.ObterPorIdAsync(estudoDeCaso.Id)).ReturnsAsync(estudoDeCaso);
        DefinirParametros(EixosDoQuadro7.Select(e => e.Eixo == Eixo.CO ? e with { Peso = 0.2 } : e).ToList(), []);

        var acao = () => _servico.CalcularAsync(estudoDeCaso.Id);

        (await acao.Should().ThrowAsync<ParametrosInvalidosException>()).WithMessage("Soma dos pesos igual a 1,1; deve ser 1.");
    }
}
