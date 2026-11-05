using FluentAssertions;
using Sbar.Application.Grafo;
using Sbar.Domain.Entities;
using Xunit;

namespace Sbar.Application.Tests.Grafo;

// CT02 (TCC revisado, Quadro 14) e seção 4.4.1: detecção de comunidades por modularidade (CNM guloso).
// Oráculo: partições e Q de docs/dominio/estudos-de-caso.md (Quadros 24 e 32 do TCC de 2026-10-25). O TCC explora, a cada passo,
// TODAS as fusões empatadas e adota, entre as divisões finais, a de maior Q — por isso o resultado não depende da ordem de cadastro.
public class DetectorDeComunidadesTests
{
    private static readonly string[] OrdemDosQuadros20E31 =
        ["A", "TP1", "TP2", "TP3", "TP4", "P1", "P2", "P3", "P4", "P5", "S1", "S2", "DS", "E1", "E2"];

    private static readonly string[] ParticaoDoTccCe1 = ["A,E2,P4,S1,TP1,TP4", "DS,P1,P2,P3,TP2,TP3", "E1,P5,S2"];
    private static readonly string[] ParticaoDoTccCe2 = ["A,E2,P5,TP1", "DS,P1,S1", "E1,P4,S2,TP4", "P2,P3,TP2,TP3"];
    // A outra divisão do Estudo de Caso 1, obtida por um desempate diferente (Q menor): o TCC a cita e não a adota.
    private static readonly string[] OutraParticaoCe1 = ["A,E2,P4,TP1,TP4", "DS,P1,S1,TP2,TP3", "E1,P5,S2", "P2,P3"];

    // Estudo de Caso 1/2 como lista de arestas (códigos) — mesmas arestas das fixtures, para alimentar a
    // implementação de referência dos testes.
    private static IReadOnlyList<(string Origem, string Destino, int Peso)> ArestasDe(
        EstudoDeCaso estudoDeCaso, Dictionary<string, No> nos)
    {
        var codigoPorId = nos.ToDictionary(p => p.Value.Id, p => p.Key);
        return estudoDeCaso.Arestas.Select(a => (codigoPorId[a.NoOrigemId], codigoPorId[a.NoDestinoId], a.Peso.Valor)).ToList();
    }

    // Partição como conjunto de "A,B,C" (códigos ordenados) — independe da numeração das comunidades.
    private static string[] ComoCodigos(ResultadoDeComunidades resultado, Dictionary<string, No> nos)
    {
        var codigoPorId = nos.ToDictionary(p => p.Value.Id, p => p.Key);
        return resultado.Membros
            .Select(m => string.Join(",", m.Select(id => codigoPorId[id]).Order(StringComparer.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    [Fact]
    public void Detectar_DeveReproduzirParticaoEModularidadeDoTcc_QuandoForEstudoDeCaso1()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();

        var resultado = DetectorDeComunidades.Detectar(estudoDeCaso);

        resultado.Quantidade.Should().Be(3);
        ComoCodigos(resultado, nos).Should().Equal(ParticaoDoTccCe1);
        resultado.ModularidadeQ.Should().BeApproximately(0.4117, 0.00005);
        resultado.Avisos.Should().BeEmpty(); // Q > 0,3 e mais de uma comunidade
    }

    [Fact]
    public void Detectar_DeveReproduzirParticaoEModularidadeDoTcc_QuandoForEstudoDeCaso2()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();

        var resultado = DetectorDeComunidades.Detectar(estudoDeCaso);

        resultado.Quantidade.Should().Be(4);
        ComoCodigos(resultado, nos).Should().Equal(ParticaoDoTccCe2);
        resultado.ModularidadeQ.Should().BeApproximately(0.3440, 0.00005); // CT02: quatro comunidades, Q ≈ 0,34
        resultado.Avisos.Should().BeEmpty();
    }

    [Fact]
    public void Detectar_DeveNumerarComunidadesPelaMenorPosicaoDeCadastro_E_ListarMembrosEmOrdemDeCadastro()
    {
        var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();

        var resultado = DetectorDeComunidades.Detectar(estudoDeCaso);

        // 1 = {A,...} (A é o nó 0); 2 = {TP2,...} (TP2 é o nó 2); 3 = {P5,S2,E1} (P5 é o nó 9)
        resultado.IndicePorNo[nos["A"].Id].Should().Be(1);
        resultado.IndicePorNo[nos["TP2"].Id].Should().Be(2);
        resultado.IndicePorNo[nos["P5"].Id].Should().Be(3);
        resultado.Membros[2].Should().Equal(nos["P5"].Id, nos["S2"].Id, nos["E1"].Id);
        resultado.IndicePorNo.Should().HaveCount(15);
    }

    // D9 (resolvido no TCC de 2026-10-25, 4.4.1): nos dois estudos de caso há empates exatos de ΔQ, e a exploração de todas as
    // fusões empatadas gera duas divisões finais; adota-se a de maior Q. Aqui se prova que o empate existe e que a escolha é essa.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Detectar_DeveAdotarADivisaoDeMaiorQ_EntreAsObtidasAoExplorarOsEmpates(int estudo)
    {
        var (estudoDeCaso, nos) = estudo == 1 ? EstudosDeCasoDeReferencia.CriarEstudoDeCaso1() : EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var arestas = ArestasDe(estudoDeCaso, nos);

        var divisoes = CnmDeReferencia.DivisoesFinais(OrdemDosQuadros20E31, arestas);

        divisoes.Should().HaveCount(2, "o TCC relata duas divisões possíveis em cada estudo de caso");
        var melhor = divisoes.MaxBy(d => d.Numerador)!;
        divisoes.Count(d => d.Numerador == melhor.Numerador).Should().Be(1);
        melhor.Particao.Should().Equal(estudo == 1 ? ParticaoDoTccCe1 : ParticaoDoTccCe2);
        ComoCodigos(DetectorDeComunidades.Detectar(estudoDeCaso), nos).Should().Equal(melhor.Particao);
        if (estudo == 1)
            divisoes.Select(d => d.Particao).Should().ContainEquivalentOf(OutraParticaoCe1);
    }

    // RNF02: o procedimento do TCC "não depende da ordem em que os nós são listados": cadastrar os nós em outra ordem dá a mesma divisão.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Detectar_DeveDarAMesmaDivisao_QuandoOsNosSaoCadastradosEmOutraOrdem(int estudo)
    {
        var (original, nosOriginais) = estudo == 1 ? EstudosDeCasoDeReferencia.CriarEstudoDeCaso1() : EstudosDeCasoDeReferencia.CriarEstudoDeCaso2();
        var arestas = ArestasDe(original, nosOriginais);
        var esperado = ComoCodigos(DetectorDeComunidades.Detectar(original), nosOriginais);
        var sorteio = new Random(20261025);

        for (var rodada = 0; rodada < 30; rodada++)
        {
            var embaralhados = OrdemDosQuadros20E31.OrderBy(_ => sorteio.Next()).ToArray();
            var (estudoDeCaso, nos) = EstudosDeCasoDeReferencia.Montar(embaralhados, arestas);

            ComoCodigos(DetectorDeComunidades.Detectar(estudoDeCaso), nos).Should().Equal(esperado, $"ordem #{rodada}: {string.Join(" ", embaralhados)}");
        }
    }

    [Fact]
    public void Detectar_DeveSerEstavel_QuandoRepetidoEQuandoAsArestasSaoCadastradasEmOutraOrdem()
    {
        var (estudoOriginal, nosOriginais) = EstudosDeCasoDeReferencia.CriarEstudoDeCaso1();
        var arestas = ArestasDe(estudoOriginal, nosOriginais);
        var esperado = ComoCodigos(DetectorDeComunidades.Detectar(estudoOriginal), nosOriginais);

        for (var rodada = 0; rodada < 20; rodada++)
        {
            // Mesmos nós na mesma ordem de cadastro; só a ordem de cadastro das arestas muda.
            var embaralhadas = arestas.OrderBy(_ => Random.Shared.Next()).ToList();
            var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(OrdemDosQuadros20E31, embaralhadas);

            ComoCodigos(DetectorDeComunidades.Detectar(estudo), nos).Should().Equal(esperado);
        }
    }

    // Propriedade: a divisão do produto é uma das de maior Q obtidas pela implementação de referência dos testes
    // (força bruta sobre todas as fusões empatadas) em grafos aleatórios.
    [Fact]
    public void Detectar_DeveCoincidirComAImplementacaoDeReferencia_EmGrafosAleatorios()
    {
        var sorteio = new Random(20261021);

        for (var caso = 0; caso < 60; caso++)
        {
            var quantidade = sorteio.Next(3, 11);
            var codigos = Enumerable.Range(0, quantidade).Select(i => $"N{i:00}").ToArray();
            var arestas = new List<(string, string, int)>();
            for (var i = 0; i < quantidade; i++)
                for (var j = 0; j < quantidade; j++)
                    if (i != j && sorteio.NextDouble() < 0.3)
                        arestas.Add((codigos[i], codigos[j], sorteio.Next(1, 4)));

            var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(codigos, arestas);

            var divisoes = CnmDeReferencia.DivisoesFinais(codigos, arestas);
            var melhor = divisoes.Max(d => d.Numerador);
            var empatadas = divisoes.Where(d => d.Numerador == melhor).ToList();
            var resultado = DetectorDeComunidades.Detectar(estudo);
            var obtida = ComoCodigos(resultado, nos);

            empatadas.Select(d => d.Particao).Should().ContainEquivalentOf(obtida, $"grafo aleatório #{caso}: a divisão adotada é de maior Q");
            if (empatadas.Count > 1)
                resultado.Avisos.Should().Contain(a => a.Contains("empate"), $"grafo aleatório #{caso}: empate de Q entre divisões finais deve ser avisado");
        }
    }

    [Fact]
    public void Detectar_DeveSomarArestasParalelasENosDoisSentidos()
    {
        // A→B (1), B→A (2) e A→B de novo (1): peso A–B = 4. C–D = 2 + 2 = 4 também; dois pares isolados.
        var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(
            ["A", "B", "C", "D"], [("A", "B", 1), ("B", "A", 2), ("A", "B", 1), ("C", "D", 2), ("D", "C", 2)]);

        var resultado = DetectorDeComunidades.Detectar(estudo);

        ComoCodigos(resultado, nos).Should().Equal("A,B", "C,D");
        resultado.ModularidadeQ.Should().BeApproximately(0.5, 1e-12);
    }

    [Fact]
    public void Detectar_DeveDevolverQNuloEAviso_QuandoOMapaNaoTemArestas()
    {
        var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(["A", "B", "C"], []);

        var resultado = DetectorDeComunidades.Detectar(estudo);

        resultado.ModularidadeQ.Should().BeNull();
        resultado.Quantidade.Should().Be(3); // cada nó é uma comunidade própria
        resultado.IndicePorNo[nos["B"].Id].Should().Be(2);
        resultado.Avisos.Should().ContainSingle().Which.Should().Contain("m = 0");
    }

    [Fact]
    public void Detectar_DeveRetornarVazio_QuandoOMapaNaoTemNos()
    {
        var estudo = EstudoDeCaso.Criar("Vazio");

        var resultado = DetectorDeComunidades.Detectar(estudo);

        resultado.Quantidade.Should().Be(0);
        resultado.ModularidadeQ.Should().BeNull();
        resultado.Avisos.Should().BeEmpty();
    }

    [Fact]
    public void Detectar_DeveAvisarComunidadeUnica_QuandoGrafoEEstrela()
    {
        var (estudo, _) = EstudosDeCasoDeReferencia.Montar(
            ["C", "L1", "L2", "L3", "L4"], [("C", "L1", 2), ("C", "L2", 2), ("C", "L3", 2), ("C", "L4", 2)]);

        var resultado = DetectorDeComunidades.Detectar(estudo);

        resultado.Quantidade.Should().Be(1);
        resultado.ModularidadeQ.Should().BeApproximately(0.0, 1e-12);
        resultado.Avisos.Should().Contain(a => a.Contains("única comunidade"));
        resultado.Avisos.Should().Contain(a => a.Contains("não significativa"));
    }

    [Fact]
    public void Detectar_DeveAvisarEstruturaNaoSignificativa_QuandoQEstiverAbaixoDeZeroVirgulaTres()
    {
        // Exemplo didático de passo-a-passo-sbar.md (newsletter): Q ≈ 0,1746 com três comunidades.
        var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(
            ["A", "TP1", "TP2", "P1", "DS", "E1"],
            [("A", "TP1", 3), ("TP1", "P1", 3), ("P1", "DS", 3), ("P1", "E1", 2), ("E1", "TP2", 1), ("TP2", "A", 1)]);

        var resultado = DetectorDeComunidades.Detectar(estudo);

        ComoCodigos(resultado, nos).Should().Equal("A,TP1", "DS,P1", "E1,TP2");
        resultado.ModularidadeQ.Should().BeApproximately(0.1746, 0.00005);
        resultado.Avisos.Should().ContainSingle().Which.Should().Contain("não significativa");
    }

    [Fact]
    public void Detectar_DeveTratarNoSemArestasComoComunidadeProprias_QuandoOutrosNosTemArestas()
    {
        var (estudo, nos) = EstudosDeCasoDeReferencia.Montar(["A", "B", "Isolado"], [("A", "B", 3)]);

        var resultado = DetectorDeComunidades.Detectar(estudo);

        ComoCodigos(resultado, nos).Should().Equal("A,B", "Isolado");
    }

    // Referência dos testes: o CNM do TCC por força bruta — a cada passo ramifica em TODAS as fusões de ganho máximo e devolve as
    // divisões finais distintas com o numerador inteiro exato de Q (Σ_c 4m·W_c − K_c²; Q = numerador / 4m²).
    private static class CnmDeReferencia
    {
        public sealed record Divisao(string[] Particao, long Numerador);

        public static IReadOnlyList<Divisao> DivisoesFinais(
            IReadOnlyList<string> nos,
            IReadOnlyList<(string Origem, string Destino, int Peso)> arestas)
        {
            var posicao = nos.Select((c, i) => (c, i)).ToDictionary(t => t.c, t => t.i);
            var pesoDoPar = new Dictionary<(int, int), long>();
            var k = new long[nos.Count];
            long m = 0;

            foreach (var (origem, destino, peso) in arestas)
            {
                var (a, b) = (posicao[origem], posicao[destino]);
                var chave = a < b ? (a, b) : (b, a);
                pesoDoPar[chave] = pesoDoPar.GetValueOrDefault(chave) + peso;
                k[a] += peso;
                k[b] += peso;
                m += peso;
            }

            var finais = new Dictionary<string, Divisao>();
            var visitados = new HashSet<string>();
            Explorar(Enumerable.Range(0, nos.Count).Select(i => new List<int> { i }).ToList());
            return finais.Values.ToList();

            void Explorar(List<List<int>> comunidades)
            {
                var chaveDoEstado = string.Join("|", comunidades.Select(c => string.Join(",", c.Order())).Order(StringComparer.Ordinal));
                if (!visitados.Add(chaveDoEstado))
                    return;

                var candidatas = new List<(long Ganho, int A, int B)>();
                for (var a = 0; a < comunidades.Count; a++)
                    for (var b = a + 1; b < comunidades.Count; b++)
                    {
                        long w = 0;
                        foreach (var i in comunidades[a])
                            foreach (var j in comunidades[b])
                                w += pesoDoPar.GetValueOrDefault(i < j ? (i, j) : (j, i));
                        if (w == 0)
                            continue;

                        var ka = comunidades[a].Sum(i => k[i]);
                        var kb = comunidades[b].Sum(i => k[i]);
                        candidatas.Add((2 * (2 * m * w - ka * kb), a, b));
                    }

                var maior = candidatas.Count == 0 ? 0 : candidatas.Max(c => c.Ganho);
                if (m == 0 || maior <= 0)
                {
                    long numerador = 0;
                    foreach (var c in comunidades)
                    {
                        long interno = 0;
                        foreach (var i in c)
                            foreach (var j in c.Where(j => j > i))
                                interno += pesoDoPar.GetValueOrDefault((i, j));
                        var kc = c.Sum(i => k[i]);
                        numerador += 4 * m * interno - kc * kc;
                    }

                    var particao = comunidades
                        .Select(c => string.Join(",", c.Select(i => nos[i]).Order(StringComparer.Ordinal)))
                        .Order(StringComparer.Ordinal)
                        .ToArray();
                    finais[string.Join("|", particao)] = new Divisao(particao, numerador);
                    return;
                }

                foreach (var (_, a, b) in candidatas.Where(c => c.Ganho == maior))
                {
                    var proximo = comunidades.Select(c => c.ToList()).ToList();
                    proximo[a].AddRange(proximo[b]);
                    proximo.RemoveAt(b);
                    Explorar(proximo);
                }
            }
        }
    }
}
