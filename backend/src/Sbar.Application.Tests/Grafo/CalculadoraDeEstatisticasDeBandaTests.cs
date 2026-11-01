using FluentAssertions;
using Sbar.Application.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Xunit;

namespace Sbar.Application.Tests.Grafo;

public class CalculadoraDeEstatisticasDeBandaTests
{
    [Fact]
    public void Calcular_DeveBaterComQuadro9_QuandoForEstudoDeCaso1()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        estatisticas[Banda.Ator].Media.Should().BeApproximately(6.00, 0.01);
        estatisticas[Banda.Ator].DesvioPadrao.Should().BeApproximately(0.00, 0.01);
        estatisticas[Banda.Frontstage].Media.Should().BeApproximately(6.00, 0.01);
        estatisticas[Banda.Frontstage].DesvioPadrao.Should().BeApproximately(2.00, 0.01);
        estatisticas[Banda.Backstage].Media.Should().BeApproximately(7.20, 0.01);
        estatisticas[Banda.Backstage].DesvioPadrao.Should().BeApproximately(1.47, 0.01);
        estatisticas[Banda.Suporte].Media.Should().BeApproximately(7.00, 0.01);
        estatisticas[Banda.Suporte].DesvioPadrao.Should().BeApproximately(2.94, 0.01);
        estatisticas[Banda.Externo].Media.Should().BeApproximately(3.50, 0.01);
        estatisticas[Banda.Externo].DesvioPadrao.Should().BeApproximately(1.50, 0.01);
    }

    [Fact]
    public void Calcular_DeveBaterComQuadro16_QuandoForEstudoDeCaso2()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        estatisticas[Banda.Ator].Media.Should().BeApproximately(8.00, 0.01);
        estatisticas[Banda.Ator].DesvioPadrao.Should().BeApproximately(0.00, 0.01);
        estatisticas[Banda.Frontstage].Media.Should().BeApproximately(7.00, 0.01);
        estatisticas[Banda.Frontstage].DesvioPadrao.Should().BeApproximately(1.58, 0.01);
        estatisticas[Banda.Backstage].Media.Should().BeApproximately(5.40, 0.01);
        estatisticas[Banda.Backstage].DesvioPadrao.Should().BeApproximately(2.33, 0.01);
        estatisticas[Banda.Suporte].Media.Should().BeApproximately(6.67, 0.01);
        estatisticas[Banda.Suporte].DesvioPadrao.Should().BeApproximately(2.49, 0.01);
        estatisticas[Banda.Externo].Media.Should().BeApproximately(3.50, 0.01);
        estatisticas[Banda.Externo].DesvioPadrao.Should().BeApproximately(2.50, 0.01);
    }

    [Fact]
    public void Calcular_DeveResultarEmDesvioPadraoZero_QuandoBandaTiverUmUnicoNo()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Banda de um nó");
        estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        estatisticas[Banda.Ator].DesvioPadrao.Should().Be(0);
    }

    [Fact]
    public void Calcular_NaoDeveConterEntrada_QuandoBandaEstiverVazia()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sem nós externos");
        estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        estatisticas.Should().NotContainKey(Banda.Externo);
    }
}
