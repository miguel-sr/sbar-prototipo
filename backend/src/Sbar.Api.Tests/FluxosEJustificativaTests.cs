using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Infrastructure;

namespace Sbar.Api.Tests;

// Ponta a ponta (rotas, EF Core, Postgres real) das sub-etapas 9.3 e 9.4 da Etapa 9: fluxos principais como
// entrada com verificação de caminho (RF05, CT09, US03) e justificativa obrigatória da aresta (RN03, CT06, US02).
[Collection("Postgres")]
public class FluxosEJustificativaTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public FluxosEJustificativaTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    private async Task<T> LerAsync<T>(HttpResponseMessage resposta)
    {
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<T>(JsonTestOptions.Padrao))!;
    }

    private async Task<string> MensagemDeErroAsync(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<Dictionary<string, object>>(JsonTestOptions.Padrao))!["title"].ToString()!;

    // Cliente → Portal → Validação → Repositório (arestas com justificativa), sem aresta de volta.
    private async Task<(Guid CasoId, NoDto Cliente, NoDto Portal, NoDto Validacao, NoDto Repositorio, ArestaDto PortalParaValidacao)> CriarMapaAsync()
    {
        var caso = await LerAsync<EstudoDeCasoDto>(await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Pedidos Digital", null), JsonTestOptions.Padrao));

        async Task<NoDto> No(string nome, TipoDeNo tipo) =>
            await LerAsync<NoDto>(await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest(nome, tipo), JsonTestOptions.Padrao));

        async Task<ArestaDto> Aresta(NoDto origem, NoDto destino) =>
            await LerAsync<ArestaDto>(await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/arestas",
                new CriarArestaRequest(origem.Id, destino.Id, TipoDeAresta.Sincrona, 3, $"{origem.Nome} aciona {destino.Nome}"), JsonTestOptions.Padrao));

        var cliente = await No("Cliente", TipoDeNo.Ator);
        var portal = await No("Portal", TipoDeNo.Touchpoint);
        var validacao = await No("Validação do pedido", TipoDeNo.Processo);
        var repositorio = await No("Repositório de pedidos", TipoDeNo.Dados);

        await Aresta(cliente, portal);
        var portalParaValidacao = await Aresta(portal, validacao);
        await Aresta(validacao, repositorio);

        return (caso.Id, cliente, portal, validacao, repositorio, portalParaValidacao);
    }

    private Task<HttpResponseMessage> CriarFluxoAsync(Guid casoId, string nome, params NoDto[] nos) =>
        _client.PostAsJsonAsync($"/api/casos/{casoId}/fluxos", new CriarFluxoRequest(nome, nos.Select(n => n.Id).ToList()), JsonTestOptions.Padrao);

    // ----- Fluxos principais (9.3) -----

    [Fact]
    public async Task CriarFluxo_DeveRetornarCreatedComFluxoValido_ListarEmOrdemDeCadastro()
    {
        var (casoId, cliente, portal, validacao, repositorio, _) = await CriarMapaAsync();

        var resposta = await CriarFluxoAsync(casoId, "Pedido", cliente, portal, validacao, repositorio);
        await CriarFluxoAsync(casoId, "Entrada", cliente, portal);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var criado = await LerAsync<FluxoDto>(resposta);
        criado.Valido.Should().BeTrue();
        criado.EtapaAusente.Should().BeNull();
        criado.BandasPercorridas.Should().Be(4);
        criado.NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id, repositorio.Id);

        var lista = (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!;
        lista.Select(f => f.Nome).Should().Equal("Pedido", "Entrada");
        lista.Should().OnlyContain(f => f.Valido);
    }

    // CT09 — etapa sem aresta: 422, com a etapa e os nomes na mensagem
    [Fact]
    public async Task CriarFluxo_DeveRetornarUnprocessableEntityIndicandoAEtapa_QuandoFaltaAresta()
    {
        var (casoId, cliente, portal, _, repositorio, _) = await CriarMapaAsync();

        var resposta = await CriarFluxoAsync(casoId, "Atalho", cliente, portal, repositorio);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var mensagem = await MensagemDeErroAsync(resposta);
        mensagem.Should().Contain("etapa 2").And.Contain("Portal").And.Contain("Repositório de pedidos");
        (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!.Should().BeEmpty();
    }

    [Fact]
    public async Task CriarFluxo_DeveRetornarUnprocessableEntity_QuandoNomeVazioPoucosNosOuNoDeOutroCaso()
    {
        var (casoId, cliente, portal, _, _, _) = await CriarMapaAsync();
        var (_, clienteDeOutroCaso, _, _, _, _) = await CriarMapaAsync();

        (await CriarFluxoAsync(casoId, "  ", cliente, portal)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CriarFluxoAsync(casoId, "Um nó só", cliente)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CriarFluxoAsync(casoId, "Sem nós")).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CriarFluxoAsync(casoId, "Nó de fora", cliente, clienteDeOutroCaso)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CriarFluxoAsync(casoId, new string('x', 101), cliente, portal)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Fluxos_DeveRetornarNotFound_QuandoOEstudoDeCasoOuOFluxoNaoExistem()
    {
        var (casoId, cliente, portal, _, _, _) = await CriarMapaAsync();

        (await _client.GetAsync($"/api/casos/{Guid.NewGuid()}/fluxos")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CriarFluxoAsync(Guid.NewGuid(), "x", cliente, portal)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PutAsJsonAsync($"/api/casos/{casoId}/fluxos/{Guid.NewGuid()}", new AtualizarFluxoRequest("x", [cliente.Id, portal.Id]), JsonTestOptions.Padrao))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync($"/api/casos/{casoId}/fluxos/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AtualizarFluxo_DeveSubstituirNomeENos_E_RejeitarAtualizacaoInvalidaSemAlterarOFluxo()
    {
        var (casoId, cliente, portal, validacao, repositorio, _) = await CriarMapaAsync();
        var fluxo = await LerAsync<FluxoDto>(await CriarFluxoAsync(casoId, "Entrada", cliente, portal));

        var ok = await _client.PutAsJsonAsync($"/api/casos/{casoId}/fluxos/{fluxo.Id}",
            new AtualizarFluxoRequest("Até a validação", [cliente.Id, portal.Id, validacao.Id]), JsonTestOptions.Padrao);
        var invalido = await _client.PutAsJsonAsync($"/api/casos/{casoId}/fluxos/{fluxo.Id}",
            new AtualizarFluxoRequest("Atalho", [cliente.Id, repositorio.Id]), JsonTestOptions.Padrao);

        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LerAsync<FluxoDto>(ok)).NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id);
        invalido.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var lista = (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!;
        lista.Should().ContainSingle();
        lista[0].Nome.Should().Be("Até a validação");
        lista[0].NosIds.Should().Equal(cliente.Id, portal.Id, validacao.Id);
    }

    [Fact]
    public async Task ExcluirFluxo_DeveRetornarNoContent()
    {
        var (casoId, cliente, portal, _, _, _) = await CriarMapaAsync();
        var fluxo = await LerAsync<FluxoDto>(await CriarFluxoAsync(casoId, "Entrada", cliente, portal));

        var resposta = await _client.DeleteAsync($"/api/casos/{casoId}/fluxos/{fluxo.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!.Should().BeEmpty();
    }

    // US03 — excluir Aresta ou Nó nunca apaga o fluxo: ele fica inválido, com a etapa ausente indicada, e sai do DEP.
    [Fact]
    public async Task ExcluirAresta_DeveTornarOFluxoInvalidoSemApagaLo_E_TiraloDoDep()
    {
        var (casoId, cliente, portal, validacao, repositorio, portalParaValidacao) = await CriarMapaAsync();
        var fluxo = await LerAsync<FluxoDto>(await CriarFluxoAsync(casoId, "Pedido", cliente, portal, validacao, repositorio));
        var antes = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao);
        antes!.Eixos.Single(e => e.Eixo == Eixo.DEP).Aplicavel.Should().BeTrue();
        antes.Eixos.Single(e => e.Eixo == Eixo.DEP).ValorBruto.Should().BeApproximately(4 / 5.0, 1e-9);

        (await _client.DeleteAsync($"/api/casos/{casoId}/arestas/{portalParaValidacao.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!;
        lista.Should().ContainSingle();
        lista[0].Id.Should().Be(fluxo.Id);
        lista[0].NosIds.Should().Equal(fluxo.NosIds);
        lista[0].Valido.Should().BeFalse();
        lista[0].EtapaAusente!.Posicao.Should().Be(2);
        (lista[0].EtapaAusente!.OrigemId, lista[0].EtapaAusente!.DestinoId).Should().Be((portal.Id, validacao.Id));

        var depois = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao);
        depois!.Eixos.Single(e => e.Eixo == Eixo.DEP).Aplicavel.Should().BeFalse();
        depois.Fluxos.Should().ContainSingle().Which.Valido.Should().BeFalse();
        depois.Avisos.Should().Contain(a => a.Contains("inválido"));
    }

    [Fact]
    public async Task ExcluirNo_DeveTornarOFluxoInvalidoSemApagaLo()
    {
        var (casoId, cliente, portal, validacao, repositorio, _) = await CriarMapaAsync();
        await CriarFluxoAsync(casoId, "Pedido", cliente, portal, validacao, repositorio);

        (await _client.DeleteAsync($"/api/casos/{casoId}/nos/{validacao.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = (await _client.GetFromJsonAsync<List<FluxoDto>>($"/api/casos/{casoId}/fluxos", JsonTestOptions.Padrao))!;
        lista.Should().ContainSingle();
        lista[0].NosIds.Should().HaveCount(4);
        lista[0].Valido.Should().BeFalse();
        lista[0].EtapaAusente!.Motivo.Should().Contain("removido");
    }

    [Fact]
    public async Task ExcluirOEstudoDeCaso_DeveApagarOsFluxosJuntoComOMapa()
    {
        var (casoId, cliente, portal, _, _, _) = await CriarMapaAsync();
        await CriarFluxoAsync(casoId, "Entrada", cliente, portal);

        (await _client.DeleteAsync($"/api/casos/{casoId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetAsync($"/api/casos/{casoId}/fluxos")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- Justificativa da aresta (9.4) -----

    [Fact]
    public async Task CriarAresta_DeveDevolverAJustificativaAparada()
    {
        var (casoId, cliente, _, _, repositorio, _) = await CriarMapaAsync();

        var resposta = await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(cliente.Id, repositorio.Id, TipoDeAresta.Assincrona, 1, "   Auditoria direta   "), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        (await LerAsync<ArestaDto>(resposta)).Justificativa.Should().Be("Auditoria direta");
        var lista = (await _client.GetFromJsonAsync<List<ArestaDto>>($"/api/casos/{casoId}/arestas", JsonTestOptions.Padrao))!;
        lista.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.Justificativa));
    }

    // CT06 — aresta sem justificativa é rejeitada
    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task CriarAresta_DeveRetornarUnprocessableEntity_QuandoAJustificativaForVazia(string justificativa)
    {
        var (casoId, cliente, _, _, repositorio, _) = await CriarMapaAsync();

        var resposta = await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(cliente.Id, repositorio.Id, TipoDeAresta.Sincrona, 2, justificativa), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(resposta)).Should().Contain("Justificativa");
    }

    [Fact]
    public async Task CriarAresta_DeveRetornarUnprocessableEntity_QuandoAJustificativaPassarDe500Caracteres()
    {
        var (casoId, cliente, _, _, repositorio, _) = await CriarMapaAsync();

        var resposta = await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(cliente.Id, repositorio.Id, TipoDeAresta.Sincrona, 2, new string('x', 501)), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CriarAresta_DeveRetornarBadRequest_QuandoOCorpoNaoTrazAJustificativa()
    {
        var (casoId, cliente, _, _, repositorio, _) = await CriarMapaAsync();

        // corpo da API antiga (sem "justificativa"): o contrato mudou, então o campo ausente é rejeitado
        var antigo = new { noOrigemId = cliente.Id, noDestinoId = repositorio.Id, tipo = "Sincrona", peso = 2 };
        var resposta = await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas", antigo);

        resposta.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AtualizarAresta_DeveTrocarAJustificativa_ERejeitarVazia()
    {
        var (casoId, _, portal, validacao, _, portalParaValidacao) = await CriarMapaAsync();

        var ok = await _client.PutAsJsonAsync($"/api/casos/{casoId}/arestas/{portalParaValidacao.Id}",
            new AtualizarArestaRequest(portal.Id, validacao.Id, TipoDeAresta.Sincrona, 3, "  Nova justificativa "), JsonTestOptions.Padrao);
        var vazia = await _client.PutAsJsonAsync($"/api/casos/{casoId}/arestas/{portalParaValidacao.Id}",
            new AtualizarArestaRequest(portal.Id, validacao.Id, TipoDeAresta.Sincrona, 3, ""), JsonTestOptions.Padrao);

        (await LerAsync<ArestaDto>(ok)).Justificativa.Should().Be("Nova justificativa");
        vazia.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var atual = (await _client.GetFromJsonAsync<List<ArestaDto>>($"/api/casos/{casoId}/arestas", JsonTestOptions.Padrao))!.Single(a => a.Id == portalParaValidacao.Id);
        atual.Justificativa.Should().Be("Nova justificativa");
    }

    // CT05 — intensidade fora de {1, 2, 3} continua rejeitada
    [Fact]
    public async Task CriarAresta_DeveRetornarUnprocessableEntity_QuandoOPesoForQuatro()
    {
        var (casoId, cliente, _, _, repositorio, _) = await CriarMapaAsync();

        var resposta = await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(cliente.Id, repositorio.Id, TipoDeAresta.Sincrona, 4, "Justificativa válida"), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // D11 (opção 1) — a migration preenche as arestas já gravadas com o texto-padrão "pendente", para a interface
    // marcá-las para revisão; as migrations anteriores ficam intactas.
    [Fact]
    public async Task Migration_DevePreencherAsArestasExistentesComATextoDePendencia()
    {
        var nomeDoBanco = $"sbar_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(_postgres.ConnectionStringAdmin))
        {
            await admin.OpenAsync();
            await using var criar = new NpgsqlCommand($"""CREATE DATABASE "{nomeDoBanco}" """, admin);
            await criar.ExecuteNonQueryAsync();
        }

        try
        {
            var conexao = new NpgsqlConnectionStringBuilder(_postgres.ConnectionStringAdmin) { Database = nomeDoBanco }.ConnectionString;
            var opcoes = new DbContextOptionsBuilder<SbarDbContext>().UseNpgsql(conexao).Options;
            await using var contexto = new SbarDbContext(opcoes);
            var migrator = contexto.GetService<IMigrator>();

            // 1) banco no estado ANTERIOR à justificativa, com uma aresta gravada como o código antigo gravava
            await migrator.MigrateAsync("AssinaturasDoQuadro8");
            var (caso, a, b, aresta) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await contexto.Database.ExecuteSqlRawAsync($"""
                INSERT INTO "EstudosDeCaso" ("Id", "Nome", "DataCriacao") VALUES ('{caso}', 'Antigo', now());
                INSERT INTO "Nos" ("Id", "Nome", "Tipo", "Banda", "EstudoDeCasoId", "Ordem") VALUES
                  ('{a}', 'Cliente', 'Ator', 'Ator', '{caso}', 1), ('{b}', 'Portal', 'Touchpoint', 'Frontstage', '{caso}', 2);
                INSERT INTO "Arestas" ("Id", "NoOrigemId", "NoDestinoId", "Tipo", "Peso", "EstudoDeCasoId", "Ordem")
                  VALUES ('{aresta}', '{a}', '{b}', 'Sincrona', 3, '{caso}', 1);
                """);

            // 2) migra até a mais recente
            await migrator.MigrateAsync();

            var justificativa = await contexto.Database
                .SqlQueryRaw<string>("""SELECT "Justificativa" AS "Value" FROM "Arestas" """)
                .SingleAsync();
            justificativa.Should().Be(Aresta.JustificativaPendente);
        }
        finally
        {
            await using var admin = new NpgsqlConnection(_postgres.ConnectionStringAdmin);
            await admin.OpenAsync();
            await using var apagar = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{nomeDoBanco}" WITH (FORCE)""", admin);
            await apagar.ExecuteNonQueryAsync();
        }
    }
}
