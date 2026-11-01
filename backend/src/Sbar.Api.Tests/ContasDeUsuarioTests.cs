using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Contas, sessão e isolamento entre usuários (contas de usuário) pela API real.
// Fica na camada da Api porque o que se prova é a combinação cookie + política padrão + filtro do DbContext + banco.
[Collection("Postgres")]
public class ContasDeUsuarioTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;

    public ContasDeUsuarioTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    public static IEnumerable<object[]> RotasProtegidas =>
    [
        ["GET", "/api/casos"],
        ["POST", "/api/casos"],
        ["GET", "/api/parametros"],
        ["GET", "/api/perfis-arquiteturais"],
        ["GET", "/api/casos/" + Guid.NewGuid() + "/metricas"],
        ["GET", "/api/casos/" + Guid.NewGuid() + "/exportacao"],
        ["GET", "/api/account/me"],
    ];

    [Theory]
    [MemberData(nameof(RotasProtegidas))]
    public async Task SemSessao_RotasProtegidas_Devolvem401(string metodo, string rota)
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var resposta = await anonimo.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), rota));

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cadastro_ComConviteInvalido_Devolve403()
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var resposta = await anonimo.PostAsJsonAsync("/api/account/register",
            new CadastrarRequest("novo@sbar.test", ApiTestFactory.SenhaDeTeste, "errado"));

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cadastro_ComEmailJaUsado_Devolve409()
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var resposta = await anonimo.PostAsJsonAsync("/api/account/register",
            new CadastrarRequest(ApiTestFactory.EmailPadrao, ApiTestFactory.SenhaDeTeste, ApiTestFactory.CodigoDeConvite));

        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cadastro_ComSenhaFraca_Devolve422()
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var resposta = await anonimo.PostAsJsonAsync("/api/account/register",
            new CadastrarRequest("fraca@sbar.test", "abc", ApiTestFactory.CodigoDeConvite));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Devolve401_ECertoIniciaASessao()
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var errada = await anonimo.PostAsJsonAsync("/api/account/login", new EntrarRequest(ApiTestFactory.EmailPadrao, "Errada1234"));
        var certa = await anonimo.PostAsJsonAsync("/api/account/login", new EntrarRequest(ApiTestFactory.EmailPadrao, ApiTestFactory.SenhaDeTeste));
        var eu = await anonimo.GetFromJsonAsync<UsuarioDto>("/api/account/me");

        errada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        certa.StatusCode.Should().Be(HttpStatusCode.OK);
        eu!.Email.Should().Be(ApiTestFactory.EmailPadrao);
    }

    [Fact]
    public async Task Logout_EncerraASessao()
    {
        var saida = await _api.Client.PostAsync("/api/account/logout", null);
        var eu = await _api.Client.GetAsync("/api/account/me");

        saida.StatusCode.Should().Be(HttpStatusCode.NoContent);
        eu.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CookieDeSessao_SaiHttpOnly()
    {
        using var anonimo = _api.CriarClienteAnonimo();

        var resposta = await anonimo.PostAsJsonAsync("/api/account/login", new EntrarRequest(ApiTestFactory.EmailPadrao, ApiTestFactory.SenhaDeTeste));

        resposta.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("sbar.sessao=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OutroUsuario_NaoVeNemAcessaOCasoDeOutro_EmNenhumaRota()
    {
        var (casoDeA, nosDeA) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_api.Client);
        using var usuarioB = await _api.CriarClienteAutenticadoAsync("b@sbar.test");

        var listagem = await usuarioB.GetFromJsonAsync<List<EstudoDeCasoDto>>("/api/casos", JsonTestOptions.Padrao);
        listagem.Should().BeEmpty();

        string[] leituras =
        [
            $"/api/casos/{casoDeA}", $"/api/casos/{casoDeA}/nos", $"/api/casos/{casoDeA}/arestas", $"/api/casos/{casoDeA}/fluxos",
            $"/api/casos/{casoDeA}/metricas", $"/api/casos/{casoDeA}/matriz-rastreabilidade",
            $"/api/casos/{casoDeA}/exportacao?formato=json",
        ];
        foreach (var rota in leituras)
            (await usuarioB.GetAsync(rota)).StatusCode.Should().Be(HttpStatusCode.NotFound, rota);

        (await usuarioB.PutAsJsonAsync($"/api/casos/{casoDeA}", new AtualizarEstudoDeCasoRequest("Invadido", null))).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await usuarioB.PostAsJsonAsync($"/api/casos/{casoDeA}/nos", new CriarNoRequest("Intruso", Sbar.Domain.Enums.TipoDeNo.Ator), JsonTestOptions.Padrao)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await usuarioB.DeleteAsync($"/api/casos/{casoDeA}/nos/{nosDeA["A"]}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await usuarioB.DeleteAsync($"/api/casos/{casoDeA}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // O dono segue vendo o caso intacto.
        var doDono = await _api.Client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{casoDeA}", JsonTestOptions.Padrao);
        doDono!.Nos.Should().HaveCount(15);
    }

    [Fact]
    public async Task ParametrosEPerfis_SaoPorUsuario_EditarOsDeUmNaoAlteraOsDoOutro()
    {
        using var usuarioB = await _api.CriarClienteAutenticadoAsync("b@sbar.test");
        var eixosComCoMaior = Enum.GetValues<Eixo>().Select(e =>
            new ParametroDeEixoRequest(e, e == Eixo.CO ? 6 : e == Eixo.UX ? 0 : e == Eixo.VOL ? 1 : e == Eixo.DEP ? 0.2 : 0, e == Eixo.CO ? 45 : e == Eixo.UX ? 3 : e == Eixo.VOL ? 3 : 1, 0.1)).ToList();

        (await _api.Client.PutAsJsonAsync("/api/parametros/limites-e-pesos",
            new AtualizarLimitesEPesosRequest("Só do usuário A", eixosComCoMaior), JsonTestOptions.Padrao)).EnsureSuccessStatusCode();
        var perfilDeA = (await _api.Client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!.First();
        (await _api.Client.DeleteAsync($"/api/perfis-arquiteturais/{perfilDeA.Id}")).EnsureSuccessStatusCode();

        var deA = await _api.Client.GetFromJsonAsync<ConjuntoDeParametrosDto>("/api/parametros/atual", JsonTestOptions.Padrao);
        var deB = await usuarioB.GetFromJsonAsync<ConjuntoDeParametrosDto>("/api/parametros/atual", JsonTestOptions.Padrao);

        deA!.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(45);
        deA.Assinaturas.Should().HaveCount(1);
        deB!.Versao.Numero.Should().Be(1);
        deB.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(40); // Quadro 7, intacto
        deB.Assinaturas.Should().HaveCount(2);
        (await usuarioB.GetAsync($"/api/perfis-arquiteturais/{perfilDeA.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound, "o Id do perfil de A não existe para B, que tem a sua própria cópia");
    }

    [Fact]
    public async Task Cadastro_DoEmailDoLegado_LevaTambemOsParametrosESuasVersoes()
    {
        // Os parâmetros globais de antes das contas (semeados pela InitialCreate e preservados) existem sem dono; aqui o
        // legado foi editado (CO máximo 99) para provar que quem o reivindica fica com a versão editada.
        await using (var conexao = new Npgsql.NpgsqlConnection(new Npgsql.NpgsqlConnectionStringBuilder(_postgres.ConnectionStringAdmin)
        {
            Database = _api.NomeDoBanco
        }.ConnectionString))
        {
            await conexao.OpenAsync();
            await using var comando = new Npgsql.NpgsqlCommand(
                """UPDATE "ConfiguracoesDeEixo" SET "LimiteMaximo" = 99 WHERE "Eixo" = 'CO' AND "DonoId" IS NULL""", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        using var dono = await _api.CriarClienteAutenticadoAsync(ApiTestFactory.EmailDoDonoDoLegado);
        var atual = await dono.GetFromJsonAsync<ConjuntoDeParametrosDto>("/api/parametros/atual", JsonTestOptions.Padrao);
        var deOutro = await _api.Client.GetFromJsonAsync<ConjuntoDeParametrosDto>("/api/parametros/atual", JsonTestOptions.Padrao);

        atual!.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(99, "o dono do legado fica com os parâmetros que já existiam");
        deOutro!.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(40);
    }

    [Fact]
    public async Task CriarCaso_ComOLimiteAtingido_Devolve422()
    {
        for (var i = 0; i < EstudoDeCaso.LimiteDeCasosPorUsuario; i++)
            (await _api.Client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest($"Caso {i}", null), JsonTestOptions.Padrao))
                .EnsureSuccessStatusCode();

        var excedente = await _api.Client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Um a mais", null), JsonTestOptions.Padrao);

        excedente.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Cadastro_DoEmailDoLegado_ReivindicaOsCasosSemDono()
    {
        await using (var conexao = new Npgsql.NpgsqlConnection(new Npgsql.NpgsqlConnectionStringBuilder(_postgres.ConnectionStringAdmin)
        {
            Database = _api.NomeDoBanco
        }.ConnectionString))
        {
            await conexao.OpenAsync();
            await using var comando = new Npgsql.NpgsqlCommand(
                """INSERT INTO "EstudosDeCaso" ("Id", "Nome", "DataCriacao") VALUES (gen_random_uuid(), 'Caso antigo', now())""", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        var antes = await _api.Client.GetFromJsonAsync<List<EstudoDeCasoDto>>("/api/casos", JsonTestOptions.Padrao);
        using var dono = await _api.CriarClienteAutenticadoAsync(ApiTestFactory.EmailDoDonoDoLegado);
        var depois = await dono.GetFromJsonAsync<List<EstudoDeCasoDto>>("/api/casos", JsonTestOptions.Padrao);

        antes.Should().BeEmpty();
        depois.Should().ContainSingle(c => c.Nome == "Caso antigo");
    }
}
