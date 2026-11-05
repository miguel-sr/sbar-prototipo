using FluentAssertions;
using Sbar.Application.Grafo;
using Xunit;

namespace Sbar.Application.Tests.Grafo;

public class CalculadoraDeCargaTests
{
    [Fact]
    public void Calcular_DeveBaterComQuadro9_QuandoForEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();

        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var esperado = new Dictionary<string, int>
        {
            ["A"] = 6, ["TP1"] = 8, ["TP2"] = 8, ["TP3"] = 4, ["TP4"] = 4,
            ["P1"] = 9, ["P2"] = 8, ["P3"] = 6, ["P4"] = 5, ["P5"] = 8,
            ["S1"] = 4, ["S2"] = 6, ["DS"] = 11, ["E1"] = 5, ["E2"] = 2,
        };

        foreach (var (codigo, valorEsperado) in esperado)
            cargas[nos[codigo].Id].Should().Be(valorEsperado, $"carga de {codigo}");
    }

    [Fact]
    public void Calcular_DeveBaterComQuadro16_QuandoForEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();

        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);

        var esperado = new Dictionary<string, int>
        {
            ["A"] = 8, ["TP1"] = 8, ["TP2"] = 9, ["TP3"] = 6, ["TP4"] = 5,
            ["P1"] = 6, ["P2"] = 9, ["P3"] = 4, ["P4"] = 2, ["P5"] = 6,
            ["S1"] = 4, ["S2"] = 6, ["DS"] = 10, ["E1"] = 6, ["E2"] = 1,
        };

        foreach (var (codigo, valorEsperado) in esperado)
            cargas[nos[codigo].Id].Should().Be(valorEsperado, $"carga de {codigo}");
    }
}
