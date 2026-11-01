using FluentAssertions;
using Sbar.Application.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Xunit;

namespace Sbar.Application.Tests.Grafo;

public class ClassificadorDeGargaloTests
{
    // CT01 — pontos potencialmente críticos do Estudo de Caso 1: P1 e DS (Quadro 23)
    [Fact]
    [Trait("Requisito", "CT01")]
    [Trait("Requisito", "RN05")]
    public void Classificar_DeveIdentificarP1EDS_QuandoForEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);
        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        var gargalos = ClassificadorDeGargalo.Classificar(estudoDeCaso, cargas, estatisticas);

        gargalos.Should().BeEquivalentTo([nos["P1"].Id, nos["DS"].Id]);
    }

    // Quadro 33: TP2, P2 e DS — inclui um nó de Frontstage, que não entra no DISP mas precisa ser destacado
    [Fact]
    [Trait("Requisito", "RN05")]
    public void Classificar_DeveIdentificarTP2P2EDS_QuandoForEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);
        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        var gargalos = ClassificadorDeGargalo.Classificar(estudoDeCaso, cargas, estatisticas);

        gargalos.Should().BeEquivalentTo([nos["TP2"].Id, nos["P2"].Id, nos["DS"].Id]);
    }

    [Fact]
    public void Classificar_NuncaDeveMarcarGargalo_QuandoBandaTiverExatamenteDoisNos()
    {
        // Identidade algébrica: para n=2, μ+σ = max(a,b) sempre —
        // então o maior valor nunca é ESTRITAMENTE maior que o limiar, não importa a disparidade
        // entre as duas cargas. Aqui E1 fica com carga 9 e E2 com carga 1 (disparidade grande).
        var estudoDeCaso = EstudoDeCaso.Criar("Banda de dois nós");
        var ator = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var e1 = estudoDeCaso.AdicionarNo("Externo 1", TipoDeNo.Externo);
        var e2 = estudoDeCaso.AdicionarNo("Externo 2", TipoDeNo.Externo);
        estudoDeCaso.AdicionarAresta(ator.Id, e1.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(ator.Id, e1.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(ator.Id, e1.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(ator.Id, e2.Id, TipoDeAresta.Sincrona, 1, "Justificativa de teste");

        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);
        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        cargas[e1.Id].Should().Be(9);
        cargas[e2.Id].Should().Be(1);

        var gargalos = ClassificadorDeGargalo.Classificar(estudoDeCaso, cargas, estatisticas);

        gargalos.Should().NotContain(e1.Id).And.NotContain(e2.Id);
    }

    [Fact]
    public void Classificar_NaoDeveMarcarGargalo_QuandoCargaForExatamenteIgualAoLimiar()
    {
        // Três nós com carga idêntica (2) → μ=2, σ=0 → limiar=2 → carga==limiar, nunca > limiar.
        var estudoDeCaso = EstudoDeCaso.Criar("Carga igual ao limiar");
        var externo = estudoDeCaso.AdicionarNo("Externo", TipoDeNo.Ator);
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Processo);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Processo);
        var c = estudoDeCaso.AdicionarNo("C", TipoDeNo.Processo);
        estudoDeCaso.AdicionarAresta(externo.Id, a.Id, TipoDeAresta.Sincrona, 2, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(externo.Id, b.Id, TipoDeAresta.Sincrona, 2, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(externo.Id, c.Id, TipoDeAresta.Sincrona, 2, "Justificativa de teste");

        var cargas = CalculadoraDeCarga.Calcular(estudoDeCaso);
        var estatisticas = CalculadoraDeEstatisticasDeBanda.Calcular(estudoDeCaso, cargas);

        var gargalos = ClassificadorDeGargalo.Classificar(estudoDeCaso, cargas, estatisticas);

        gargalos.Should().NotContain(a.Id).And.NotContain(b.Id).And.NotContain(c.Id);
    }
}
