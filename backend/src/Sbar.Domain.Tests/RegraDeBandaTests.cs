using FluentAssertions;
using Sbar.Domain.Enums;
using Sbar.Domain.Services;
using Xunit;

namespace Sbar.Domain.Tests;

public class RegraDeBandaTests
{
    // RN01 — cada nó pertence a exatamente uma banda, sempre derivada do tipo
    [Theory]
    [Trait("Requisito", "RN01")]
    [InlineData(TipoDeNo.Ator, Banda.Ator)]
    [InlineData(TipoDeNo.Touchpoint, Banda.Frontstage)]
    [InlineData(TipoDeNo.Processo, Banda.Backstage)]
    [InlineData(TipoDeNo.Suporte, Banda.Suporte)]
    [InlineData(TipoDeNo.Dados, Banda.Suporte)]
    [InlineData(TipoDeNo.Externo, Banda.Externo)]
    public void Derivar_DeveRetornarBandaCorreta_QuandoTipoForValido(TipoDeNo tipo, Banda bandaEsperada)
    {
        RegraDeBanda.Derivar(tipo).Should().Be(bandaEsperada);
    }

    [Fact]
    public void Derivar_DeveLancarArgumentOutOfRange_QuandoTipoNaoExiste()
    {
        var acao = () => RegraDeBanda.Derivar((TipoDeNo)99);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
