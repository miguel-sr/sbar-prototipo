using FluentAssertions;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Xunit;

namespace Sbar.Domain.Tests;

// RF05, CT09 e US03 (TCC, seções 4.2.7 e 5.3): fluxos principais cadastrados pelo usuário e verificados
// por caminho dirigido contra as Arestas do Mapa.
public class FluxosPrincipaisTests
{
    // Mapa mínimo: Cliente (Ator) → Portal (Frontstage) → Validação (Backstage) → Repositório (Suporte); sem aresta de volta.
    private static (EstudoDeCaso Caso, No Cliente, No Portal, No Validacao, No Repositorio) NovoMapa()
    {
        var caso = EstudoDeCaso.Criar("Pedidos Digital");
        var cliente = caso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var portal = caso.AdicionarNo("Portal", TipoDeNo.Touchpoint);
        var validacao = caso.AdicionarNo("Validação do pedido", TipoDeNo.Processo);
        var repositorio = caso.AdicionarNo("Repositório de pedidos", TipoDeNo.Dados);

        caso.AdicionarAresta(cliente.Id, portal.Id, TipoDeAresta.Sincrona, 3, "O cliente acessa o portal");
        caso.AdicionarAresta(portal.Id, validacao.Id, TipoDeAresta.Sincrona, 3, "O pedido é validado");
        caso.AdicionarAresta(validacao.Id, repositorio.Id, TipoDeAresta.Sincrona, 3, "O pedido é gravado");

        return (caso, cliente, portal, validacao, repositorio);
    }

    [Fact]
    public void AdicionarFluxo_DeveGuardarOsNosNaOrdemInformadaEAparaONome_ERepetirNosQuandoOCaminhoRevisita()
    {
        var (caso, cliente, portal, validacao, repositorio) = NovoMapa();
        caso.AdicionarAresta(repositorio.Id, validacao.Id, TipoDeAresta.Sincrona, 2, "Retorno da gravação"); // permite revisitar a Validação

        var fluxo = caso.AdicionarFluxo("  Pedido  ", [cliente.Id, portal.Id, validacao.Id, repositorio.Id, validacao.Id]);

        fluxo.Nome.Should().Be("Pedido");
        fluxo.NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id, repositorio.Id, validacao.Id);
        fluxo.Passos.Select(p => p.Posicao).Should().Equal(1, 2, 3, 4, 5);
        caso.Fluxos.Should().ContainSingle().Which.Should().Be(fluxo);
    }

    [Fact]
    public void AdicionarFluxo_DevePermitirFluxoQueNaoComecaNoAtor_ENaoTerminaEmNoSemSaida()
    {
        var (caso, _, portal, validacao, _) = NovoMapa();

        var fluxo = caso.AdicionarFluxo("Só o miolo", [portal.Id, validacao.Id]);

        caso.AvaliarFluxo(fluxo).Valido.Should().BeTrue();
    }

    [Fact]
    public void AvaliarFluxo_DeveContarAsBandasDistintasDoCaminhoIncluindoOrigemEDestino()
    {
        var (caso, cliente, portal, validacao, repositorio) = NovoMapa();

        var completo = caso.AdicionarFluxo("Completo", [cliente.Id, portal.Id, validacao.Id, repositorio.Id]);
        var curto = caso.AdicionarFluxo("Curto", [cliente.Id, portal.Id]);

        caso.AvaliarFluxo(completo).BandasPercorridas.Should().Be(4); // Ator, Frontstage, Backstage, Suporte
        caso.AvaliarFluxo(curto).BandasPercorridas.Should().Be(2);
    }

    [Fact]
    [Trait("Requisito", "CT09")]
    [Trait("Requisito", "RF05")]
    public void AdicionarFluxo_DeveLancarFluxoInvalidoException_QuandoUmaEtapaNaoTemArestaEIndicarAEtapa()
    {
        var (caso, cliente, portal, _, repositorio) = NovoMapa();

        // Portal → Repositório não existe (só passa pela Validação): a etapa 2 está ausente (CT09)
        var acao = () => caso.AdicionarFluxo("Atalho", [cliente.Id, portal.Id, repositorio.Id]);

        acao.Should().Throw<FluxoInvalidoException>()
            .Which.Message.Should().Contain("etapa 2").And.Contain("Portal").And.Contain("Repositório de pedidos");
        caso.Fluxos.Should().BeEmpty();
    }

    [Fact]
    [Trait("Requisito", "CT09")]
    public void AdicionarFluxo_DeveConsiderarADirecaoDaAresta()
    {
        var (caso, cliente, portal, _, _) = NovoMapa();

        var acao = () => caso.AdicionarFluxo("Ao contrário", [portal.Id, cliente.Id]); // só existe Cliente → Portal

        acao.Should().Throw<FluxoInvalidoException>().Which.Message.Should().Contain("etapa 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AdicionarFluxo_DeveLancarFluxoInvalidoException_QuandoTemMenosDeDoisNos(int quantidade)
    {
        var (caso, cliente, _, _, _) = NovoMapa();

        var acao = () => caso.AdicionarFluxo("Curto demais", Enumerable.Repeat(cliente.Id, quantidade).ToList());

        acao.Should().Throw<FluxoInvalidoException>().Which.Message.Should().Contain("pelo menos 2");
    }

    [Fact]
    public void AdicionarFluxo_DeveLancarNoDeOutroEstudoDeCasoException_QuandoUmNoNaoPertenceAoCaso()
    {
        var (caso, cliente, _, _, _) = NovoMapa();

        var acao = () => caso.AdicionarFluxo("Com nó de fora", [cliente.Id, Guid.NewGuid()]);

        acao.Should().Throw<NoDeOutroEstudoDeCasoException>();
    }

    [Theory]
    [InlineData(0, 'x')]   // vazio
    [InlineData(3, ' ')]   // só espaços
    [InlineData(101, 'x')] // acima do limite de 100
    public void AdicionarFluxo_DeveLancarNomeInvalidoException_QuandoNomeVazioOuAcimaDeCemCaracteres(int tamanho, char caractere)
    {
        var (caso, cliente, portal, _, _) = NovoMapa();

        var acao = () => caso.AdicionarFluxo(new string(caractere, tamanho), [cliente.Id, portal.Id]);

        acao.Should().Throw<NomeInvalidoException>();
        caso.Fluxos.Should().BeEmpty();
    }

    // US03: excluir Nó ou Aresta NUNCA apaga nem altera o fluxo em silêncio — ele só passa a ser inválido.
    [Fact]
    [Trait("Requisito", "US03")]
    public void AvaliarFluxo_DeveFicarInvalidoSemSerApagado_QuandoAArestaDaEtapaForExcluida()
    {
        var (caso, cliente, portal, validacao, repositorio) = NovoMapa();
        var fluxo = caso.AdicionarFluxo("Pedido", [cliente.Id, portal.Id, validacao.Id, repositorio.Id]);
        var portalParaValidacao = caso.Arestas.Single(a => a.NoOrigemId == portal.Id && a.NoDestinoId == validacao.Id);

        caso.RemoverAresta(portalParaValidacao.Id);
        var avaliacao = caso.AvaliarFluxo(fluxo);

        caso.Fluxos.Should().ContainSingle();
        fluxo.NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id, repositorio.Id);
        avaliacao.Valido.Should().BeFalse();
        avaliacao.EtapaAusente.Should().NotBeNull();
        avaliacao.EtapaAusente!.Posicao.Should().Be(2);
        (avaliacao.EtapaAusente.OrigemId, avaliacao.EtapaAusente.DestinoId).Should().Be((portal.Id, validacao.Id));
        avaliacao.EtapaAusente.Motivo.Should().Contain("Portal");
    }

    [Fact]
    public void AvaliarFluxo_DeveFicarInvalidoSemSerApagado_QuandoUmNoDoCaminhoForExcluido()
    {
        var (caso, cliente, portal, validacao, repositorio) = NovoMapa();
        var fluxo = caso.AdicionarFluxo("Pedido", [cliente.Id, portal.Id, validacao.Id, repositorio.Id]);

        caso.RemoverNo(validacao.Id);
        var avaliacao = caso.AvaliarFluxo(fluxo);

        caso.Fluxos.Should().ContainSingle();
        fluxo.NosIds.Should().Contain(validacao.Id); // o passo continua lá
        avaliacao.Valido.Should().BeFalse();
        avaliacao.EtapaAusente!.Posicao.Should().Be(2);
        avaliacao.EtapaAusente.Motivo.Should().Contain("removido");
        avaliacao.BandasPercorridas.Should().Be(3); // só conta os nós que ainda existem: Ator, Frontstage, Suporte
    }

    [Fact]
    public void AvaliarFluxo_DeveVoltarAFicarValido_QuandoAArestaForCadastradaDeNovo()
    {
        var (caso, cliente, portal, _, _) = NovoMapa();
        var fluxo = caso.AdicionarFluxo("Entrada", [cliente.Id, portal.Id]);
        caso.RemoverAresta(caso.Arestas.Single(a => a.NoOrigemId == cliente.Id).Id);
        caso.AvaliarFluxo(fluxo).Valido.Should().BeFalse();

        caso.AdicionarAresta(cliente.Id, portal.Id, TipoDeAresta.Assincrona, 1, "Restaurada");

        caso.AvaliarFluxo(fluxo).Valido.Should().BeTrue();
    }

    [Fact]
    public void AtualizarFluxo_DeveSubstituirNomeENos_ComAsMesmasValidacoesDaCriacao()
    {
        var (caso, cliente, portal, validacao, _) = NovoMapa();
        var fluxo = caso.AdicionarFluxo("Entrada", [cliente.Id, portal.Id]);

        caso.AtualizarFluxo(fluxo.Id, "Entrada e validação", [cliente.Id, portal.Id, validacao.Id]);

        fluxo.Nome.Should().Be("Entrada e validação");
        fluxo.NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id);
        fluxo.Passos.Select(p => p.Posicao).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void AtualizarFluxo_NaoDeveAlterarOFluxo_QuandoAAtualizacaoForInvalida()
    {
        var (caso, cliente, portal, _, repositorio) = NovoMapa();
        var fluxo = caso.AdicionarFluxo("Entrada", [cliente.Id, portal.Id]);

        var semAresta = () => caso.AtualizarFluxo(fluxo.Id, "Novo nome", [cliente.Id, repositorio.Id]);
        var semNome = () => caso.AtualizarFluxo(fluxo.Id, " ", [cliente.Id, portal.Id]);

        semAresta.Should().Throw<FluxoInvalidoException>();
        semNome.Should().Throw<NomeInvalidoException>();
        fluxo.Nome.Should().Be("Entrada");
        fluxo.NosIds.Should().Equal(cliente.Id, portal.Id);
    }

    [Fact]
    public void RemoverFluxo_DeveRemoverSoOFluxoIndicado()
    {
        var (caso, cliente, portal, _, _) = NovoMapa();
        var primeiro = caso.AdicionarFluxo("Primeiro", [cliente.Id, portal.Id]);
        var segundo = caso.AdicionarFluxo("Segundo", [cliente.Id, portal.Id]);

        caso.RemoverFluxo(primeiro.Id);

        caso.Fluxos.Should().ContainSingle().Which.Should().Be(segundo);
    }

    [Fact]
    public void AtualizarFluxoERemoverFluxo_DevemLancarRecursoNaoEncontradoException_QuandoOFluxoNaoExiste()
    {
        var (caso, cliente, portal, _, _) = NovoMapa();

        var atualizar = () => caso.AtualizarFluxo(Guid.NewGuid(), "x", [cliente.Id, portal.Id]);
        var remover = () => caso.RemoverFluxo(Guid.NewGuid());

        atualizar.Should().Throw<RecursoNaoEncontradoException>();
        remover.Should().Throw<RecursoNaoEncontradoException>();
    }

    [Fact]
    public void Fluxos_DeveListarEmOrdemDeCadastro_MesmoAposRemoverUmDoMeio()
    {
        var (caso, cliente, portal, _, _) = NovoMapa();
        var f1 = caso.AdicionarFluxo("F1", [cliente.Id, portal.Id]);
        var f2 = caso.AdicionarFluxo("F2", [cliente.Id, portal.Id]);
        var f3 = caso.AdicionarFluxo("F3", [cliente.Id, portal.Id]);

        caso.RemoverFluxo(f2.Id);
        var f4 = caso.AdicionarFluxo("F4", [cliente.Id, portal.Id]);

        caso.Fluxos.Select(f => f.Nome).Should().Equal(f1.Nome, f3.Nome, f4.Nome);
        f4.Ordem.Should().BeGreaterThan(f3.Ordem);
    }
}
