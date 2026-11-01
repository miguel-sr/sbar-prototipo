using FluentAssertions;
using Sbar.Application.Comparacao;
using Sbar.Domain.Metricas;
using Xunit;

namespace Sbar.Application.Tests.Comparacao;

// RF09/RN09 — o perfil mais próximo e a margem entre as distâncias (subsídio à decisão, TCC 4.6 e 7.10.1).
public class GeradorDeRecomendacaoTests
{
    private static DistanciaPerfil Distancia(string nome, double distancia) =>
        new(Guid.NewGuid(), nome, distancia, distancia / 5 * 100, []);

    [Fact]
    public void Gerar_DeveEscolherAMenorDistanciaEPreservarAOrdemDaLista()
    {
        var distancias = new[] { Distancia("Microsserviços", 1.508), Distancia("Monólito", 1.037) };

        var recomendacao = GeradorDeRecomendacao.Gerar(distancias);

        recomendacao.EstiloRecomendado.Should().Be("Monólito");
        recomendacao.Distancias.Select(d => d.PerfilNome).Should().Equal("Microsserviços", "Monólito");
        recomendacao.DistanciaMaxima.Should().Be(5);
    }

    [Fact]
    public void Gerar_DeveCalcularAMargemComoADiferencaEntreAsDuasMenoresDistancias()
    {
        var recomendacao = GeradorDeRecomendacao.Gerar([Distancia("A", 1.146), Distancia("B", 1.321), Distancia("C", 3.0)]);

        recomendacao.Margem.Should().BeApproximately(0.175, 1e-9); // Estudo de Caso 2 (TCC 7.10.1): 3,5% da escala
    }

    [Fact]
    public void Gerar_DeveTerMargemNula_QuandoHaApenasUmPerfil()
    {
        var recomendacao = GeradorDeRecomendacao.Gerar([Distancia("Único", 2.0)]);

        recomendacao.Margem.Should().BeNull();
        recomendacao.EstiloRecomendado.Should().Be("Único");
    }

    [Fact]
    public void Gerar_DeveTerMargemZero_QuandoDoisPerfisEmpatam()
    {
        var recomendacao = GeradorDeRecomendacao.Gerar([Distancia("A", 1.5), Distancia("B", 1.5)]);

        recomendacao.Margem.Should().Be(0);
    }
}
