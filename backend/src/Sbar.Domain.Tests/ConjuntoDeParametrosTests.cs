using FluentAssertions;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Domain.Tests;

// RF11, RN07 e CT10 — validação dos parâmetros de análise: limites de normalização (Quadro 7) e pesos p_i (4.5.1).
public class ConjuntoDeParametrosTests
{
    private static List<ParametroDeEixo> DezEixosValidos() =>
        Enum.GetValues<Eixo>().Select(e => new ParametroDeEixo(e, 0, 1, 0.1)).ToList();

    [Fact]
    public void ValidarEixos_NaoDeveLancar_QuandoOsDezPesosSaoZeroVirgulaUm()
    {
        // 10 × 0,1 em ponto flutuante dá 0,9999999999999999 — a tolerância evita rejeitar a configuração padrão do método.
        var acao = () => ConjuntoDeParametros.ValidarEixos(DezEixosValidos());

        acao.Should().NotThrow();
    }

    [Fact]
    public void ValidarEixos_NaoDeveLancar_QuandoOsPesosSaoDiferentesMasSomamUm()
    {
        var eixos = DezEixosValidos();
        eixos[0] = eixos[0] with { Peso = 0.5 };
        for (var i = 1; i < 10; i++)
            eixos[i] = eixos[i] with { Peso = 0.5 / 9 };

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().NotThrow();
    }

    // CT10 — soma dos pesos diferente de 1 (para mais ou para menos) bloqueia, com mensagem de erro
    [Theory]
    [Trait("Requisito", "CT10")]
    [Trait("Requisito", "RN07")]
    [InlineData(0.2, "Soma dos pesos igual a 1,1; deve ser 1.")]
    [InlineData(0.0, "Soma dos pesos igual a 0,9; deve ser 1.")]
    public void ValidarEixos_DeveLancarParametrosInvalidosException_QuandoASomaDosPesosNaoEUm(double pesoDeCo, string mensagem)
    {
        var eixos = DezEixosValidos();
        eixos[0] = eixos[0] with { Peso = pesoDeCo };

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<ParametrosInvalidosException>().WithMessage(mensagem);
    }

    [Fact]
    public void ValidarEixos_DeveLancarParametrosInvalidosException_QuandoUmEixoFalta()
    {
        var eixos = DezEixosValidos().Where(e => e.Eixo != Eixo.DISP).ToList();

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<ParametrosInvalidosException>().WithMessage("*DISP*");
    }

    [Fact]
    public void ValidarEixos_DeveLancarParametrosInvalidosException_QuandoUmEixoSeRepete()
    {
        var eixos = DezEixosValidos();
        eixos.Add(new ParametroDeEixo(Eixo.CO, 0, 1, 0));

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<ParametrosInvalidosException>().WithMessage("*CO*mais de uma vez*");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void ValidarEixos_DeveLancarLimitesDeNormalizacaoInvalidosException_QuandoMinimoNaoEMenorQueMaximo(double minimo, double maximo)
    {
        var eixos = DezEixosValidos();
        eixos[3] = eixos[3] with { Minimo = minimo, Maximo = maximo };

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<LimitesDeNormalizacaoInvalidosException>().WithMessage("*UX*");
    }

    [Fact]
    public void ValidarEixos_DeveLancarParametrosInvalidosException_QuandoUmPesoENegativo()
    {
        var eixos = DezEixosValidos();
        eixos[0] = eixos[0] with { Peso = -0.1 };
        eixos[1] = eixos[1] with { Peso = 0.3 }; // soma continua 1: o que reprova é o peso negativo

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<ParametrosInvalidosException>().WithMessage("*peso do eixo CO*maior ou igual a zero*");
    }

    // NaN e infinito passariam silenciosamente por `máximo <= mínimo` (a comparação é falsa) e envenenariam a normalização.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidarEixos_DeveLancarParametrosInvalidosException_QuandoUmLimiteNaoENumerico(double valor)
    {
        var eixos = DezEixosValidos();
        eixos[0] = eixos[0] with { Maximo = valor };

        var acao = () => ConjuntoDeParametros.ValidarEixos(eixos);

        acao.Should().Throw<ParametrosInvalidosException>().WithMessage("*limites do eixo CO*números finitos*");
    }

    [Fact]
    public void ConfiguracaoDeEixo_Atualizar_NaoDeveAlterarNada_QuandoOMaximoNaoSuperaOMinimo()
    {
        var configuracao = ConfiguracaoDeEixo.Criar(Eixo.CO, 6, 40, 0.1);

        var acao = () => configuracao.Atualizar(40, 40, 0.2);

        acao.Should().Throw<LimitesDeNormalizacaoInvalidosException>();
        (configuracao.LimiteMinimo, configuracao.LimiteMaximo, configuracao.Peso).Should().Be((6.0, 40.0, 0.1));
    }
}
