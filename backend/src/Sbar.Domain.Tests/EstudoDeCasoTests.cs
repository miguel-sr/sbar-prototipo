using FluentAssertions;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Xunit;

namespace Sbar.Domain.Tests;

public class EstudoDeCasoTests
{
    [Fact]
    public void AdicionarNo_DeveDerivarBandaCorreta_QuandoTipoForTouchpoint()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos Digital");

        var no = estudoDeCaso.AdicionarNo("Cardápio digital", TipoDeNo.Touchpoint);

        no.Banda.Should().Be(Banda.Frontstage);
        estudoDeCaso.Nos.Should().ContainSingle().Which.Should().Be(no);
    }

    [Fact]
    public void AdicionarAresta_DeveCriarAresta_QuandoOrigemEDestinoPertencemAoEstudoDeCaso()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos Digital");
        var origem = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var destino = estudoDeCaso.AdicionarNo("Cardápio digital", TipoDeNo.Touchpoint);

        var aresta = estudoDeCaso.AdicionarAresta(origem.Id, destino.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        estudoDeCaso.Arestas.Should().ContainSingle().Which.Should().Be(aresta);
        aresta.Peso.Valor.Should().Be(3);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AdicionarAresta_DeveLancarNoDeOutroEstudoDeCasoException_QuandoOrigemOuDestinoNaoPertencemAoEstudoDeCaso(bool origemDeFora)
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos Digital");
        var doCaso = estudoDeCaso.AdicionarNo("Cardápio digital", TipoDeNo.Touchpoint);
        var deFora = Guid.NewGuid();
        var (origem, destino) = origemDeFora ? (deFora, doCaso.Id) : (doCaso.Id, deFora);

        var acao = () => estudoDeCaso.AdicionarAresta(origem, destino, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        acao.Should().Throw<NoDeOutroEstudoDeCasoException>();
    }

    [Fact]
    public void AdicionarAresta_DeveLancarArestaAutoReferenciadaException_QuandoOrigemForIgualAoDestino()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos Digital");
        var no = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);

        var acao = () => estudoDeCaso.AdicionarAresta(no.Id, no.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        acao.Should().Throw<ArestaAutoReferenciadaException>();
    }
}
