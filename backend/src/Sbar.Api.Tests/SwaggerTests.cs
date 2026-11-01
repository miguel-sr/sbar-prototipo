using System.Net;
using FluentAssertions;

namespace Sbar.Api.Tests;

// O Swagger só falha em tempo de execução (anotações, tipos duplicados, exemplos): este teste garante que o documento OpenApi
// continua sendo gerado com as rotas dos parâmetros versionados, da exportação, dos fluxos e dos demais recursos.
[Collection("Postgres")]
public class SwaggerTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;

    public SwaggerTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    [Fact]
    public async Task Swagger_DeveGerarODocumentoOpenApiComAsRotasDaEtapa9()
    {
        var resposta = await _api.Client.GetAsync("/swagger/v1/swagger.json");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var documento = await resposta.Content.ReadAsStringAsync();
        documento.Should().Contain("/api/parametros").And.Contain("/api/parametros/atual").And.Contain("/api/parametros/limites-e-pesos")
            .And.Contain("/api/casos/{casoId}/exportacao").And.Contain("/api/casos/{casoId}/fluxos")
            .And.Contain("/api/casos/{casoId}/metricas").And.Contain("text/csv");
    }
}
