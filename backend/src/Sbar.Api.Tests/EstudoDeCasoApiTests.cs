using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Rotas do Estudo de Caso pela API real: criação/leitura de ponta a ponta e o 404 de toda rota que depende de um caso existente.
[Collection("Postgres")]
public class EstudoDeCasoApiTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public EstudoDeCasoApiTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    [Fact]
    public async Task EstudoDeCaso_DeveSerCriadoELidoComNosEArestas()
    {
        var respostaCaso = await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Pedidos Digital", "Teste de integração"));
        respostaCaso.StatusCode.Should().Be(HttpStatusCode.Created);
        var caso = await respostaCaso.Content.ReadFromJsonAsync<EstudoDeCasoDto>(JsonTestOptions.Padrao);

        var respostaNo1 = await _client.PostAsJsonAsync($"/api/casos/{caso!.Id}/nos", new CriarNoRequest("Cliente", TipoDeNo.Ator), JsonTestOptions.Padrao);
        respostaNo1.StatusCode.Should().Be(HttpStatusCode.Created);
        var no1 = await respostaNo1.Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao);

        var respostaNo2 = await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest("Cardápio digital", TipoDeNo.Touchpoint), JsonTestOptions.Padrao);
        respostaNo2.StatusCode.Should().Be(HttpStatusCode.Created);
        var no2 = await respostaNo2.Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao);

        var respostaAresta = await _client.PostAsJsonAsync(
            $"/api/casos/{caso.Id}/arestas",
            new CriarArestaRequest(no1!.Id, no2!.Id, TipoDeAresta.Sincrona, 3, "Justificativa de teste"),
            JsonTestOptions.Padrao);

        respostaAresta.StatusCode.Should().Be(HttpStatusCode.Created);

        var releitura = await _client.GetFromJsonAsync<EstudoDeCasoDto>($"/api/casos/{caso.Id}", JsonTestOptions.Padrao);

        releitura!.Nos.Should().HaveCount(2);
        releitura.Nos.Should().ContainSingle(n => n.Banda == Banda.Frontstage);
        releitura.Arestas.Should().ContainSingle(a => a.Peso == 3);
    }

    // Toda rota que depende de um Estudo de Caso responde 404 (e não 500) quando ele não existe — o mapeamento vem
    // do tratador global de exceções; uma rota nova que esqueça de carregar o caso pelo serviço aparece aqui.
    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("GET", "/nos")]
    [InlineData("POST", "/nos")]
    [InlineData("PUT", "/nos/{filho}")]
    [InlineData("DELETE", "/nos/{filho}")]
    [InlineData("GET", "/arestas")]
    [InlineData("POST", "/arestas")]
    [InlineData("PUT", "/arestas/{filho}")]
    [InlineData("DELETE", "/arestas/{filho}")]
    [InlineData("GET", "/fluxos")]
    [InlineData("POST", "/fluxos")]
    [InlineData("PUT", "/fluxos/{filho}")]
    [InlineData("DELETE", "/fluxos/{filho}")]
    [InlineData("GET", "/metricas")]
    [InlineData("GET", "/matriz-rastreabilidade")]
    [InlineData("GET", "/exportacao")]
    public async Task Rota_DeveRetornarNotFound_QuandoOEstudoDeCasoNaoExiste(string metodo, string sufixo)
    {
        var rota = $"/api/casos/{Guid.NewGuid()}{sufixo.Replace("{filho}", Guid.NewGuid().ToString())}";
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), rota);
        var id = Guid.NewGuid();
        var recurso = sufixo.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        requisicao.Content = (metodo, recurso) switch
        {
            ("PUT", "") => JsonContent.Create(new AtualizarEstudoDeCasoRequest("X", null), options: JsonTestOptions.Padrao),
            ("POST", "nos") => JsonContent.Create(new CriarNoRequest("Cliente", TipoDeNo.Ator), options: JsonTestOptions.Padrao),
            ("PUT", "nos") => JsonContent.Create(new AtualizarNoRequest("Cliente", TipoDeNo.Ator), options: JsonTestOptions.Padrao),
            ("POST", "arestas") => JsonContent.Create(new CriarArestaRequest(id, Guid.NewGuid(), TipoDeAresta.Sincrona, 1, "j"), options: JsonTestOptions.Padrao),
            ("PUT", "arestas") => JsonContent.Create(new AtualizarArestaRequest(id, Guid.NewGuid(), TipoDeAresta.Sincrona, 1, "j"), options: JsonTestOptions.Padrao),
            ("POST", "fluxos") => JsonContent.Create(new CriarFluxoRequest("F", [id, Guid.NewGuid()]), options: JsonTestOptions.Padrao),
            ("PUT", "fluxos") => JsonContent.Create(new AtualizarFluxoRequest("F", [id, Guid.NewGuid()]), options: JsonTestOptions.Padrao),
            _ => null,
        };

        var resposta = await _client.SendAsync(requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound, $"{metodo} {rota}");
    }
}
