using FluentAssertions;
using Sbar.Application.Grafo;
using Sbar.Domain.Enums;
using Xunit;

namespace Sbar.Application.Tests.Grafo;

public class ConstrutorDeContextoDoMapaTests
{
    [Fact]
    public void Construir_DeveMontarOContextoDoEstudoDeCaso1_ComCargasComunidadesEFluxos()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();

        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        contexto.N.Should().Be(15);
        contexto.M.Should().Be(21);
        contexto.BandasOcupadas.Should().BeEquivalentTo(
            [Banda.Ator, Banda.Frontstage, Banda.Backstage, Banda.Suporte, Banda.Externo]);
        contexto.Cargas[nos["DS"].Id].Should().Be(11);
        contexto.Comunidades.Quantidade.Should().Be(3);
        contexto.Comunidades.ModularidadeQ.Should().BeApproximately(0.4117, 0.00005);
        contexto.FluxosPrincipais.Should().HaveCount(4).And.OnlyContain(f => f.Valido && f.BandasPercorridas == 3);
    }

    [Fact]
    public void Construir_DeveMontarOContextoDoEstudoDeCaso2_ComCargasComunidadesEFluxos()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();

        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        contexto.N.Should().Be(15);
        contexto.M.Should().Be(21);
        contexto.BandasOcupadas.Should().BeEquivalentTo(
            [Banda.Ator, Banda.Frontstage, Banda.Backstage, Banda.Suporte, Banda.Externo]);
        contexto.Cargas[nos["DS"].Id].Should().Be(10);
        contexto.Comunidades.Quantidade.Should().Be(4);
        contexto.Comunidades.ModularidadeQ.Should().BeApproximately(0.3440, 0.00005);
        contexto.FluxosPrincipais.Select(f => f.BandasPercorridas).Should().Equal(4, 5, 5, 3, 3);
    }

}
