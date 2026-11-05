using FluentAssertions;
using Sbar.Application.Grafo;
using Sbar.Application.Rastreabilidade;
using Sbar.Application.Tests.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Xunit;

namespace Sbar.Application.Tests.Rastreabilidade;

// RF12, US07 (TCC 4.6.1, Quadros 28 e 36): a matriz é derivada do Mapa — gargalos (Quadros 23 e 31), arestas críticas e
// externas, comunidades e fluxos — e cada linha informa os nós e as arestas de origem.
public class ConstrutorDeMatrizDeRastreabilidadeTests
{
    [Fact]
    public void Construir_DeveListarOsGargalosDeBackstageESuporte_QuandoEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasDeGargalo = linhas.Where(l => l.NoOuAresta.EndsWith("(gargalo)")).ToList();
        linhasDeGargalo.Should().HaveCount(2);
        linhasDeGargalo.Should().Contain(l => l.NoOuAresta == $"{nos["P1"].Nome} (gargalo)");
        linhasDeGargalo.Should().Contain(l => l.NoOuAresta == $"{nos["DS"].Nome} (gargalo)");
        linhasDeGargalo.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.DISP }));
    }

    // Quadro 36 do TCC revisado: TP2 (Frontstage) é gargalo e a matriz o liga a UX — ele NÃO entra em DISP (7.4: DISP só
    // considera Backstage e Suporte), então a linha traz o eixo da fórmula e explica a diferença.
    [Fact]
    public void Construir_DeveListarOGargaloDeFrontstageComUxENaoComDisp_QuandoEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasDeGargalo = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.Gargalo).ToList();
        linhasDeGargalo.Should().HaveCount(3);
        linhasDeGargalo.Select(l => l.NoOuAresta).Should().BeEquivalentTo(
            $"{nos["TP2"].Nome} (gargalo)", $"{nos["P2"].Nome} (gargalo)", $"{nos["DS"].Nome} (gargalo)");
        var tp2 = linhasDeGargalo.Single(l => l.NosIds.Contains(nos["TP2"].Id));
        tp2.Eixos.Should().Equal(Eixo.UX);
        tp2.Contribuicao.Should().Contain("Carga 9").And.Contain("μ+σ = 8,58").And.Contain("Não entra em DISP");
        linhasDeGargalo.Where(l => l.NosIds.Contains(nos["P2"].Id) || l.NosIds.Contains(nos["DS"].Id))
            .Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.DISP }));
        linhas.Count(l => l.NosIds.Contains(nos["TP2"].Id) && l.Eixos.Contains(Eixo.UX)).Should().Be(1); // TP2 aparece uma vez, não também como Touchpoint
    }

    [Fact]
    public void Construir_DeveListarAArestaCriticaComSeusNosEArestaDeOrigem_QuandoEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasCriticas = linhas.Where(l => l.NoOuAresta.EndsWith("(crítica)")).ToList();
        linhasCriticas.Should().ContainSingle();
        var critica = linhasCriticas.Single();
        critica.NoOuAresta.Should().Be($"{nos["P2"].Nome} → {nos["P3"].Nome} (crítica)");
        critica.Eixos.Should().Equal(Eixo.TR);
        critica.NosIds.Should().Equal(nos["P2"].Id, nos["P3"].Id); // rastreável até o nó e a aresta de origem
        critica.ArestasIds.Should().ContainSingle().Which.Should().Be(estudoDeCaso.Arestas.Single(a => a.Tipo == TipoDeAresta.Critica).Id);
    }

    // INT (4.4.2): toda aresta ligada à banda Externo — 4 no Estudo de Caso 1 (Quadro 25) e 5 no 2 (Quadro 33, com as paralelas)
    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public void Construir_DeveListarTodasAsArestasLigadasABandaExterna(int estudo, int quantidadeEsperada)
    {
        var (estudoDeCaso, _) = estudo == 1 ? EstudosDeCasoDeReferencia.CriarEstudoDeCaso1() : EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasExternas = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.ArestaExterna).ToList();
        linhasExternas.Should().HaveCount(quantidadeEsperada);
        linhasExternas.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.INT }) && l.ArestasIds.Count == 1);
    }

    [Fact]
    public void Construir_DeveDistinguirAsArestasParalelasPeloTipoEPesoNaContribuicao_QuandoEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var paralelas = linhas.Where(l => l.NoOuAresta == $"{nos["S2"].Nome} → {nos["E1"].Nome} (externa)").ToList();
        paralelas.Should().HaveCount(2);
        paralelas.Select(l => l.Contribuicao).Should().BeEquivalentTo(
            "Aresta Assíncrona (peso 2) conectada à Banda Externo; contribui para o numerador de INT (Integração).",
            "Aresta Batch (peso 1) conectada à Banda Externo; contribui para o numerador de INT (Integração).");
    }

    // FLEX e AUT (4.4.2): uma linha por comunidade, com nós, saídas externas, autonomia e isolamento médio dos nós internos.
    [Fact]
    public void Construir_DeveListarUmaLinhaPorComunidadeComFlexEAut_QuandoEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var comunidades = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.Comunidade).ToList();
        comunidades.Should().HaveCount(3);
        comunidades.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.FLEX, Eixo.AUT }));
        linhas.Should().OnlyContain(l => l.NosIds.Count > 0); // toda linha é rastreável até um nó
        comunidades.Select(l => l.NosIds.Count).Should().Equal(6, 6, 3);

        // {TP2, TP3, P1, P2, P3, DS} (pedido, preparo e dados): 2 saídas externas ⇒ não autônoma; isolamento médio
        // (0,600 + 1,000 + 1,000 + 0,333) / 4 = 0,733 (Quadro 24).
        var pedido = comunidades.Single(l => l.NosIds.Contains(nos["P2"].Id));
        pedido.Contribuicao.Should().Contain("2 saída(s) externa(s)").And.Contain("não autônoma").And.Contain("0,733");
        pedido.ArestasIds.Should().HaveCount(2);

        // {P5, S2, E1} (pagamento): 1 saída (P5 → DS) ⇒ autônoma; internos P5 (0,5) e S2 (1,0) ⇒ média 0,750.
        var pagamento = comunidades.Single(l => l.NosIds.Contains(nos["S2"].Id));
        pagamento.Contribuicao.Should().Contain("1 saída(s) externa(s)").And.Contain("autônoma (no máximo uma saída)").And.Contain("0,750");
    }

    [Fact]
    public void Construir_DeveListarUmaLinhaDeDepPorFluxoValido_QuandoEstudoDeCaso2()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var fluxos = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.Fluxo).ToList();
        fluxos.Should().HaveCount(5);
        fluxos.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.DEP }));
        fluxos.Select(l => l.Contribuicao).Should().Contain(c => c.StartsWith("Percorre 5 banda(s) distinta(s)"));
        fluxos.Should().Contain(l => l.Contribuicao.Contains("sobre 5 fluxo(s) válido(s)"));
    }

    [Fact]
    public void Construir_NaoDeveListarFluxoInvalido_QuandoAArestaDoFluxoFoiRemovida()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        estudoDeCaso.RemoverAresta(estudoDeCaso.Arestas.Single(a => a.NoOrigemId == nos["S1"].Id && a.NoDestinoId == nos["DS"].Id).Id);
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        linhas.Count(l => l.Categoria == CategoriaDeRastreabilidade.Fluxo).Should().Be(3); // F1 perdeu a etapa S1 → DS
    }

    // UX é o grau de entrada médio dos Touchpoints: no CE2, só TP4 (canal de notificação, 2 entradas) fica acima da média 1,25.
    [Fact]
    public void Construir_DeveListarOTouchpointComGrauDeEntradaAcimaDaMedia_QuandoEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var touchpoints = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.NoFrontstage).ToList();
        var linha = touchpoints.Should().ContainSingle().Subject;
        linha.NosIds.Should().Equal(nos["TP4"].Id);
        linha.Eixos.Should().Equal(Eixo.UX);
        linha.Contribuicao.Should().Contain("Grau de entrada 2").And.Contain("média dos Touchpoints (1,25)");
    }

    // Um gargalo em banda que não alimenta nenhum eixo aparece como ponto de atenção, com a lista de eixos vazia.
    [Fact]
    public void Construir_DeveListarOGargaloDeBandaForaDoDispSemEixos_QuandoExternoTemUmNoDominante()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Integrações concentradas");
        var cliente = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var hub = estudoDeCaso.AdicionarNo("Hub de integração", TipoDeNo.Externo);
        var a = estudoDeCaso.AdicionarNo("Parceiro A", TipoDeNo.Externo);
        var b = estudoDeCaso.AdicionarNo("Parceiro B", TipoDeNo.Externo);
        var c = estudoDeCaso.AdicionarNo("Parceiro C", TipoDeNo.Externo);
        estudoDeCaso.AdicionarAresta(cliente.Id, hub.Id, TipoDeAresta.Sincrona, 3, "Cliente usa o hub");
        estudoDeCaso.AdicionarAresta(hub.Id, a.Id, TipoDeAresta.Sincrona, 3, "Hub chama A");
        estudoDeCaso.AdicionarAresta(hub.Id, b.Id, TipoDeAresta.Sincrona, 3, "Hub chama B");
        estudoDeCaso.AdicionarAresta(hub.Id, c.Id, TipoDeAresta.Sincrona, 3, "Hub chama C");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var gargalo = linhas.Single(l => l.Categoria == CategoriaDeRastreabilidade.Gargalo);
        gargalo.NosIds.Should().Equal(hub.Id);
        gargalo.Eixos.Should().BeEmpty();
        gargalo.Contribuicao.Should().Contain("fora do DISP (banda Externo)");
    }

    // CO (4.4.2): CO = N + 2 × |bandas ocupadas| — uma linha por Banda ocupada, com os nós que ela soma a N.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Construir_DeveListarUmaLinhaDeCoPorBandaOcupada_ComOsNosDaBanda(int estudo)
    {
        var (estudoDeCaso, _) = estudo == 1 ? EstudosDeCasoDeReferencia.CriarEstudoDeCaso1() : EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasDeCo = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.BandaOcupada).ToList();
        linhasDeCo.Should().HaveCount(contexto.BandasOcupadas.Count);
        linhasDeCo.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.CO }));
        linhasDeCo.Sum(l => l.NosIds.Count).Should().Be(contexto.N);
        foreach (var banda in contexto.BandasOcupadas)
        {
            var nosDaBanda = estudoDeCaso.Nos.Where(n => n.Banda == banda).Select(n => n.Id).ToList();
            linhasDeCo.Should().ContainSingle(l => l.Elemento == $"Banda {banda}").Which.NosIds.Should().BeEquivalentTo(nosDaBanda);
        }
    }

    // ESC (4.4.2): por Banda, pares de nós sem aresta entre eles / pares possíveis. Banda com menos de 2 nós não gera linha.
    [Fact]
    public void Construir_DeveListarUmaLinhaDeEscPorBandaComDoisOuMaisNos_ComOsParesSemAresta()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Escalabilidade por banda");
        var cliente = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator);
        var a = estudoDeCaso.AdicionarNo("Serviço A", TipoDeNo.Processo);
        var b = estudoDeCaso.AdicionarNo("Serviço B", TipoDeNo.Processo);
        var c = estudoDeCaso.AdicionarNo("Serviço C", TipoDeNo.Processo);
        estudoDeCaso.AdicionarAresta(cliente.Id, a.Id, TipoDeAresta.Sincrona, 1, "Cliente aciona A");
        estudoDeCaso.AdicionarAresta(a.Id, b.Id, TipoDeAresta.Sincrona, 1, "A chama B");
        estudoDeCaso.AdicionarAresta(b.Id, a.Id, TipoDeAresta.Assincrona, 1, "B avisa A");
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linha = linhas.Single(l => l.Categoria == CategoriaDeRastreabilidade.ParesDaBanda);
        linha.Eixos.Should().Equal(Eixo.ESC);
        linha.Elemento.Should().Be("Banda Backstage");
        linha.NosIds.Should().BeEquivalentTo(new[] { a.Id, b.Id, c.Id });
        linha.Contribuicao.Should().Contain("3 pares possíveis").And.Contain("1 com aresta").And.Contain("2 sem aresta");
    }

    // VOL (4.4.2): VOL = Σ w(e) / M — cada aresta contribui com o próprio peso.
    [Fact]
    public void Construir_DeveListarUmaLinhaDeVolPorAresta_ComOPesoQueElaSoma()
    {
        var (estudoDeCaso, _) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        var linhas = ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);

        var linhasDeVol = linhas.Where(l => l.Categoria == CategoriaDeRastreabilidade.VolumeDaAresta).ToList();
        linhasDeVol.Should().HaveCount(estudoDeCaso.Arestas.Count);
        linhasDeVol.Should().OnlyContain(l => l.Eixos.SequenceEqual(new[] { Eixo.VOL }));
        linhasDeVol.SelectMany(l => l.ArestasIds).Should().BeEquivalentTo(estudoDeCaso.Arestas.Select(a => a.Id));
        var critica = estudoDeCaso.Arestas.Single(a => a.Tipo == TipoDeAresta.Critica);
        linhasDeVol.Single(l => l.ArestasIds.Contains(critica.Id)).Contribuicao.Should().Contain($"peso {critica.Peso}");
    }
}
