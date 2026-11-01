using FluentAssertions;
using Sbar.Application.Calculadoras;
using Sbar.Application.Grafo;
using Sbar.Application.Tests.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Xunit;

namespace Sbar.Application.Tests.Calculadoras;

public class CalculadorasDeEixoTests
{
    private static readonly ICalculadoraDeEixo[] Calculadoras =
    [
        new CalculadoraCO(), new CalculadoraESC(), new CalculadoraINT(), new CalculadoraUX(),
        new CalculadoraDISP(), new CalculadoraFLEX(), new CalculadoraTR(), new CalculadoraAUT(),
        new CalculadoraVOL(), new CalculadoraDEP(),
    ];

    private static double Bruto(Eixo eixo, ContextoDoMapa contexto)
    {
        var calculadora = Calculadoras.Single(c => c.Eixo == eixo);
        var resultado = calculadora.Calcular(contexto);
        resultado.Aplicavel.Should().BeTrue($"{eixo} deveria ser aplicável neste caso");
        return resultado.Valor!.Value;
    }

    [Theory]
    [Trait("Requisito", "CT03")]
    [InlineData(Eixo.CO, 25.00)]
    [InlineData(Eixo.ESC, 0.65)]
    [InlineData(Eixo.INT, 0.1905)]
    [InlineData(Eixo.UX, 1.00)]
    [InlineData(Eixo.DISP, 0.75)]
    [InlineData(Eixo.FLEX, 0.70)]
    [InlineData(Eixo.TR, 0.0638)]
    [InlineData(Eixo.AUT, 0.3333)]
    [InlineData(Eixo.VOL, 2.2381)]
    [InlineData(Eixo.DEP, 0.60)]
    public void Calcular_DeveBaterComQuadro25_QuandoForEstudoDeCaso1(Eixo eixo, double esperado)
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        Bruto(eixo, contexto).Should().BeApproximately(esperado, 0.001);
    }

    [Theory]
    [Trait("Requisito", "CT03")]
    [InlineData(Eixo.CO, 25.00)]
    [InlineData(Eixo.ESC, 0.75)]
    [InlineData(Eixo.INT, 0.2381)]
    [InlineData(Eixo.UX, 1.25)]
    [InlineData(Eixo.DISP, 0.75)]
    [InlineData(Eixo.FLEX, 0.5521)]
    [InlineData(Eixo.TR, 0.0667)]
    [InlineData(Eixo.AUT, 0.50)]
    [InlineData(Eixo.VOL, 2.1429)]
    [InlineData(Eixo.DEP, 0.80)]
    public void Calcular_DeveBaterComQuadro35_QuandoForEstudoDeCaso2(Eixo eixo, double esperado)
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        Bruto(eixo, contexto).Should().BeApproximately(esperado, 0.001);
    }

    [Fact]
    [Trait("Requisito", "RN08")]
    public void CalculadoraINT_DeveSerNaoAplicavel_QuandoMForZero()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sem arestas");
        estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var resultado = new CalculadoraINT().Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().NotBeNullOrWhiteSpace();
    }

    // CT08 — mapa sem nós de Frontstage: UX exibida como não aplicável
    [Fact]
    [Trait("Requisito", "CT08")]
    [Trait("Requisito", "RN08")]
    public void CalculadoraUX_DeveSerNaoAplicavel_QuandoNaoHaNoFrontstage()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sem Frontstage");
        var a = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var p = estudoDeCaso.AdicionarNo("Processo", TipoDeNo.Processo);
        estudoDeCaso.AdicionarAresta(a.Id, p.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var resultado = new CalculadoraUX().Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CalculadoraDEP_DeveSerNaoAplicavel_QuandoNaoHaFluxoPrincipalCadastrado()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sem fluxos");
        var a = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("Processo B", TipoDeNo.Processo);
        estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var resultado = new CalculadoraDEP().Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().Contain("F = ∅");
    }

    [Fact]
    public void CalculadoraDEP_DeveIgnorarFluxoInvalido_E_UsarSoOsValidos()
    {
        // A→B→C atravessa 3 bandas (Ator, Backstage, Suporte). O fluxo "Quebrado" perde a aresta B→C depois de
        // cadastrado: fica inválido (sinalizado, não apagado) e sai do DEP, que passa a ver só o fluxo válido.
        var estudoDeCaso = EstudoDeCaso.Criar("Fluxo que quebra");
        var a = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("Processo", TipoDeNo.Processo);
        var c = estudoDeCaso.AdicionarNo("Repositório", TipoDeNo.Dados);
        estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        var bc = estudoDeCaso.AdicionarAresta(b.Id, c.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        estudoDeCaso.AdicionarFluxo("Curto", [a.Id, b.Id]);                 // Ator, Backstage → 2 bandas
        estudoDeCaso.AdicionarFluxo("Quebrado", [a.Id, b.Id, c.Id]);        // 3 bandas, enquanto a aresta B→C existir
        estudoDeCaso.RemoverAresta(bc.Id);

        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);
        var resultado = new CalculadoraDEP().Calcular(contexto);

        contexto.Fluxos.Should().HaveCount(2);
        contexto.FluxosPrincipais.Should().ContainSingle().Which.Nome.Should().Be("Curto");
        resultado.Aplicavel.Should().BeTrue();
        resultado.Valor.Should().BeApproximately(2 / 5.0, 1e-12);
    }

    // RN08 — denominador zero: ESC é a exceção e vale 1
    [Fact]
    [Trait("Requisito", "RN08")]
    public void CalculadoraESC_DeveRetornarUm_QuandoNenhumaBandaTiverDoisNos()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Uma banda por nó");
        var a = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var tp = estudoDeCaso.AdicionarNo("Touchpoint", TipoDeNo.Touchpoint);
        estudoDeCaso.AdicionarAresta(a.Id, tp.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        Bruto(Eixo.ESC, contexto).Should().Be(1.0);
    }

    // RN08 — grafo sem arestas: VOL = Σw/M e TR = Σw(CRI)/Σw têm denominador zero e ficam não aplicáveis
    [Theory]
    [Trait("Requisito", "RN08")]
    [InlineData(Eixo.VOL)]
    [InlineData(Eixo.TR)]
    public void Calcular_DeveSerNaoAplicavel_QuandoOGrafoNaoTemArestas(Eixo eixo)
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sem arestas");
        estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var resultado = Calculadoras.Single(c => c.Eixo == eixo).Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().NotBeNullOrWhiteSpace();
    }

    // RN08 — banda vazia: sem nó em Backstage/Suporte não há isolamento a promediar
    [Fact]
    [Trait("Requisito", "RN08")]
    public void CalculadoraFLEX_DeveSerNaoAplicavel_QuandoNaoHaNoInterno()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Só Ator e Frontstage");
        var a = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var tp = estudoDeCaso.AdicionarNo("Touchpoint", TipoDeNo.Touchpoint);
        estudoDeCaso.AdicionarAresta(a.Id, tp.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var resultado = new CalculadoraFLEX().Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().NotBeNullOrWhiteSpace();
    }

    // TCC 4.4.2: isolamento(v) = 1 quando grau_total(v) = 0 (nó interno sem nenhuma aresta)
    [Fact]
    public void CalculadoraFLEX_DeveValerUm_QuandoOUnicoNoInternoNaoTemArestas()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Nó interno isolado");
        estudoDeCaso.AdicionarNo("Processo isolado", TipoDeNo.Processo);
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        Bruto(Eixo.FLEX, contexto).Should().Be(1.0);
    }

    // RN08 — grafo sem nós: não há comunidade, então AUT = |autônomas|/|C| não se aplica
    [Fact]
    [Trait("Requisito", "RN08")]
    public void CalculadoraAUT_DeveSerNaoAplicavel_QuandoOGrafoNaoTemNos()
    {
        var contexto = ConstrutorDeContextoDoMapa.Construir(EstudoDeCaso.Criar("Vazio"));

        var resultado = new CalculadoraAUT().Calcular(contexto);

        resultado.Aplicavel.Should().BeFalse();
        resultado.Motivo.Should().NotBeNullOrWhiteSpace();
    }
}
