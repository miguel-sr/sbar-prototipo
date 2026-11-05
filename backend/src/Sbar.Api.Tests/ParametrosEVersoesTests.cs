using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;
using Sbar.Infrastructure;

namespace Sbar.Api.Tests;

// Ponta a ponta (rotas, EF Core, transação e bloqueio consultivo no Postgres real) das sub-etapas 9.5 e 9.6 da Etapa 9:
// parâmetros versionados (RF11, US06, RN07, CT10) e saída como subsídio com contribuição por eixo, % do máximo e margem.
[Collection("Postgres")]
public class ParametrosEVersoesTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private ApiTestFactory _api = null!;
    private HttpClient _client = null!;

    public ParametrosEVersoesTests(PostgresContainerFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _api = new ApiTestFactory(_postgres.ConnectionStringAdmin);
        await _api.IniciarAsync();
        _client = _api.Client;
    }

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();

    // Limites do Quadro 6 e pesos iguais (p_i = 0,1) — o estado semeado pela migration.
    private static List<ParametroDeEixoRequest> EixosDoQuadro7(double maximoDeCo = 40, double pesoDeCo = 0.1, double pesoDeDep = 0.1) =>
    [
        new(Eixo.CO, 6, maximoDeCo, pesoDeCo), new(Eixo.ESC, 0, 1, 0.1), new(Eixo.INT, 0, 1, 0.1), new(Eixo.UX, 0, 3, 0.1),
        new(Eixo.DISP, 0, 1, 0.1), new(Eixo.FLEX, 0, 1, 0.1), new(Eixo.TR, 0, 1, 0.1), new(Eixo.AUT, 0, 1, 0.1),
        new(Eixo.VOL, 1, 3, 0.1), new(Eixo.DEP, 0.2, 1, pesoDeDep),
    ];

    private async Task<T> LerAsync<T>(HttpResponseMessage resposta)
    {
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<T>(JsonTestOptions.Padrao))!;
    }

    private static async Task<string> MensagemDeErroAsync(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<Dictionary<string, object>>(JsonTestOptions.Padrao))!["title"].ToString()!;

    private Task<HttpResponseMessage> PutLimitesEPesosAsync(string? descricao, List<ParametroDeEixoRequest> eixos) =>
        _client.PutAsJsonAsync("/api/parametros/limites-e-pesos", new AtualizarLimitesEPesosRequest(descricao, eixos), JsonTestOptions.Padrao);

    private Task<List<VersaoDeParametrosDto>> ListarVersoesAsync() =>
        _client.GetFromJsonAsync<List<VersaoDeParametrosDto>>("/api/parametros", JsonTestOptions.Padrao)!;

    [Fact]
    public async Task Listar_DeveCriarAVersao1SobDemanda_QuandoAindaNaoHaNenhuma()
    {
        var versoes = await ListarVersoesAsync();

        var versao = versoes.Should().ContainSingle().Subject;
        versao.Numero.Should().Be(1);
        versao.Descricao.Should().Contain("Versão inicial");
        versao.CriadaEm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ObterAtual_DeveTrazerOsLimitesDoQuadro7EAsAssinaturasDoQuadro8()
    {
        var atual = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/atual"));

        atual.Versao.Numero.Should().Be(1);
        atual.Eixos.Should().HaveCount(10);
        atual.Eixos.Single(e => e.Eixo == Eixo.CO).Should().Be(new ParametroDeEixoDto(Eixo.CO, 6, 40, 0.1));
        atual.Eixos.Single(e => e.Eixo == Eixo.DEP).Should().Be(new ParametroDeEixoDto(Eixo.DEP, 0.2, 1, 0.1));
        atual.Eixos.Sum(e => e.Peso).Should().BeApproximately(1.0, 1e-9);

        atual.Assinaturas.Select(a => a.Nome).Should().BeEquivalentTo("Monólito Modular (provisório)", "Microsserviços (provisório)");
        var microsservicos = atual.Assinaturas.Single(a => a.Nome.StartsWith("Microsserviços"));
        microsservicos.Valores.Should().HaveCount(10);
        microsservicos.Valores["VOL"].Should().Be(2.0); // Quadro 7 (a reconstrução antiga tinha 4,2)
        microsservicos.Valores["AUT"].Should().Be(4.5);
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveCriarNovaVersaoSemAlterarOSnapshotDaAntiga()
    {
        await ListarVersoesAsync(); // garante a v1

        var nova = await LerAsync<ConjuntoDeParametrosDto>(await PutLimitesEPesosAsync("Recalibra CO após os estudos", EixosDoQuadro7(maximoDeCo: 45)));

        nova.Versao.Numero.Should().Be(2);
        nova.Versao.Descricao.Should().Be("Recalibra CO após os estudos");
        nova.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(45);

        var antiga = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/1"));
        antiga.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(40);
        var atual = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/atual"));
        atual.Versao.Numero.Should().Be(2);
        atual.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(45);

        (await ListarVersoesAsync()).Select(v => v.Numero).Should().Equal(2, 1);
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveCriarAVersaoInicialAntesDaPrimeiraAlteracao_QuandoAindaNaoExistia()
    {
        // Sem nenhuma leitura prévia: a v1 guarda o estado anterior à alteração, e a alteração vira a v2.
        var nova = await LerAsync<ConjuntoDeParametrosDto>(await PutLimitesEPesosAsync(null, EixosDoQuadro7(maximoDeCo: 50)));

        nova.Versao.Numero.Should().Be(2);
        nova.Versao.Descricao.Should().Be("Limites e pesos atualizados");
        var v1 = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/1"));
        v1.Eixos.Single(e => e.Eixo == Eixo.CO).Maximo.Should().Be(40);
    }

    // CT10 — soma dos pesos diferente de 1: 422 com mensagem, nada gravado.
    [Fact]
    public async Task AtualizarLimitesEPesos_DeveRetornarUnprocessableEntityENaoCriarVersao_QuandoASomaDosPesosNaoEUm()
    {
        await ListarVersoesAsync();

        var resposta = await PutLimitesEPesosAsync("inválido", EixosDoQuadro7(pesoDeCo: 0.2));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(resposta)).Should().Be("Soma dos pesos igual a 1,1; deve ser 1.");
        (await ListarVersoesAsync()).Should().ContainSingle();
        var atual = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/atual"));
        atual.Eixos.Single(e => e.Eixo == Eixo.CO).Peso.Should().Be(0.1);
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveRetornarUnprocessableEntity_QuandoUmEixoFalta()
    {
        var resposta = await PutLimitesEPesosAsync("sem DEP", EixosDoQuadro7().Where(e => e.Eixo != Eixo.DEP).ToList());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(resposta)).Should().Contain("DEP");
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveRetornarUnprocessableEntity_QuandoOMinimoNaoEMenorQueOMaximo()
    {
        var eixos = EixosDoQuadro7();
        eixos[0] = eixos[0] with { Maximo = 6 };

        var resposta = await PutLimitesEPesosAsync("limites trocados", eixos);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(resposta)).Should().Contain("CO");
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveRetornarUnprocessableEntity_QuandoUmPesoENegativo()
    {
        var eixos = EixosDoQuadro7(pesoDeCo: -0.1, pesoDeDep: 0.3);

        var resposta = await PutLimitesEPesosAsync("peso negativo", eixos);

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(resposta)).Should().Contain("maior ou igual a zero");
    }

    [Fact]
    public async Task AtualizarLimitesEPesos_DeveRetornarUnprocessableEntity_QuandoADescricaoPassaDe200Caracteres()
    {
        var resposta = await PutLimitesEPesosAsync(new string('x', 201), EixosDoQuadro7());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ObterPorNumero_DeveRetornarNotFound_QuandoAVersaoNaoExiste()
    {
        var resposta = await _client.GetAsync("/api/parametros/99");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await MensagemDeErroAsync(resposta)).Should().Contain("99");
    }

    [Fact]
    public async Task AlterarPerfis_DeveGerarUmaVersaoPorAlteracaoComDescricaoAutomaticaEPreservarAAntiga()
    {
        var perfis = (await _client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!;
        var monolito = perfis.Single(p => p.Nome.StartsWith("Monólito"));

        // atualizar (v2), criar (v3), remover (v4)
        var atualizar = await _client.PutAsJsonAsync($"/api/perfis-arquiteturais/{monolito.Id}",
            new CriarPerfilArquiteturalRequest("Monólito Modular (provisório)", 5, 1.5, 1, 2.5, 3, 1.5, 1.5, 1, 3, 3), JsonTestOptions.Padrao);
        atualizar.StatusCode.Should().Be(HttpStatusCode.OK);
        var criado = await LerAsync<PerfilArquiteturalDto>(await _client.PostAsJsonAsync("/api/perfis-arquiteturais",
            new CriarPerfilArquiteturalRequest("Serverless", 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), JsonTestOptions.Padrao));
        (await _client.DeleteAsync($"/api/perfis-arquiteturais/{criado.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var versoes = await ListarVersoesAsync();
        versoes.Select(v => v.Numero).Should().Equal(4, 3, 2, 1);
        versoes.Select(v => v.Descricao).Should().Equal(
            "Assinatura 'Serverless' removida",
            "Assinatura 'Serverless' criada",
            "Assinatura 'Monólito Modular (provisório)' atualizada",
            versoes[3].Descricao);

        var v1 = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/1"));
        v1.Assinaturas.Single(a => a.Nome.StartsWith("Monólito")).Valores["CO"].Should().Be(2.0);
        var v2 = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/2"));
        v2.Assinaturas.Single(a => a.Nome.StartsWith("Monólito")).Valores["CO"].Should().Be(5.0);
        var v3 = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/3"));
        v3.Assinaturas.Select(a => a.Nome).Should().Contain("Serverless");
        var v4 = await LerAsync<ConjuntoDeParametrosDto>(await _client.GetAsync("/api/parametros/4"));
        v4.Assinaturas.Select(a => a.Nome).Should().NotContain("Serverless");
    }

    [Fact]
    public async Task AlterarPerfil_NaoDeveCriarVersao_QuandoOPerfilNaoExiste()
    {
        await ListarVersoesAsync();

        var resposta = await _client.PutAsJsonAsync($"/api/perfis-arquiteturais/{Guid.NewGuid()}",
            new CriarPerfilArquiteturalRequest("Fantasma", 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), JsonTestOptions.Padrao);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListarVersoesAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Metricas_DeveInformarAVersaoDosParametros_EReaplicarUmaVersaoAntigaAposAlterarUmaAssinatura()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var antes = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas"));
        antes.VersaoDosParametros.Numero.Should().Be(1);
        var distanciaDoMonolitoAntes = antes.Recomendacao!.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito")).Distancia;
        distanciaDoMonolitoAntes.Should().BeApproximately(1.051, 0.0005);

        // Altera a assinatura do Monólito (CO 2 → 5): nova versão; a v1 continua reproduzindo o resultado do TCC.
        var monolito = (await _client.GetFromJsonAsync<List<PerfilArquiteturalDto>>("/api/perfis-arquiteturais", JsonTestOptions.Padrao))!
            .Single(p => p.Nome.StartsWith("Monólito"));
        (await _client.PutAsJsonAsync($"/api/perfis-arquiteturais/{monolito.Id}",
            new CriarPerfilArquiteturalRequest(monolito.Nome, 5, 1.5, 1, 2.5, 3, 1.5, 1.5, 1, 3, 3), JsonTestOptions.Padrao))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var vigente = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas"));
        vigente.VersaoDosParametros.Numero.Should().Be(2);
        vigente.Recomendacao!.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito")).Distancia.Should().NotBeApproximately(distanciaDoMonolitoAntes, 0.01);

        var reaplicada = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas?versao=1"));
        reaplicada.VersaoDosParametros.Numero.Should().Be(1);
        reaplicada.Recomendacao!.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito")).Distancia.Should().BeApproximately(distanciaDoMonolitoAntes, 1e-9);
        reaplicada.Recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Microsserviços")).Distancia.Should().BeApproximately(1.605, 0.0005);
    }

    [Fact]
    public async Task Metricas_DeveUsarOsLimitesDaVersaoPedida_QuandoOLimiteDeCoMudou()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);
        await PutLimitesEPesosAsync("CO até 45", EixosDoQuadro7(maximoDeCo: 45));

        var vigente = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas"));
        var v1 = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas?versao=1"));

        vigente.Eixos.Single(e => e.Eixo == Eixo.CO).ValorNormalizado.Should().BeApproximately(5.0 * (25 - 6) / (45 - 6), 1e-9);
        v1.Eixos.Single(e => e.Eixo == Eixo.CO).ValorNormalizado.Should().BeApproximately(5.0 * (25 - 6) / (40 - 6), 1e-9);
    }

    [Fact]
    public async Task Metricas_DeveRetornarNotFound_QuandoAVersaoNaoExiste()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resposta = await _client.GetAsync($"/api/casos/{casoId}/metricas?versao=99");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // CT10 pela API: com a soma dos pesos do estado vigente ≠ 1 (alteração direta no banco, fora da API), o cálculo é bloqueado;
    // a versão 1 (snapshot) continua calculável.
    [Fact]
    public async Task Metricas_DeveBloquearOCalculoComUnprocessableEntity_QuandoASomaDosPesosVigentesNaoEUm()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);
        await ListarVersoesAsync();

        using (var escopo = _api.Factory.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<SbarDbContext>();
            await contexto.Database.ExecuteSqlAsync($"UPDATE \"ConfiguracoesDeEixo\" SET \"Peso\" = 0.2 WHERE \"Eixo\" = 'CO'");
        }

        var bloqueada = await _client.GetAsync($"/api/casos/{casoId}/metricas");
        bloqueada.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await MensagemDeErroAsync(bloqueada)).Should().Be("Soma dos pesos igual a 1,1; deve ser 1.");

        (await _client.GetAsync($"/api/casos/{casoId}/metricas?versao=1")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metricas_DeveInformarContribuicoesPercentualEMargem_QuandoEstudoDeCaso1()
    {
        var (casoId, _) = await EstudosDeCasoViaApi.CriarEstudoDeCaso1Async(_client);

        var resultado = await LerAsync<ResultadoDeMetricasDto>(await _client.GetAsync($"/api/casos/{casoId}/metricas"));

        var recomendacao = resultado.Recomendacao!;
        recomendacao.DistanciaMaxima.Should().Be(5);
        recomendacao.Margem.Should().BeApproximately(0.553, 0.001); // TCC 6.10.1
        var monolito = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Monólito"));
        var microsservicos = recomendacao.Distancias.Single(d => d.PerfilNome.StartsWith("Microsserviços"));
        monolito.Contribuicoes.Should().HaveCount(10);
        monolito.Contribuicoes.Sum(c => c.Contribuicao).Should().BeApproximately(1.1050, 0.0001); // Quadro 26
        microsservicos.Contribuicoes.Sum(c => c.Contribuicao).Should().BeApproximately(2.5747, 0.0002);
        monolito.PercentualDoMaximo.Should().BeApproximately(21, 0.5);
        microsservicos.PercentualDoMaximo.Should().BeApproximately(32, 0.5);
        microsservicos.Contribuicoes.Single(c => c.Eixo == Eixo.INT).Contribuicao.Should().BeApproximately(0.649, 0.0005);
    }

    // RF11 — gravações concorrentes não podem colidir na numeração (bloqueio consultivo + transação).
    [Fact]
    public async Task AtualizarLimitesEPesos_DeveNumerarSemColisao_QuandoHaGravacoesConcorrentes()
    {
        const int gravacoes = 8;

        var respostas = await Task.WhenAll(Enumerable.Range(0, gravacoes).Select(i =>
            PutLimitesEPesosAsync($"alteração {i}", EixosDoQuadro7(maximoDeCo: 41 + i))));

        respostas.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        var versoes = await ListarVersoesAsync();
        versoes.Select(v => v.Numero).Should().Equal(Enumerable.Range(1, gravacoes + 1).Reverse()); // v1 + 8 alterações
        versoes.Select(v => v.Descricao).Where(d => d.StartsWith("alteração")).Should().HaveCount(gravacoes);
    }
}
