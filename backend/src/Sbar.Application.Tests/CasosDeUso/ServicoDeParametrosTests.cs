using FluentAssertions;
using Moq;
using Sbar.Application.CasosDeUso;
using Sbar.Application.Parametros;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.ValueObjects;
using Xunit;

namespace Sbar.Application.Tests.CasosDeUso;

// RF11/US06 — versões de parâmetros: v1 sob demanda, nova versão a cada alteração e bloqueio da soma dos pesos.
// Repositórios em memória (Moq); persistência do snapshot, numeração concorrente e rotas ficam em ParametrosVersionadosApiTests.
public class ServicoDeParametrosTests
{
    private readonly List<ConfiguracaoDeEixo> _configuracoes =
        Enum.GetValues<Eixo>().Select(e => ConfiguracaoDeEixo.Criar(e, 0, 1, 0.1)).ToList();

    private readonly List<PerfilArquitetural> _perfis =
    [
        PerfilArquitetural.Criar(new DadosDoPerfilArquitetural("Monólito Modular (provisório)", 2, 1.5, 1, 2.5, 3, 1.5, 1.5, 1, 3, 3)),
        PerfilArquitetural.Criar(new DadosDoPerfilArquitetural("Microsserviços (provisório)", 4, 4, 3.5, 2.5, 4, 4, 2.5, 4.5, 2, 4)),
    ];

    private readonly List<VersaoDeParametros> _versoes = [];
    private readonly Mock<IUnidadeDeTrabalho> _unidadeDeTrabalho = new();
    private readonly ServicoDeParametros _servico;

    public ServicoDeParametrosTests()
    {
        var configuracoes = new Mock<IConfiguracaoDeEixoRepository>();
        configuracoes.Setup(r => r.ListarAsync()).ReturnsAsync(() => _configuracoes);
        configuracoes.Setup(r => r.SalvarAsync()).Returns(Task.CompletedTask);

        var perfis = new Mock<IPerfilArquiteturalRepository>();
        perfis.Setup(r => r.ListarAsync()).ReturnsAsync(() => _perfis);

        var versoes = new Mock<IVersaoDeParametrosRepository>();
        versoes.Setup(r => r.ObterUltimaAsync()).ReturnsAsync(() => _versoes.OrderByDescending(v => v.Numero).FirstOrDefault());
        versoes.Setup(r => r.ObterPorNumeroAsync(It.IsAny<int>())).ReturnsAsync((int n) => _versoes.FirstOrDefault(v => v.Numero == n));
        versoes.Setup(r => r.ListarAsync()).ReturnsAsync(() => _versoes.OrderBy(v => v.Numero).ToList());
        versoes.Setup(r => r.AdicionarAsync(It.IsAny<VersaoDeParametros>())).Callback<VersaoDeParametros>(_versoes.Add).Returns(Task.CompletedTask);
        versoes.Setup(r => r.SalvarAsync()).Returns(Task.CompletedTask);

        _unidadeDeTrabalho
            .Setup(u => u.ExecutarComBloqueioAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns((string _, Func<Task> operacao) => operacao());

        var usuarioAtual = new Mock<IUsuarioAtual>();
        usuarioAtual.Setup(u => u.DonoId).Returns(Guid.NewGuid());

        _servico = new ServicoDeParametros(configuracoes.Object, perfis.Object, versoes.Object, _unidadeDeTrabalho.Object, usuarioAtual.Object);
    }

    private static List<ParametroDeEixo> EixosComCoMaximo(double maximo) =>
        Enum.GetValues<Eixo>().Select(e => new ParametroDeEixo(e, e == Eixo.CO ? 6 : 0, e == Eixo.CO ? maximo : 1, 0.1)).ToList();

    [Fact]
    public async Task ObterAtualAsync_DeveCriarAVersao1SobDemanda_QuandoNaoHaNenhumaVersao()
    {
        var atual = await _servico.ObterAtualAsync();

        atual.Versao.Numero.Should().Be(1);
        atual.Versao.Descricao.Should().Be(ServicoDeParametros.DescricaoDaVersaoInicial);
        atual.Eixos.Should().HaveCount(10);
        atual.Assinaturas.Select(a => a.Nome).Should().BeEquivalentTo("Monólito Modular (provisório)", "Microsserviços (provisório)");
        _versoes.Should().ContainSingle();
    }

    [Fact]
    public async Task ObterAtualAsync_NaoDeveCriarOutraVersao_QuandoAVersao1JaExiste()
    {
        await _servico.ObterAtualAsync();
        await _servico.ObterAtualAsync();
        await _servico.ListarVersoesAsync();

        _versoes.Should().ContainSingle();
    }

    [Fact]
    public async Task AtualizarLimitesEPesosAsync_DeveAtualizarAsConfiguracoesERegistrarNovaVersao()
    {
        var atualizado = await _servico.AtualizarLimitesEPesosAsync("  Recalibra CO  ", EixosComCoMaximo(45));

        atualizado.Versao.Numero.Should().Be(2);
        atualizado.Versao.Descricao.Should().Be("Recalibra CO");
        _configuracoes.Single(c => c.Eixo == Eixo.CO).LimiteMaximo.Should().Be(45);
        _versoes.Select(v => v.Numero).Should().Equal(1, 2); // a v1 (estado anterior) nasce antes da alteração
    }

    [Fact]
    public async Task AtualizarLimitesEPesosAsync_DeveUsarDescricaoPadrao_QuandoNaoInformaDescricao()
    {
        var atualizado = await _servico.AtualizarLimitesEPesosAsync("   ", EixosComCoMaximo(45));

        atualizado.Versao.Descricao.Should().Be("Limites e pesos atualizados");
    }

    // CT10 — soma dos pesos diferente de 1: nada é gravado.
    [Fact]
    [Trait("Requisito", "CT10")]
    [Trait("Requisito", "RN07")]
    public async Task AtualizarLimitesEPesosAsync_NaoDeveGravarNada_QuandoASomaDosPesosNaoEUm()
    {
        var eixos = EixosComCoMaximo(40).Select(e => e.Eixo == Eixo.CO ? e with { Peso = 0.3 } : e).ToList();

        var acao = () => _servico.AtualizarLimitesEPesosAsync("inválido", eixos);

        await acao.Should().ThrowAsync<ParametrosInvalidosException>().WithMessage("Soma dos pesos igual a 1,2; deve ser 1.");
        _versoes.Should().BeEmpty();
        _configuracoes.Single(c => c.Eixo == Eixo.CO).Peso.Should().Be(0.1);
    }

    [Fact]
    public async Task AtualizarLimitesEPesosAsync_DeveLancarNomeInvalidoException_QuandoADescricaoPassaDoMaximo()
    {
        var acao = () => _servico.AtualizarLimitesEPesosAsync(new string('x', 201), EixosComCoMaximo(45));

        await acao.Should().ThrowAsync<NomeInvalidoException>();
        _versoes.Should().BeEmpty();
    }

    [Fact]
    public async Task RegistrarVersaoAsync_DeveGuardarOEstadoVigenteNoMomentoDoRegistro()
    {
        await _servico.GarantirVersaoInicialAsync();
        _perfis[0].Atualizar(new DadosDoPerfilArquitetural("Monólito Modular (provisório)", 5, 1.5, 1, 2.5, 3, 1.5, 1.5, 1, 3, 3));

        await _servico.RegistrarVersaoAsync("CO do monólito 2 → 5");

        (await _servico.ObterVersaoAsync(1)).Assinaturas.Single(a => a.Nome.StartsWith("Monólito")).Valores[Eixo.CO].Should().Be(2);
        (await _servico.ObterVersaoAsync(2)).Assinaturas.Single(a => a.Nome.StartsWith("Monólito")).Valores[Eixo.CO].Should().Be(5);
    }
}

public class SnapshotDeParametrosTests
{
    private static readonly IReadOnlyList<ParametroDeEixo> Eixos =
        Enum.GetValues<Eixo>().Select(e => new ParametroDeEixo(e, 0, 1, 0.1)).ToList();

    private static AssinaturaDeReferencia Assinatura(string nome, Guid id) =>
        new(id, nome, Enum.GetValues<Eixo>().ToDictionary(e => e, e => (double)(int)e / 2));

    [Fact]
    public void Serializar_DeveSerRecuperavelPorDesserializar_SemPerdaDeValores()
    {
        var id = Guid.NewGuid();
        var json = SnapshotDeParametros.Serializar(Eixos, [Assinatura("Monólito", id)]);

        var (eixos, assinaturas) = SnapshotDeParametros.Desserializar(json);

        eixos.Should().Equal(Eixos);
        assinaturas.Should().ContainSingle();
        assinaturas[0].Id.Should().Be(id);
        assinaturas[0].Nome.Should().Be("Monólito");
        assinaturas[0].Valores.Should().BeEquivalentTo(Assinatura("Monólito", id).Valores);
    }

}
