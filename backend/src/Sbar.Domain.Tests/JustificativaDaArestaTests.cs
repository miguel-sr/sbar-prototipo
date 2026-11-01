using FluentAssertions;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Xunit;

namespace Sbar.Domain.Tests;

// RN03, RF03, US02 e CT06 (TCC, seção 5.3): toda aresta tem justificativa rastreável ao Blueprint.
public class JustificativaDaArestaTests
{
    private static (EstudoDeCaso Caso, No A, No B) NovoMapa()
    {
        var caso = EstudoDeCaso.Criar("Pedidos Digital");
        var a = caso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var b = caso.AdicionarNo("Portal", TipoDeNo.Touchpoint);
        return (caso, a, b);
    }

    [Fact]
    public void AdicionarAresta_DeveGuardarAJustificativaAparada()
    {
        var (caso, a, b) = NovoMapa();

        var aresta = caso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "  O cliente sempre acessa o portal (Blueprint, etapa 1)  ");

        aresta.Justificativa.Should().Be("O cliente sempre acessa o portal (Blueprint, etapa 1)");
    }

    // CT06 — aresta com justificativa vazia é rejeitada
    [Theory]
    [Trait("Requisito", "CT06")]
    [Trait("Requisito", "RN03")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void AdicionarAresta_DeveLancarNomeInvalidoException_QuandoAJustificativaForVazia(string justificativa)
    {
        var (caso, a, b) = NovoMapa();

        var acao = () => caso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, justificativa);

        acao.Should().Throw<NomeInvalidoException>().Which.Message.Should().Contain("Justificativa");
        caso.Arestas.Should().BeEmpty();
    }

    [Fact]
    public void AdicionarAresta_DeveAceitarJustificativaDe500CaracteresERecusarMais()
    {
        var (caso, a, b) = NovoMapa();

        var aresta = caso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, new string('x', 500));
        var acao = () => caso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, new string('x', 501));

        aresta.Justificativa.Should().HaveLength(500);
        acao.Should().Throw<NomeInvalidoException>().Which.Message.Should().Contain("500");
    }

    [Fact]
    public void AtualizarAresta_NaoDeveAlterarAAresta_QuandoAJustificativaNovaForInvalida()
    {
        var (caso, a, b) = NovoMapa();
        var aresta = caso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Original");

        var acao = () => caso.AtualizarAresta(aresta.Id, b.Id, a.Id, TipoDeAresta.Critica, 1, " ");

        acao.Should().Throw<NomeInvalidoException>();
        (aresta.NoOrigemId, aresta.NoDestinoId, aresta.Tipo, aresta.Peso.Valor, aresta.Justificativa)
            .Should().Be((a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Original"));
    }
}
