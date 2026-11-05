using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;

namespace Sbar.Api.Tests;

// RF11 (assinaturas editáveis) — CRUD dos Perfis Arquiteturais de referência pela API real: rotas, persistência e 404.
// A versão de parâmetros gerada a cada alteração é verificada em ParametrosVersionadosApiTests.
[Collection("Postgres")]
public class PerfisArquiteturaisApiTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public PerfisArquiteturaisApiTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    private static CriarPerfilArquiteturalRequest Perfil(string nome, double valorDeCo) =>
        new(nome, valorDeCo, 1, 1, 1, 1, 1, 1, 1, 1, 1);

    [Fact]
    public async Task Perfil_DeveSerCriadoLidoAtualizadoERemovido()
    {
        var criacao = await _client.PostAsJsonAsync("/api/perfis-arquiteturais", Perfil("Serverless", 1.5), JsonTestOptions.Padrao);
        criacao.StatusCode.Should().Be(HttpStatusCode.Created);
        var criado = (await criacao.Content.ReadFromJsonAsync<PerfilArquiteturalDto>(JsonTestOptions.Padrao))!;
        criado.Nome.Should().Be("Serverless");
        criado.ValorCO.Should().Be(1.5);

        (await _client.GetFromJsonAsync<PerfilArquiteturalDto>($"/api/perfis-arquiteturais/{criado.Id}", JsonTestOptions.Padrao))
            .Should().BeEquivalentTo(criado);
        var lista = (await _client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!;
        lista.Select(p => p.Nome).Should().Contain("Serverless").And.Contain(n => n.StartsWith("Monólito")); // junto com o seed do Quadro 7

        var atualizacao = await _client.PutAsJsonAsync($"/api/perfis-arquiteturais/{criado.Id}", Perfil("Serverless v2", 4), JsonTestOptions.Padrao);
        atualizacao.StatusCode.Should().Be(HttpStatusCode.OK);
        var relido = (await _client.GetFromJsonAsync<PerfilArquiteturalDto>($"/api/perfis-arquiteturais/{criado.Id}", JsonTestOptions.Padrao))!;
        (relido.Nome, relido.ValorCO).Should().Be(("Serverless v2", 4.0));

        (await _client.DeleteAsync($"/api/perfis-arquiteturais/{criado.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/perfis-arquiteturais/{criado.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Perfil_DeveRetornarNotFound_QuandoNaoExiste(string metodo)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), $"/api/perfis-arquiteturais/{Guid.NewGuid()}");
        if (metodo == "PUT")
            requisicao.Content = JsonContent.Create(Perfil("Fantasma", 1), options: JsonTestOptions.Padrao);

        var resposta = await _client.SendAsync(requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
