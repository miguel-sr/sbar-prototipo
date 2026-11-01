using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;
using Xunit.Abstractions;

namespace Sbar.Api.Tests;

// RNF05 (TCC 5.3.4, Quadro 12): "os cálculos de um Mapa com até 50 nós devem ser concluídos em até 2 segundos". Mede a chamada
// real de métricas (HTTP → serviços → EF Core → Postgres → CNM → dez eixos → distâncias) com um Mapa sintético de 50 nós, todas as
// bandas e densidade parecida com a dos estudos de caso (≈ 1,4 aresta por nó). A primeira execução (aquecimento do JIT e do
// EF) é descartada: o limite vale a partir da segunda. O tempo medido é registrado na saída do teste.
[Collection("Postgres")]
public class DesempenhoDoCalculoTests : IAsyncLifetime
{
    private const int LimiteEmMilissegundos = 2000;

    private readonly PostgresContainerFixture _postgres;
    private readonly ITestOutputHelper _saida;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public DesempenhoDoCalculoTests(PostgresContainerFixture postgres, ITestOutputHelper saida)
    {
        _postgres = postgres;
        _saida = saida;
    }

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    // Mesma geração determinística de Sbar.Application.Tests/Grafo/MapaSintetico.cs (semente fixa): 1 Ator, 9 Touchpoints, 18 Processos,
    // 8 de Suporte, 6 de Dados e 8 Externos = 50 nós; uma árvore geradora (49 arestas) mais 21 arestas aleatórias = 70 arestas.
    private static (List<(string Nome, TipoDeNo Tipo)> Nos, List<(int Origem, int Destino, TipoDeAresta Tipo, int Peso)> Arestas) GerarMapaDe50Nos()
    {
        var rng = new Random(20261021);
        var tipos = new List<TipoDeNo>();
        tipos.Add(TipoDeNo.Ator);
        tipos.AddRange(Enumerable.Repeat(TipoDeNo.Touchpoint, 9));
        tipos.AddRange(Enumerable.Repeat(TipoDeNo.Processo, 18));
        tipos.AddRange(Enumerable.Repeat(TipoDeNo.Suporte, 8));
        tipos.AddRange(Enumerable.Repeat(TipoDeNo.Dados, 6));
        tipos.AddRange(Enumerable.Repeat(TipoDeNo.Externo, 8));

        var nos = tipos.Select((tipo, i) => ($"Nó {i + 1:00} ({tipo})", tipo)).ToList();
        var arestas = new List<(int, int, TipoDeAresta, int)>();
        var tiposDeAresta = new[] { TipoDeAresta.Sincrona, TipoDeAresta.Sincrona, TipoDeAresta.Assincrona, TipoDeAresta.Batch, TipoDeAresta.Critica };

        for (var i = 1; i < nos.Count; i++)
            arestas.Add((rng.Next(0, i), i, tiposDeAresta[rng.Next(tiposDeAresta.Length)], rng.Next(1, 4)));

        while (arestas.Count < 70)
        {
            var origem = rng.Next(0, nos.Count);
            var destino = rng.Next(0, nos.Count);
            if (origem != destino)
                arestas.Add((origem, destino, tiposDeAresta[rng.Next(tiposDeAresta.Length)], rng.Next(1, 4)));
        }

        return (nos, arestas);
    }

    private async Task<Guid> CriarMapaSinteticoAsync()
    {
        var (nosSpec, arestasSpec) = GerarMapaDe50Nos();
        var caso = (await (await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Mapa sintético de 50 nós", null), JsonTestOptions.Padrao))
            .Content.ReadFromJsonAsync<EstudoDeCasoDto>(JsonTestOptions.Padrao))!;

        var ids = new List<Guid>();
        foreach (var (nome, tipo) in nosSpec)
        {
            var resposta = await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest(nome, tipo), JsonTestOptions.Padrao);
            resposta.EnsureSuccessStatusCode();
            ids.Add((await resposta.Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao))!.Id);
        }

        foreach (var (origem, destino, tipo, peso) in arestasSpec)
            (await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/arestas",
                new CriarArestaRequest(ids[origem], ids[destino], tipo, peso, "Interação sintética do teste de desempenho"), JsonTestOptions.Padrao))
                .EnsureSuccessStatusCode();

        // Fluxos válidos por construção: caminhadas aleatórias que seguem arestas existentes.
        var rng = new Random(7);
        var saidas = arestasSpec.GroupBy(a => a.Origem).ToDictionary(g => g.Key, g => g.Select(a => a.Destino).ToList());
        var criados = 0;
        for (var tentativa = 0; tentativa < 100 && criados < 6; tentativa++)
        {
            var atual = rng.Next(0, nosSpec.Count);
            var caminho = new List<int> { atual };
            while (caminho.Count < 6 && saidas.TryGetValue(atual, out var proximos))
            {
                atual = proximos[rng.Next(proximos.Count)];
                caminho.Add(atual);
            }

            if (caminho.Count < 2)
                continue;

            (await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/fluxos",
                new CriarFluxoRequest($"Fluxo sintético {criados + 1}", caminho.Select(i => ids[i]).ToList()), JsonTestOptions.Padrao)).EnsureSuccessStatusCode();
            criados++;
        }

        criados.Should().BeGreaterThan(0, "o Mapa sintético precisa de fluxos para exercitar o DEP");
        return caso.Id;
    }

    private async Task<long> MedirAsync(string rota)
    {
        var cronometro = Stopwatch.StartNew();
        var resposta = await _client.GetAsync(rota);
        cronometro.Stop();
        resposta.EnsureSuccessStatusCode();
        _ = await resposta.Content.ReadAsStringAsync();
        return cronometro.ElapsedMilliseconds;
    }

    [Fact]
    public async Task Metricas_DeveConcluirEmAte2Segundos_QuandoOMapaTem50Nos()
    {
        var casoId = await CriarMapaSinteticoAsync();

        var aquecimento = await MedirAsync($"/api/casos/{casoId}/metricas");
        var medido = await MedirAsync($"/api/casos/{casoId}/metricas");
        var matriz = await MedirAsync($"/api/casos/{casoId}/matriz-rastreabilidade");
        var exportacao = await MedirAsync($"/api/casos/{casoId}/exportacao?formato=csv");

        _saida.WriteLine($"RNF05 — Mapa de 50 nós / 70 arestas: GET /metricas = {medido} ms (1ª chamada, com aquecimento: {aquecimento} ms); " +
                         $"matriz = {matriz} ms; exportação CSV = {exportacao} ms. Limite: {LimiteEmMilissegundos} ms.");

        medido.Should().BeLessThan(LimiteEmMilissegundos);
        matriz.Should().BeLessThan(LimiteEmMilissegundos);

        var resultado = (await _client.GetFromJsonAsync<ResultadoDeMetricasDto>($"/api/casos/{casoId}/metricas", JsonTestOptions.Padrao))!;
        resultado.Eixos.Should().HaveCount(10);
        resultado.Comunidades.Sum(c => c.NosIds.Count).Should().Be(50);
        resultado.Fluxos.Should().OnlyContain(f => f.Valido);
    }
}
