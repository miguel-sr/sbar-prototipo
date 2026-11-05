using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Ponta a ponta da sub-etapa 9.8 (RF13, US07): exportação da análise em JSON e CSV (D13), com os valores dos Quadros 25 e 27.
[Collection("Postgres")]
public partial class ExportacaoTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public ExportacaoTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    private static string NomeDoAnexo(HttpResponseMessage resposta) =>
        resposta.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty;

    [Fact]
    public async Task ExportarJson_DeveReunirTudoQueOTccManda_QuandoEstudoDeCaso1()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        resposta.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        NomeDoAnexo(resposta).Should().Be("sbar-sistema-de-pedidos-digital-v1.json");

        var exportacao = (await resposta.Content.ReadFromJsonAsync<ExportacaoDto>(JsonTestOptions.Padrao))!;
        exportacao.GeradoEm.Kind.Should().Be(DateTimeKind.Utc);
        exportacao.Caso.Nome.Should().Be("Sistema de Pedidos Digital");
        exportacao.VersaoDosParametros.Numero.Should().Be(1);
        exportacao.Parametros.Versao.Numero.Should().Be(1);
        exportacao.Parametros.Eixos.Should().HaveCount(10);
        exportacao.Parametros.Assinaturas.Should().HaveCount(2);

        // entradas: 15 nós, 21 arestas (todas com justificativa) e 4 fluxos válidos
        exportacao.Nos.Should().HaveCount(15);
        exportacao.Arestas.Should().HaveCount(21).And.OnlyContain(a => !string.IsNullOrWhiteSpace(a.Justificativa));
        exportacao.Fluxos.Should().HaveCount(4).And.OnlyContain(f => f.Valido);

        // comunidades e Q (Quadro 24), métricas (Quadro 25) e bandas (Quadro 23)
        exportacao.Comunidades.Should().HaveCount(3);
        exportacao.ModularidadeQ.Should().BeApproximately(0.4117, 0.00005);
        exportacao.Eixos.Should().HaveCount(10);
        exportacao.Eixos.Single(e => e.Eixo == Eixo.FLEX).ValorNormalizado.Should().BeApproximately(3.5, 1e-9);
        exportacao.Eixos.Single(e => e.Eixo == Eixo.AUT).ValorNormalizado.Should().BeApproximately(5.0 / 3, 1e-9);
        exportacao.Eixos.Single(e => e.Eixo == Eixo.DEP).ValorNormalizado.Should().BeApproximately(2.5, 1e-9);
        exportacao.Bandas.Should().HaveCount(5);

        // distâncias com contribuições e margem (Quadros 26 e 27)
        var recomendacao = exportacao.Recomendacao!;
        recomendacao.EstiloRecomendado.Should().Be("Monólito Modular (provisório)");
        recomendacao.Margem.Should().BeApproximately(0.553, 0.001);
        recomendacao.Distancias.Should().OnlyContain(d => d.Contribuicoes.Count == 10);
        recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito")).Distancia.Should().BeApproximately(1.051, 0.0005);

        // matriz de rastreabilidade completa (44 linhas: 14 de gargalo, arestas, 3 comunidades e fluxos + 5 de CO + 4 de ESC + 21 de VOL)
        exportacao.Matriz.Should().HaveCount(44);
    }

    [Fact]
    public async Task ExportarJson_DeveUsarAVersaoPedida_QuandoOsParametrosMudaram()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);
        var monolito = (await _client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!
            .Single(p => p.Nome.StartsWith("Monólito"));
        (await _client.PutAsJsonAsync($"/api/perfis-arquiteturais/{monolito.Id}",
            new CriarPerfilArquiteturalRequest(monolito.Nome, 5, 1.5, 1, 2.5, 3, 1.5, 1.5, 1, 3, 3), JsonTestOptions.Padrao)).EnsureSuccessStatusCode();

        var vigente = (await _client.GetFromJsonAsync<ExportacaoDto>($"/api/casos/{casoId}/exportacao", JsonTestOptions.Padrao))!;
        var antiga = (await _client.GetFromJsonAsync<ExportacaoDto>($"/api/casos/{casoId}/exportacao?versao=1", JsonTestOptions.Padrao))!;

        vigente.VersaoDosParametros.Numero.Should().Be(2);
        antiga.VersaoDosParametros.Numero.Should().Be(1);
        antiga.Parametros.Assinaturas.Single(a => a.Nome.StartsWith("Monólito")).Valores["CO"].Should().Be(2.0);
        antiga.Recomendacao!.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito")).Distancia.Should().BeApproximately(1.051, 0.0005);
    }

    [Fact]
    public async Task ExportarCsv_DeveGerarSecoesComBomSeparadorPontoEPontoDecimal_QuandoEstudoDeCaso1()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao?formato=csv");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        resposta.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
        resposta.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        NomeDoAnexo(resposta).Should().Be("sbar-sistema-de-pedidos-digital-v1.csv");

        var bytes = await resposta.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF); // BOM UTF-8
        var csv = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        var linhas = csv.Split("\r\n");

        foreach (var secao in new[]
        {
            "# Caso", "# Versão dos parâmetros", "# Limites e pesos", "# Assinaturas de referência", "# Nós", "# Arestas",
            "# Fluxos principais", "# Comunidades", "# Modularidade", "# Avisos", "# Bandas", "# Eixos", "# Distâncias",
            "# Pesos efetivos", "# Contribuições por eixo", "# Matriz de rastreabilidade",
        })
            linhas.Should().Contain(secao);

        linhas.Should().Contain("eixo;aplicavel;valorBruto;valorNormalizado;motivo");
        linhas.Should().Contain(l => RegexLinhaDoFlex().IsMatch(l)); // FLEX;true;0.7;3.5; — ponto decimal invariante
        linhas.Should().Contain(l => l.StartsWith("0.4117") || l.StartsWith("0.41169")); // modularidade Q
        var dep = linhas.Single(l => l.StartsWith("DEP;true;")).Split(';');
        double.Parse(dep[2], System.Globalization.CultureInfo.InvariantCulture).Should().BeApproximately(0.6, 1e-9);   // bruto
        double.Parse(dep[3], System.Globalization.CultureInfo.InvariantCulture).Should().BeApproximately(2.5, 1e-9);   // normalizado
        // Seções separadas por linha em branco e cabeçalho com o separador ';'
        linhas.Should().Contain("id;nome;tipo;banda");
        csv.Should().Contain("Monólito Modular (provisório);1.05");
        // 15 nós e 21 arestas: linhas de dados entre o cabeçalho e a próxima seção
        DadosDaSecao(linhas, "# Nós").Should().HaveCount(15);
        DadosDaSecao(linhas, "# Arestas").Should().HaveCount(21);
        DadosDaSecao(linhas, "# Matriz de rastreabilidade").Should().HaveCount(44);
        DadosDaSecao(linhas, "# Contribuições por eixo").Should().HaveCount(20); // 2 perfis × 10 eixos
        DadosDaSecao(linhas, "# Pesos efetivos").Should().HaveCount(10); // dez eixos aplicáveis: efetivos = configurados
    }

    [Fact]
    public async Task ExportarCsv_DeveEscaparSeparadorEAspasNosCampos_QuandoAJustificativaTemPontoEVirgulaEAspas()
    {
        var caso = (await (await _client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest("Clínica Ação; teste", null), JsonTestOptions.Padrao))
            .Content.ReadFromJsonAsync<EstudoDeCasoDto>(JsonTestOptions.Padrao))!;
        var a = (await (await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest("Paciente", TipoDeNo.Ator), JsonTestOptions.Padrao))
            .Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao))!;
        var b = (await (await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest("Portal", TipoDeNo.Touchpoint), JsonTestOptions.Padrao))
            .Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao))!;
        (await _client.PostAsJsonAsync($"/api/casos/{caso.Id}/arestas",
            new CriarArestaRequest(a.Id, b.Id, TipoDeAresta.Sincrona, 3, "Acesso; \"obrigatório\" ao portal"), JsonTestOptions.Padrao)).EnsureSuccessStatusCode();

        var resposta = await _client.GetAsync($"/api/casos/{caso.Id}/exportacao?formato=csv");

        NomeDoAnexo(resposta).Should().Be("sbar-clinica-acao-teste-v1.csv");
        var csv = await resposta.Content.ReadAsStringAsync();
        csv.Should().Contain("\"Acesso; \"\"obrigatório\"\" ao portal\"");
        csv.Should().Contain("\"Clínica Ação; teste\"");
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("pdf")]
    [InlineData("csv2")]
    public async Task Exportar_DeveRetornarBadRequest_QuandoOFormatoNaoEJsonNemCsv(string formato)
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao?formato={formato}");

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var titulo = (await resposta.Content.ReadFromJsonAsync<Dictionary<string, object>>(JsonTestOptions.Padrao))!["title"].ToString()!;
        titulo.Should().Contain("json").And.Contain("csv");
    }

    [Fact]
    public async Task Exportar_DeveUsarJson_QuandoOFormatoVemVazio()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao?formato=");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Exportar_DeveAceitarOFormatoSemDistinguirMaiusculas()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao?formato=CSV");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task Exportar_DeveRetornarNotFound_QuandoOEstudoDeCasoNaoExiste()
    {
        var resposta = await _client.GetAsync($"/api/casos/{Guid.NewGuid()}/exportacao");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Exportar_DeveRetornarNotFound_QuandoAVersaoDosParametrosNaoExiste()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/exportacao?versao=99");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Linhas de dados de uma seção do CSV: entre o cabeçalho (linha seguinte ao título) e a linha em branco.
    private static List<string> DadosDaSecao(string[] linhas, string titulo)
    {
        var inicio = Array.IndexOf(linhas, titulo);
        return linhas.Skip(inicio + 2).TakeWhile(l => l.Length > 0).ToList();
    }

    [GeneratedRegex(@"^FLEX;true;0\.[67]\d*;3\.[45]\d*;$")]
    private static partial Regex RegexLinhaDoFlex();
}
