using FluentAssertions;
using Sbar.Domain.Exceptions;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Domain.Tests;

public class PesoTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Criar_DeveAceitarValor_QuandoValorForUmDoisOuTres(int valor)
    {
        var peso = Peso.Criar(valor);

        peso.Valor.Should().Be(valor);
        ((int)peso).Should().Be(valor);
    }

    // RN02, CT05 — intensidade fora de {1, 2, 3} é rejeitada
    [Theory]
    [Trait("Requisito", "CT05")]
    [Trait("Requisito", "RN02")]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Criar_DeveLancarPesoDeArestaInvalidoException_QuandoValorForaDoIntervalo(int valor)
    {
        var acao = () => Peso.Criar(valor);

        acao.Should().Throw<PesoDeArestaInvalidoException>();
    }

    // Objeto de valor: a igualdade é pelo valor (importa ao comparar arestas e agrupar por peso)
    [Fact]
    public void Equals_DeveSerVerdadeiro_QuandoOsPesosTemOMesmoValor()
    {
        Peso.Criar(2).Should().Be(Peso.Criar(2));
        Peso.Criar(2).GetHashCode().Should().Be(Peso.Criar(2).GetHashCode());
        Peso.Criar(2).Should().NotBe(Peso.Criar(3));
        Peso.Criar(2).Equals(null).Should().BeFalse();
    }
}
