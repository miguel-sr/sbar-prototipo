using FluentAssertions;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Xunit;

namespace Sbar.Domain.Tests;

// Validação de nomes, edição/exclusão de Nós e Arestas e ordem de cadastro do agregado EstudoDeCaso.
public class EstudoDeCasoEdicaoTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DeveLancarNomeInvalidoException_QuandoNomeVazioOuSoEspacos(string? nome)
    {
        var acao = () => EstudoDeCaso.Criar(nome!);

        acao.Should().Throw<NomeInvalidoException>().Which.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public void Criar_DeveAplicarTrimNoNomeEVirarNullNaDescricaoVazia()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("  Pedidos  ", "   ");

        estudoDeCaso.Nome.Should().Be("Pedidos");
        estudoDeCaso.DominioDescricao.Should().BeNull();
    }

    [Fact]
    public void Criar_DeveAceitarNomeNoLimiteERecusarAcimaDele()
    {
        EstudoDeCaso.Criar(new string('x', 120)).Nome.Should().HaveLength(120);

        var acao = () => EstudoDeCaso.Criar(new string('x', 121));

        acao.Should().Throw<NomeInvalidoException>();
    }

    [Fact]
    public void Criar_DeveLancarNomeInvalidoException_QuandoDescricaoPassaDeMilCaracteres()
    {
        var acao = () => EstudoDeCaso.Criar("Pedidos", new string('d', 1001));

        acao.Should().Throw<NomeInvalidoException>();
    }

    [Fact]
    public void AdicionarNo_DeveLancarNomeInvalidoException_QuandoNomeVazioOuAcimaDeCemCaracteres()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");

        var vazio = () => estudoDeCaso.AdicionarNo("  ", TipoDeNo.Processo);
        var longo = () => estudoDeCaso.AdicionarNo(new string('x', 101), TipoDeNo.Processo);

        vazio.Should().Throw<NomeInvalidoException>();
        longo.Should().Throw<NomeInvalidoException>();
        estudoDeCaso.Nos.Should().BeEmpty();
    }

    [Fact]
    public void Atualizar_DeveTrocarNomeEDescricao()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos", "antiga");

        estudoDeCaso.Atualizar(" Pedidos v2 ", "nova");

        estudoDeCaso.Nome.Should().Be("Pedidos v2");
        estudoDeCaso.DominioDescricao.Should().Be("nova");
    }

    [Fact]
    public void Atualizar_NaoDeveAlterarNada_QuandoNovoNomeInvalido()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos", "antiga");

        var acao = () => estudoDeCaso.Atualizar("", "nova");

        acao.Should().Throw<NomeInvalidoException>();
        estudoDeCaso.Nome.Should().Be("Pedidos");
        estudoDeCaso.DominioDescricao.Should().Be("antiga");
    }

    [Fact]
    public void AtualizarNo_DeveRederivarABanda_QuandoOTipoMuda()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var no = estudoDeCaso.AdicionarNo("Carrinho", TipoDeNo.Touchpoint);

        var atualizado = estudoDeCaso.AtualizarNo(no.Id, "Fila de pedidos", TipoDeNo.Processo);

        atualizado.Nome.Should().Be("Fila de pedidos");
        atualizado.Tipo.Should().Be(TipoDeNo.Processo);
        atualizado.Banda.Should().Be(Banda.Backstage);
    }

    [Fact]
    public void AtualizarNo_DeveLancarRecursoNaoEncontradoException_QuandoNoNaoExiste()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");

        var acao = () => estudoDeCaso.AtualizarNo(Guid.NewGuid(), "X", TipoDeNo.Ator);

        acao.Should().Throw<RecursoNaoEncontradoException>();
    }

    [Fact]
    public void RemoverNo_DeveRemoverTambemAsArestasQueEntramOuSaemDele()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Touchpoint);
        var c = estudoDeCaso.AdicionarNo("C", TipoDeNo.Processo);
        estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");
        estudoDeCaso.AdicionarAresta(b.Id, c.Id, TipoDeAresta.Sincrona, 2, "Justificativa de teste");
        var sobrevivente = estudoDeCaso.AdicionarAresta(a.Id, c.Id, TipoDeAresta.Batch, 1, "Justificativa de teste");

        estudoDeCaso.RemoverNo(b.Id);

        estudoDeCaso.Nos.Select(n => n.Id).Should().BeEquivalentTo(new[] { a.Id, c.Id });
        estudoDeCaso.Arestas.Should().ContainSingle().Which.Id.Should().Be(sobrevivente.Id);
    }

    [Fact]
    public void RemoverNo_DeveLancarRecursoNaoEncontradoException_QuandoNoNaoExiste()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");

        var acao = () => estudoDeCaso.RemoverNo(Guid.NewGuid());

        acao.Should().Throw<RecursoNaoEncontradoException>();
    }

    [Fact]
    public void AtualizarAresta_DeveTrocarTipoPesoNosEJustificativaAparada()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Touchpoint);
        var c = estudoDeCaso.AdicionarNo("C", TipoDeNo.Processo);
        var aresta = estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        var atualizada = estudoDeCaso.AtualizarAresta(aresta.Id, b.Id, c.Id, TipoDeAresta.Critica, 1, "  Nova justificativa  ");

        atualizada.NoOrigemId.Should().Be(b.Id);
        atualizada.NoDestinoId.Should().Be(c.Id);
        atualizada.Tipo.Should().Be(TipoDeAresta.Critica);
        atualizada.Peso.Valor.Should().Be(1);
        atualizada.Justificativa.Should().Be("Nova justificativa");
    }

    [Fact]
    public void AtualizarAresta_DeveManterAArestaIntacta_QuandoAAtualizacaoEInvalida()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Touchpoint);
        var aresta = estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        var autoLoop = () => estudoDeCaso.AtualizarAresta(aresta.Id, a.Id, a.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");
        var pesoInvalido = () => estudoDeCaso.AtualizarAresta(aresta.Id, a.Id, b.Id, TipoDeAresta.Batch, 4, "Justificativa de teste");
        var noDeFora = () => estudoDeCaso.AtualizarAresta(aresta.Id, a.Id, Guid.NewGuid(), TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        autoLoop.Should().Throw<ArestaAutoReferenciadaException>();
        pesoInvalido.Should().Throw<PesoDeArestaInvalidoException>();
        noDeFora.Should().Throw<NoDeOutroEstudoDeCasoException>();
        aresta.Tipo.Should().Be(TipoDeAresta.Sincrona);
        aresta.Peso.Valor.Should().Be(3);
    }

    [Fact]
    public void RemoverAresta_DeveRemoverSoAAresta_EMantemOsNos()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Touchpoint);
        var aresta = estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste");

        estudoDeCaso.RemoverAresta(aresta.Id);

        estudoDeCaso.Arestas.Should().BeEmpty();
        estudoDeCaso.Nos.Should().HaveCount(2);
    }

    [Fact]
    public void RemoverAresta_DeveLancarRecursoNaoEncontradoException_QuandoArestaNaoExiste()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");

        var acao = () => estudoDeCaso.RemoverAresta(Guid.NewGuid());

        acao.Should().Throw<RecursoNaoEncontradoException>();
    }

    [Fact]
    public void Nos_DevemVirNaOrdemDeCadastro_EAOrdemNaoSeRepeteDepoisDeUmaExclusao()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var nomes = new[] { "Zeta", "Alfa", "Mike", "Beta", "Omega" };
        var nos = nomes.Select(n => estudoDeCaso.AdicionarNo(n, TipoDeNo.Processo)).ToList();

        estudoDeCaso.Nos.Select(n => n.Nome).Should().Equal(nomes);

        estudoDeCaso.RemoverNo(nos[1].Id);
        var novo = estudoDeCaso.AdicionarNo("Novo", TipoDeNo.Processo);

        estudoDeCaso.Nos.Select(n => n.Nome).Should().Equal("Zeta", "Mike", "Beta", "Omega", "Novo");
        novo.Ordem.Should().Be(6);
    }

    [Fact]
    public void Arestas_DevemVirNaOrdemDeCadastro()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Pedidos");
        var a = estudoDeCaso.AdicionarNo("A", TipoDeNo.Ator);
        var b = estudoDeCaso.AdicionarNo("B", TipoDeNo.Touchpoint);
        var pesos = new[] { 3, 1, 2, 3, 1, 2 };
        var criadas = pesos.Select(p => estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, p, "Justificativa de teste")).ToList();

        estudoDeCaso.Arestas.Select(x => x.Id).Should().Equal(criadas.Select(x => x.Id));
    }
}
