using FluentAssertions;
using Sbar.Application.Parametros;
using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.Parametros;

// Os padrões do sistema (Quadro 7 e Quadro 8 do TCC revisado) são copiados para cada conta nova: precisam ser um
// conjunto válido pelas mesmas regras que a edição aplica (RN07) e reproduzir os valores publicados.
public class PadroesDoSistemaTests
{
    [Fact]
    public void Eixos_CobremOsDezEixos_ESomamPesoUm()
    {
        PadroesDoSistema.Eixos.Select(e => e.Eixo).Should().BeEquivalentTo(Enum.GetValues<Eixo>());
        PadroesDoSistema.Eixos.Sum(e => e.Peso).Should().BeApproximately(1.0, 1e-9);

        var validar = () => ConjuntoDeParametros.ValidarEixos(PadroesDoSistema.Eixos);
        validar.Should().NotThrow();
    }

    [Fact]
    public void Eixos_ReproduzemOsLimitesDoQuadro7()
    {
        PadroesDoSistema.Eixos.Single(e => e.Eixo == Eixo.CO).Should().Be(new ParametroDeEixo(Eixo.CO, 6, 40, 0.1));
        PadroesDoSistema.Eixos.Single(e => e.Eixo == Eixo.UX).Should().Be(new ParametroDeEixo(Eixo.UX, 0, 3, 0.1));
        PadroesDoSistema.Eixos.Single(e => e.Eixo == Eixo.VOL).Should().Be(new ParametroDeEixo(Eixo.VOL, 1, 3, 0.1));
        PadroesDoSistema.Eixos.Single(e => e.Eixo == Eixo.DEP).Should().Be(new ParametroDeEixo(Eixo.DEP, 0.2, 1, 0.1));
    }

    [Fact]
    public void Perfis_SaoOMonolitoModularEOsMicrosservicosDoQuadro8()
    {
        PadroesDoSistema.Perfis.Select(p => p.Nome).Should().Equal("Monólito Modular (provisório)", "Microsserviços (provisório)");

        var microsservicos = PadroesDoSistema.Perfis[1];
        microsservicos.ValorVOL.Should().Be(2.0);
        microsservicos.ValorAUT.Should().Be(4.5);
    }
}
