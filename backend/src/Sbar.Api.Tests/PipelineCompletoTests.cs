using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Testes de ponta a ponta pela API real (não pelos mocks de Sbar.Application.Tests) — cobrem a
// "cola" entre camadas (rotas, DI, serialização/enum, EF Core) que já causou bugs reais nesta
// sessão e que testes unitários com mock não pegam. Não duplicam a cobertura de fórmulas, que já
// está em Sbar.Application.Tests contra os dois Estudos de Caso completos.
[Collection("Postgres")]
public class PipelineCompletoTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public PipelineCompletoTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    // Estudo de Caso 1 completo (docs/dominio/estudos-de-caso.md) — montado pelo helper compartilhado.
    private Task<(Guid CasoId, Dictionary<string, Guid> Nos)> CriarEstudoDeCaso1Async() =>
        EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

    // CT03/CT04 pela API real: Mapa (nós, arestas com justificativa e fluxos de entrada) → comunidades → dez métricas →
    // distâncias, com os valores dos Quadros 25 e 27 do TCC revisado. Os dois perfis de referência
    // ("Monólito Modular (provisório)"/"Microsserviços (provisório)", Quadro 7) já vêm semeados pela migration desde
    // o primeiro `Database.Migrate()`, então a comparação aparece na primeira leitura.
    [Fact]
    public async Task PipelineCompleto_DeveCalcularRadarComparacaoEMatriz_QuandoEstudoDeCaso1CompletoViaApi()
    {
        var (casoId, nos) = await CriarEstudoDeCaso1Async();

        var metricasComPerfis = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao);
        metricasComPerfis!.Eixos.Should().HaveCount(10);
        metricasComPerfis.Eixos.Should().OnlyContain(e => e.Aplicavel);

        var esperado = new Dictionary<Eixo, double>
        {
            [Eixo.CO] = 2.79, [Eixo.ESC] = 3.25, [Eixo.INT] = 0.95, [Eixo.UX] = 1.67, [Eixo.DISP] = 3.75,
            [Eixo.FLEX] = 3.50, [Eixo.TR] = 0.32, [Eixo.AUT] = 1.67, [Eixo.VOL] = 3.10, [Eixo.DEP] = 2.50,
        };
        foreach (var (eixo, valor) in esperado)
            metricasComPerfis.Eixos.Single(e => e.Eixo == eixo).ValorNormalizado.Should().BeApproximately(valor, 0.005, eixo.ToString());

        // RF06/CT02: três comunidades, Q = 0,41 (Quadro 24)
        metricasComPerfis.Comunidades.Should().HaveCount(3);
        metricasComPerfis.ModularidadeQ.Should().BeApproximately(0.4117, 0.00005);
        metricasComPerfis.Avisos.Should().BeEmpty();
        metricasComPerfis.Comunidades.Single(c => c.NosIds.Contains(nos["P2"])).NosIds.Should().BeEquivalentTo(new[] { nos["TP2"], nos["TP3"], nos["P1"], nos["P2"], nos["P3"], nos["DS"] });

        // CT01: pontos potencialmente críticos P1 e DS; μ/σ por banda (Quadro 23)
        metricasComPerfis.NosGargalo.Should().BeEquivalentTo(new[] { nos["P1"], nos["DS"] });
        metricasComPerfis.Bandas.Should().HaveCount(5);
        metricasComPerfis.Bandas.Single(b => b.Banda == Banda.Suporte).Limiar.Should().BeApproximately(7.0 + 2.94, 0.01);

        // RF05: fluxos de entrada, todos válidos, três bandas cada
        metricasComPerfis.Fluxos.Should().HaveCount(4).And.OnlyContain(fluxo => fluxo.Valido && fluxo.BandasPercorridas == 3);

        // Quadro 27: Monólito Modular 1,051 × Microsserviços 1,605 — o perfil mais próximo é o Monólito
        metricasComPerfis.Recomendacao.Should().NotBeNull();
        metricasComPerfis.Recomendacao!.EstiloRecomendado.Should().Be("Monólito Modular (provisório)");
        metricasComPerfis.Recomendacao.Distancias.Should().HaveCount(2);
        metricasComPerfis.Recomendacao.Distancias.Single(d => d.PerfilNome == "Monólito Modular (provisório)").Distancia
            .Should().BeApproximately(1.051, 0.0005);
        metricasComPerfis.Recomendacao.Distancias.Single(d => d.PerfilNome == "Microsserviços (provisório)").Distancia
            .Should().BeApproximately(1.605, 0.0005);

        // Removendo os perfis, a comparação some (nada pra comparar) — os eixos continuam iguais.
        var perfis = (await _client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!;
        foreach (var perfil in perfis)
            (await _client.DeleteAsync($"/api/perfis-arquiteturais/{perfil.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var metricasSemPerfis = await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao);
        metricasSemPerfis!.Recomendacao.Should().BeNull();
        metricasSemPerfis.Eixos.Single(e => e.Eixo == Eixo.CO).ValorNormalizado.Should().BeApproximately(2.79, 0.01);

        // Matriz de rastreabilidade (RF12) — as 44 linhas (14 de gargalo, arestas, comunidades e fluxos + 5 de CO + 4 de ESC + 21 de VOL) validadas em ConstrutorDeMatrizDeRastreabilidadeTests:
        // 2 gargalos (DISP), 1 aresta crítica (TR), 4 externas (INT), 3 comunidades (FLEX/AUT) e 4 fluxos (DEP).
        var matriz = (await _client.GetFromJsonAsync<List<LinhaDeRastreabilidadeDto>>($"/api/casos/{casoId}/matriz-rastreabilidade", JsonTestOptions.Padrao))!;
        matriz.Should().HaveCount(44);
        matriz.Should().Contain(l => l.NoOuAresta.EndsWith("(gargalo)") && l.Eixos.Contains(Eixo.DISP));
        matriz.Should().Contain(l => l.NoOuAresta.EndsWith("(crítica)") && l.Eixos.Contains(Eixo.TR));
        matriz.Where(l => l.Categoria == "gargalo").Should().HaveCount(2);
        matriz.Where(l => l.Categoria == "aresta-externa").Should().HaveCount(4);
        matriz.Where(l => l.Categoria == "comunidade").Should().HaveCount(3).And.OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.FLEX, Eixo.AUT }));
        matriz.Where(l => l.Categoria == "fluxo").Should().HaveCount(4).And.OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.DEP }));
    }

    [Theory]
    [InlineData("metricas")]
    [InlineData("matriz-rastreabilidade")]
    public async Task Obter_DeveRetornarNotFound_QuandoEstudoDeCasoNaoExiste(string rota)
    {
        var resposta = await _client.GetAsync($"/api/casos/{Guid.NewGuid()}/{rota}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CriarAresta_DeveRetornarUnprocessableEntity_QuandoPesoForaDoIntervalo()
    {
        var (casoId, nos) = await CriarEstudoDeCaso1Async();

        var resposta = await _client.PostAsJsonAsync(
            $"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(nos["A"], nos["TP1"], TipoDeAresta.Sincrona, 5, "Justificativa de teste"),
            JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CriarAresta_DeveRetornarUnprocessableEntity_QuandoOrigemEDestinoForemOMesmoNo()
    {
        var (casoId, nos) = await CriarEstudoDeCaso1Async();

        var resposta = await _client.PostAsJsonAsync(
            $"/api/casos/{casoId}/arestas",
            new CriarArestaRequest(nos["A"], nos["A"], TipoDeAresta.Sincrona, 1, "Justificativa de teste"),
            JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
