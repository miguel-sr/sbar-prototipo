using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Ponta a ponta (rotas, EF Core, cascata) das correções da rodada de testes do front: listagem com
// Nós/Arestas, ordem estável, validação de nomes, PUT/DELETE e o nosGargalo do Estudo de Caso 2.
[Collection("Postgres")]
public class EdicaoEExclusaoTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public EdicaoEExclusaoTests(PostgresContainerFixture postgres) => _postgres = postgres;

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

    private async Task<EstudoDeCasoDto> CriarCasoAsync(string nome = "Pedidos Digital") =>
        await LerAsync<EstudoDeCasoDto>(await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest(nome, null), JsonTestOptions.Padrao));

    private async Task<NoDto> CriarNoAsync(Guid casoId, string nome, TipoDeNo tipo) =>
        await LerAsync<NoDto>(await _client.PostAsJsonAsync($"/api/casos/{casoId}/nos", new CriarNoRequest(nome, tipo), JsonTestOptions.Padrao));

    private async Task<ArestaDto> CriarArestaAsync(Guid casoId, Guid origem, Guid destino, TipoDeAresta tipo = TipoDeAresta.Sincrona, int peso = 3) =>
        await LerAsync<ArestaDto>(await _client.PostAsJsonAsync($"/api/casos/{casoId}/arestas", new CriarArestaRequest(origem, destino, tipo, peso, "Justificativa de teste"), JsonTestOptions.Padrao));

    // ----- Listagem -----

    [Fact]
    public async Task Listar_DeveTrazerNosEArestasDeCadaCaso()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "Cliente", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "Cardápio", TipoDeNo.Touchpoint);
        await CriarArestaAsync(caso.Id, a.Id, b.Id);
        await CriarCasoAsync("Outro caso");

        var lista = await _client.GetFromJsonAsync<List<EstudoDeCasoDto>>("/api/casos", JsonTestOptions.Padrao);

        var doCaso = lista!.Single(c => c.Id == caso.Id);
        doCaso.Nos.Should().HaveCount(2);
        doCaso.Arestas.Should().HaveCount(1);
        lista!.Single(c => c.Nome == "Outro caso").Nos.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_DeveDevolverNosEArestasNaOrdemDeCadastro()
    {
        var caso = await CriarCasoAsync();
        var nomes = Enumerable.Range(1, 12).Select(i => $"Nó {13 - i}").ToList(); // decrescente, para não coincidir com ordem alfabética
        var criados = new List<NoDto>();
        foreach (var nome in nomes)
            criados.Add(await CriarNoAsync(caso.Id, nome, TipoDeNo.Processo));
        var arestas = new List<ArestaDto>();
        for (var i = 0; i < criados.Count - 1; i++)
            arestas.Add(await CriarArestaAsync(caso.Id, criados[i].Id, criados[i + 1].Id));

        var detalhe = await _client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{caso.Id}", JsonTestOptions.Padrao);
        var nosDaRota = await _client.GetFromJsonAsync<List<NoDto>>($"/api/casos/{caso.Id}/nos", JsonTestOptions.Padrao);
        var arestasDaRota = await _client.GetFromJsonAsync<List<ArestaDto>>($"/api/casos/{caso.Id}/arestas", JsonTestOptions.Padrao);

        detalhe!.Nos.Select(n => n.Nome).Should().Equal(nomes);
        nosDaRota!.Select(n => n.Nome).Should().Equal(nomes);
        detalhe.Arestas.Select(a => a.Id).Should().Equal(arestas.Select(a => a.Id));
        arestasDaRota!.Select(a => a.Id).Should().Equal(arestas.Select(a => a.Id));
    }

    // ----- Validação (422) -----

    [Fact]
    public async Task Criar_DeveRetornar422_QuandoNomeDoCasoVazioOuAcimaDoLimite()
    {
        var vazio = await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("   ", null), JsonTestOptions.Padrao);
        var longo = await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest(new string('x', 121), null), JsonTestOptions.Padrao);
        var descricaoLonga = await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Ok", new string('d', 1001)), JsonTestOptions.Padrao);

        vazio.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        longo.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        descricaoLonga.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CriarNo_DeveRetornar422_QuandoNomeVazioOuAcimaDeCemCaracteres_ENaoPersistirNada()
    {
        var caso = await CriarCasoAsync();

        var vazio = await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest("", TipoDeNo.Processo), JsonTestOptions.Padrao);
        var espacos = await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest("   ", TipoDeNo.Processo), JsonTestOptions.Padrao);
        var longo = await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest(new string('x', 5000), TipoDeNo.Processo), JsonTestOptions.Padrao);

        vazio.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        espacos.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        longo.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await _client.GetFromJsonAsync<List<NoDto>>($"/api/casos/{caso.Id}/nos", JsonTestOptions.Padrao))!.Should().BeEmpty();
    }

    // ----- PUT / DELETE: Estudo de Caso -----

    [Fact]
    public async Task AtualizarCaso_DeveTrocarNomeEDescricao_EManterNosEArestas()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        await CriarArestaAsync(caso.Id, a.Id, b.Id);

        var resposta = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}", new AtualizarEstudoDeCasoRequest(" Novo nome ", "Nova descrição"), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await LerAsync<EstudoDeCasoDto>(resposta);
        atualizado.Nome.Should().Be("Novo nome");
        atualizado.DominioDescricao.Should().Be("Nova descrição");
        var releitura = await _client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{caso.Id}", JsonTestOptions.Padrao);
        releitura!.Nome.Should().Be("Novo nome");
        releitura.Nos.Should().HaveCount(2);
        releitura.Arestas.Should().HaveCount(1);
    }

    [Fact]
    public async Task AtualizarCaso_DeveRetornar422_QuandoNomeVazio_E404QuandoCasoNaoExiste()
    {
        var caso = await CriarCasoAsync();

        var invalido = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}", new AtualizarEstudoDeCasoRequest("", null), JsonTestOptions.Padrao);
        var inexistente = await _client.PutAsJsonAsync($"/api/casos/{Guid.NewGuid()}", new AtualizarEstudoDeCasoRequest("X", null), JsonTestOptions.Padrao);

        invalido.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExcluirCaso_DeveRemoverOCasoComNosEArestas_E404NaSegundaVez()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        await CriarArestaAsync(caso.Id, a.Id, b.Id);
        var outro = await CriarCasoAsync("Fica");

        var resposta = await _client.DeleteAsync($"/api/casos/{caso.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/casos/{caso.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.DeleteAsync($"/api/casos/{caso.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var lista = await _client.GetFromJsonAsync<List<EstudoDeCasoDto>>("/api/casos", JsonTestOptions.Padrao);
        lista!.Select(c => c.Id).Should().Equal(outro.Id);
    }

    // ----- PUT / DELETE: Nó -----

    [Fact]
    public async Task AtualizarNo_DeveRederivarABanda_E422ParaNomeVazio_E404ParaNoInexistente()
    {
        var caso = await CriarCasoAsync();
        var no = await CriarNoAsync(caso.Id, "Carrinho", TipoDeNo.Touchpoint);

        var ok = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/nos/{no.Id}", new AtualizarNoRequest("Fila de pedidos", TipoDeNo.Processo), JsonTestOptions.Padrao);
        var invalido = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/nos/{no.Id}", new AtualizarNoRequest("  ", TipoDeNo.Processo), JsonTestOptions.Padrao);
        var inexistente = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/nos/{Guid.NewGuid()}", new AtualizarNoRequest("X", TipoDeNo.Ator), JsonTestOptions.Padrao);

        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await LerAsync<NoDto>(ok);
        atualizado.Nome.Should().Be("Fila de pedidos");
        atualizado.Banda.Should().Be(Banda.Backstage);
        invalido.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var nos = await _client.GetFromJsonAsync<List<NoDto>>($"/api/casos/{caso.Id}/nos", JsonTestOptions.Padrao);
        nos!.Single().Nome.Should().Be("Fila de pedidos");
    }

    [Fact]
    public async Task ExcluirNo_DeveRemoverAsArestasLigadasAEle_EManterAsDemais()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        var c = await CriarNoAsync(caso.Id, "C", TipoDeNo.Processo);
        await CriarArestaAsync(caso.Id, a.Id, b.Id);
        await CriarArestaAsync(caso.Id, b.Id, c.Id);
        var fica = await CriarArestaAsync(caso.Id, a.Id, c.Id, TipoDeAresta.Batch, 1);

        var resposta = await _client.DeleteAsync($"/api/casos/{caso.Id}/nos/{b.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var releitura = await _client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{caso.Id}", JsonTestOptions.Padrao);
        releitura!.Nos.Select(n => n.Id).Should().Equal(a.Id, c.Id);
        releitura.Arestas.Should().ContainSingle().Which.Id.Should().Be(fica.Id);
        (await _client.DeleteAsync($"/api/casos/{caso.Id}/nos/{b.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- PUT / DELETE: Aresta -----

    [Fact]
    public async Task AtualizarAresta_DeveTrocarTipoEPeso_E422ParaSelfLoopEPesoInvalido_E404ParaInexistente()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        var aresta = await CriarArestaAsync(caso.Id, a.Id, b.Id);

        var ok = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/arestas/{aresta.Id}", new AtualizarArestaRequest(b.Id, a.Id, TipoDeAresta.Critica, 1, "Justificativa de teste"), JsonTestOptions.Padrao);
        var selfLoop = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/arestas/{aresta.Id}", new AtualizarArestaRequest(a.Id, a.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste"), JsonTestOptions.Padrao);
        var peso = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/arestas/{aresta.Id}", new AtualizarArestaRequest(a.Id, b.Id, TipoDeAresta.Sincrona, 4, "Justificativa de teste"), JsonTestOptions.Padrao);
        var inexistente = await _client.PutAsJsonAsync($"/api/casos/{caso.Id}/arestas/{Guid.NewGuid()}", new AtualizarArestaRequest(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste"), JsonTestOptions.Padrao);

        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizada = await LerAsync<ArestaDto>(ok);
        atualizada.Should().BeEquivalentTo(new ArestaDto(aresta.Id, b.Id, a.Id, TipoDeAresta.Critica, 1, "Justificativa de teste"));
        selfLoop.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        peso.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var arestas = await _client.GetFromJsonAsync<List<ArestaDto>>($"/api/casos/{caso.Id}/arestas", JsonTestOptions.Padrao);
        arestas!.Single().Tipo.Should().Be(TipoDeAresta.Critica);
    }

    [Fact]
    public async Task ExcluirAresta_DeveRemoverSoAAresta()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        var aresta = await CriarArestaAsync(caso.Id, a.Id, b.Id);

        var resposta = await _client.DeleteAsync($"/api/casos/{caso.Id}/arestas/{aresta.Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var releitura = await _client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{caso.Id}", JsonTestOptions.Padrao);
        releitura!.Arestas.Should().BeEmpty();
        releitura.Nos.Should().HaveCount(2);
        (await _client.DeleteAsync($"/api/casos/{caso.Id}/arestas/{aresta.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- nosGargalo e matriz (Estudo de Caso 2 — Quadros 31 e 36) -----

    private async Task<(Guid CasoId, Dictionary<string, Guid> Nos)> CriarEstudoDeCaso2Async()
    {
        var caso = await CriarCasoAsync("Sistema de Agendamento de Clínica");

        (string Chave, string Nome, TipoDeNo Tipo)[] nosSpec =
        [
            ("A", "Paciente", TipoDeNo.Ator),
            ("TP1", "Portal de Agendamento", TipoDeNo.Touchpoint),
            ("TP2", "Tela de Seleção de Horário", TipoDeNo.Touchpoint),
            ("TP3", "Tela de Confirmação", TipoDeNo.Touchpoint),
            ("TP4", "Canal de Notificação", TipoDeNo.Touchpoint),
            ("P1", "Verificação de Disponibilidade", TipoDeNo.Processo),
            ("P2", "Registro do Agendamento", TipoDeNo.Processo),
            ("P3", "Geração da Confirmação", TipoDeNo.Processo),
            ("P4", "Job de Lembrete", TipoDeNo.Processo),
            ("P5", "Check-in da Consulta", TipoDeNo.Processo),
            ("S1", "Autenticação/Cadastro", TipoDeNo.Suporte),
            ("S2", "Serviço de Notificações", TipoDeNo.Suporte),
            ("DS", "Repositório de Agendas", TipoDeNo.Dados),
            ("E1", "Gateway SMS/E-mail", TipoDeNo.Externo),
            ("E2", "Prontuário/Convênio", TipoDeNo.Externo),
        ];

        var nos = new Dictionary<string, Guid>();
        foreach (var (chave, nome, tipo) in nosSpec)
            nos[chave] = (await CriarNoAsync(caso.Id, nome, tipo)).Id;

        (string Origem, string Destino, TipoDeAresta Tipo, int Peso)[] arestasSpec =
        [
            ("A", "TP1", TipoDeAresta.Sincrona, 3), ("TP1", "S1", TipoDeAresta.Sincrona, 2),
            ("S1", "DS", TipoDeAresta.Sincrona, 2), ("TP1", "TP2", TipoDeAresta.Sincrona, 3),
            ("TP2", "P1", TipoDeAresta.Sincrona, 3), ("P1", "DS", TipoDeAresta.Sincrona, 3),
            ("TP2", "TP3", TipoDeAresta.Sincrona, 3), ("TP3", "P2", TipoDeAresta.Sincrona, 3),
            ("P2", "DS", TipoDeAresta.Sincrona, 3), ("P2", "P3", TipoDeAresta.Assincrona, 2),
            ("P3", "S2", TipoDeAresta.Assincrona, 2), ("S2", "E1", TipoDeAresta.Assincrona, 2),
            ("E1", "TP4", TipoDeAresta.Assincrona, 2), ("P2", "P4", TipoDeAresta.Batch, 1),
            ("P4", "S2", TipoDeAresta.Batch, 1), ("S2", "E1", TipoDeAresta.Batch, 1),
            ("E1", "TP4", TipoDeAresta.Batch, 1), ("TP4", "A", TipoDeAresta.Assincrona, 2),
            ("A", "P5", TipoDeAresta.Critica, 3), ("P5", "DS", TipoDeAresta.Sincrona, 2),
            ("P5", "E2", TipoDeAresta.Batch, 1),
        ];

        foreach (var (origem, destino, tipo, peso) in arestasSpec)
            await CriarArestaAsync(caso.Id, nos[origem], nos[destino], tipo, peso);

        return (caso.Id, nos);
    }

    [Fact]
    public async Task Metricas_DeveDevolverNosGargaloDeTodasAsBandas_QuandoEstudoDeCaso2()
    {
        var (casoId, nos) = await CriarEstudoDeCaso2Async();

        var metricas = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao);

        // Quadro 31 do TCC revisado (Estudo de Caso 2): TP2 (Frontstage), P2 (Backstage) e DS (Suporte)
        metricas!.NosGargalo.Should().BeEquivalentTo(new[] { nos["TP2"], nos["P2"], nos["DS"] });
    }

    [Fact]
    public async Task Metricas_DeveDevolverNosGargaloVazio_QuandoNaoHaGargalo()
    {
        var caso = await CriarCasoAsync();
        var a = await CriarNoAsync(caso.Id, "A", TipoDeNo.Ator);
        var b = await CriarNoAsync(caso.Id, "B", TipoDeNo.Touchpoint);
        await CriarArestaAsync(caso.Id, a.Id, b.Id);

        var metricas = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{caso.Id}/metricas", JsonTestOptions.Padrao);

        metricas!.NosGargalo.Should().BeEmpty();
    }

    [Fact]
    public async Task Matriz_DeveDistinguirAsArestasParalelasPelaContribuicao_QuandoEstudoDeCaso2()
    {
        var (casoId, _) = await CriarEstudoDeCaso2Async();

        var matriz = await _client.GetFromJsonAsync<List<LinhaDeRastreabilidadeDto>>($"/api/casos/{casoId}/matriz-rastreabilidade", JsonTestOptions.Padrao);

        var paralelas = matriz!.Where(l => l.NoOuAresta == "Serviço de Notificações → Gateway SMS/E-mail (externa)").ToList();
        paralelas.Should().HaveCount(2);
        paralelas.Select(l => l.Contribuicao).Should().OnlyHaveUniqueItems();
        paralelas.Should().Contain(l => l.Contribuicao.Contains("Assíncrona (peso 2)"));
        paralelas.Should().Contain(l => l.Contribuicao.Contains("Batch (peso 1)"));
    }
}
