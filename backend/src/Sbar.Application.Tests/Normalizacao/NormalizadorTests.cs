using FluentAssertions;
using Sbar.Application.Normalizacao;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.Normalizacao;

public class NormalizadorTests
{
    // Fórmula 4.5: Norm = 5 × (M − mín) / (máx − mín); CO = 25 entre 6 e 40 → 5 × 19 / 34
    [Fact]
    public void Normalizar_DeveInterpolarLinearmenteEntreOsLimites()
    {
        var normalizado = Normalizador.Normalizar(25.00, new LimitesDeEixo(6, 40));

        normalizado.Should().BeApproximately(5.0 * 19 / 34, 1e-12);
    }

    // CT07 — métrica acima do limite máximo: valor normalizado igual a 5 (RN06)
    [Fact]
    [Trait("Requisito", "CT07")]
    [Trait("Requisito", "RN06")]
    public void Normalizar_DeveLimitarEmCinco_QuandoValorBrutoUltrapassarOMaximo()
    {
        var normalizado = Normalizador.Normalizar(50, new LimitesDeEixo(0, 1));

        normalizado.Should().Be(5);
    }

    [Fact]
    [Trait("Requisito", "RN06")]
    public void Normalizar_DeveLimitarEmZero_QuandoValorBrutoForMenorQueOMinimo()
    {
        var normalizado = Normalizador.Normalizar(-10, new LimitesDeEixo(0, 1));

        normalizado.Should().Be(0);
    }
}
