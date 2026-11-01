using System.Globalization;
using Sbar.Domain.Entities;

namespace Sbar.Application.Grafo;

public static class DetectorDeComunidades
{
    // Limiar de Q a partir do qual o TCC considera que há estrutura de comunidades significativa (4.4.1).
    public const double LimiarDeQSignificativo = 0.3;

    // Limite de estados distintos explorados ao ramificar nos empates; acima dele a exploração segue só o primeiro empate de cada
    // passo (determinístico) e o resultado traz um aviso. Os Estudos de Caso do TCC usam poucas dezenas de estados.
    private const int LimiteDeEstadosExplorados = 20_000;

    // comunidade(v) — TCC seção 4.4.1: comunidade estrutural obtida por modularidade, com o algoritmo guloso de Clauset, Newman e Moore
    // (2004) sobre a versão NÃO DIRECIONADA do Mapa, em que o peso A_ij da ligação entre dois nós é a soma das w(e) de todas as arestas
    // entre eles (paralelas e os dois sentidos somados). Cada nó começa numa comunidade; a cada passo unem-se as duas comunidades
    // CONECTADAS cuja fusão produz o maior ganho de modularidade ΔQ; o processo termina quando nenhuma fusão aumenta Q.
    //
    // ΔQ = 2·(e_ij − a_i·a_j), com e_ij = W_ij/(2m) e a_i = K_i/(2m) (W_ij = soma dos pesos entre as comunidades; K_i = soma dos pesos
    // das ligações dos seus nós; m = soma de todos os pesos). Multiplicando por (2m)² obtém-se um INTEIRO exato: 2·(2m·W_ij − K_i·K_j).
    // A comparação (e, principalmente, os empates) usa só esse inteiro — nunca ponto flutuante.
    //
    // Empates (decisão D9, resolvida no TCC de 2026-10-25): a descrição original do algoritmo não diz como escolher entre fusões de
    // mesmo ΔQ. O procedimento do TCC "explora, a cada passo, todas as fusões empatadas e, entre as divisões finais assim obtidas,
    // adota a de maior modularidade", de modo que o resultado não depende da ordem dos nós nem dos seus rótulos. Aqui cada estado
    // (partição) é explorado uma única vez, e o Q de cada divisão final é comparado como inteiro exato (Σ_c 4m·W_c − K_c²). Se duas
    // divisões finais distintas têm o MESMO Q — caso que o TCC não prevê —, adota-se a primeira na ordem de cadastro e o resultado
    // avisa, porque só nesse caso a ordem de cadastro ainda influi.
    public static ResultadoDeComunidades Detectar(EstudoDeCaso estudoDeCaso)
    {
        var nos = estudoDeCaso.Nos;
        var ordemPorNo = new Dictionary<Guid, int>(nos.Count);
        for (var i = 0; i < nos.Count; i++)
            ordemPorNo[nos[i].Id] = i;

        // A_ij (i < j, por posição de cadastro) e K_i
        var pesoEntrePares = new Dictionary<(int, int), long>();
        var grauPonderado = new long[nos.Count];
        long m = 0;

        foreach (var aresta in estudoDeCaso.Arestas)
        {
            var origem = ordemPorNo[aresta.NoOrigemId];
            var destino = ordemPorNo[aresta.NoDestinoId];
            if (origem == destino)
                continue; // self-loop não existe no domínio; ignorado por segurança

            var chavePar = origem < destino ? (origem, destino) : (destino, origem);
            pesoEntrePares[chavePar] = pesoEntrePares.GetValueOrDefault(chavePar) + aresta.Peso.Valor;
            grauPonderado[origem] += aresta.Peso.Valor;
            grauPonderado[destino] += aresta.Peso.Valor;
            m += aresta.Peso.Valor;
        }

        if (m == 0)
            return SemArestas(nos);

        // Estado = rótulo por nó; o rótulo de uma comunidade é a menor posição de cadastro dos seus nós (única por comunidade).
        var visitados = new HashSet<string>();
        var finais = new List<(int[] Rotulos, long Numerador)>();
        var limiteAtingido = false;

        Explorar(Enumerable.Range(0, nos.Count).ToArray());

        var melhorNumerador = finais.Max(f => f.Numerador);
        var melhores = finais
            .Where(f => f.Numerador == melhorNumerador)
            .OrderBy(f => string.Join(",", f.Rotulos.Select(r => r.ToString("D6", CultureInfo.InvariantCulture))), StringComparer.Ordinal)
            .ToList();

        var membrosPorComunidade = new Dictionary<int, List<int>>();
        for (var i = 0; i < nos.Count; i++)
        {
            var rotulo = melhores[0].Rotulos[i];
            if (!membrosPorComunidade.TryGetValue(rotulo, out var lista))
                membrosPorComunidade[rotulo] = lista = [];
            lista.Add(i);
        }

        var resultado = Montar(nos, membrosPorComunidade, pesoEntrePares, grauPonderado, m);

        var avisosExtras = new List<string>();
        if (melhores.Count > 1)
            avisosExtras.Add(string.Create(CultureInfo.InvariantCulture,
                $"Há {melhores.Count} divisões finais distintas com a mesma modularidade (Q = {resultado.ModularidadeQ:0.0000}), um empate que o TCC não prevê (4.4.1); foi adotada a primeira na ordem de cadastro, então a ordem de cadastro dos nós ainda influi neste resultado."));
        if (limiteAtingido)
            avisosExtras.Add($"A exploração dos empates de modularidade passou de {LimiteDeEstadosExplorados} estados e foi limitada; a divisão adotada pode não ser a de maior Q (TCC 4.4.1).");

        return avisosExtras.Count == 0 ? resultado : resultado with { Avisos = [.. resultado.Avisos, .. avisosExtras] };

        void Explorar(int[] rotulos)
        {
            if (!visitados.Add(string.Join(",", rotulos)))
                return;

            var k = new Dictionary<int, long>();
            for (var i = 0; i < rotulos.Length; i++)
                k[rotulos[i]] = k.GetValueOrDefault(rotulos[i]) + grauPonderado[i];

            // W entre cada par de comunidades conectadas (a < b) e W interno de cada comunidade
            var pesoEntreComunidades = new Dictionary<(int, int), long>();
            var pesoInterno = new Dictionary<int, long>();
            foreach (var ((i, j), peso) in pesoEntrePares)
            {
                var (a, b) = (rotulos[i], rotulos[j]);
                if (a == b)
                    pesoInterno[a] = pesoInterno.GetValueOrDefault(a) + peso;
                else
                {
                    var chave = a < b ? (a, b) : (b, a);
                    pesoEntreComunidades[chave] = pesoEntreComunidades.GetValueOrDefault(chave) + peso;
                }
            }

            var melhorGanho = long.MinValue;
            var empatadas = new List<(int A, int B)>();
            foreach (var ((a, b), peso) in pesoEntreComunidades)
            {
                var ganho = 2 * (2 * m * peso - k[a] * k[b]);
                if (ganho > melhorGanho)
                {
                    melhorGanho = ganho;
                    empatadas.Clear();
                }

                if (ganho == melhorGanho)
                    empatadas.Add((a, b));
            }

            if (empatadas.Count == 0 || melhorGanho <= 0)
            {
                long numerador = 0;
                foreach (var (rotulo, kc) in k)
                    numerador += 4 * m * pesoInterno.GetValueOrDefault(rotulo) - kc * kc;

                finais.Add((rotulos, numerador));
                return;
            }

            empatadas.Sort();
            if (visitados.Count >= LimiteDeEstadosExplorados)
            {
                limiteAtingido = limiteAtingido || empatadas.Count > 1;
                empatadas.RemoveRange(1, empatadas.Count - 1);
            }

            foreach (var (a, b) in empatadas)
            {
                var proximo = (int[])rotulos.Clone();
                for (var i = 0; i < proximo.Length; i++)
                    if (proximo[i] == b)
                        proximo[i] = a; // a < b: o rótulo continua sendo a menor posição de cadastro da comunidade
                Explorar(proximo);
            }
        }
    }

    private static ResultadoDeComunidades Montar(
        IReadOnlyList<No> nos,
        Dictionary<int, List<int>> membrosPorComunidade,
        Dictionary<(int, int), long> pesoEntrePares,
        long[] grauPonderado,
        long m)
    {
        // Numeração 1..n pela menor posição de cadastro (a chave do dicionário já é essa posição).
        var comunidades = membrosPorComunidade.OrderBy(c => c.Key).Select(c => c.Value.Order().ToList()).ToList();

        var indicePorNo = new Dictionary<Guid, int>();
        var membros = new List<IReadOnlyList<Guid>>();
        var comunidadeDaPosicao = new int[nos.Count];

        for (var c = 0; c < comunidades.Count; c++)
        {
            foreach (var posicao in comunidades[c])
            {
                indicePorNo[nos[posicao].Id] = c + 1;
                comunidadeDaPosicao[posicao] = c;
            }

            membros.Add(comunidades[c].Select(p => nos[p].Id).ToList());
        }

        // Q = Σ_c [ e_cc − a_c² ], e_cc = W_interno(c)/m (cada ligação conta nos dois sentidos de A_ij: 2·W/2m)
        // e a_c = K_c/(2m).
        var pesoInterno = new long[comunidades.Count];
        foreach (var ((i, j), peso) in pesoEntrePares)
            if (comunidadeDaPosicao[i] == comunidadeDaPosicao[j])
                pesoInterno[comunidadeDaPosicao[i]] += peso;

        var q = 0.0;
        for (var c = 0; c < comunidades.Count; c++)
        {
            var kc = comunidades[c].Sum(p => grauPonderado[p]);
            var eCc = (double)pesoInterno[c] / m;
            var aC = (double)kc / (2.0 * m);
            q += eCc - aC * aC;
        }

        return new ResultadoDeComunidades(indicePorNo, membros, q, AvisosDe(q, comunidades.Count, nos.Count));
    }

    // m = 0: Q é indefinido (0/0). Cada nó é tratado como uma comunidade própria (sem nenhuma aresta de
    // saída), o que torna FLEX e AUT triviais — por isso o aviso.
    private static ResultadoDeComunidades SemArestas(IReadOnlyList<No> nos)
    {
        var indicePorNo = new Dictionary<Guid, int>();
        var membros = new List<IReadOnlyList<Guid>>();

        for (var i = 0; i < nos.Count; i++)
        {
            indicePorNo[nos[i].Id] = i + 1;
            membros.Add([nos[i].Id]);
        }

        var avisos = new List<string>();
        if (nos.Count > 0)
            avisos.Add("O Mapa não tem arestas (m = 0): a modularidade Q é indefinida e cada nó é tratado como uma comunidade própria; FLEX e AUT não são conclusivos (TCC 4.4.1).");

        return new ResultadoDeComunidades(indicePorNo, membros, null, avisos);
    }

    private static List<string> AvisosDe(double q, int quantidadeDeComunidades, int quantidadeDeNos)
    {
        var avisos = new List<string>();

        if (quantidadeDeComunidades == 1 && quantidadeDeNos > 1)
            avisos.Add("O Mapa forma uma única comunidade: FLEX e AUT ficam degenerados (= 1) e não distinguem a estrutura do serviço (TCC 4.4.1).");

        if (q < LimiarDeQSignificativo)
            avisos.Add(string.Create(CultureInfo.InvariantCulture,
                $"Estrutura de comunidades não significativa: Q = {q:0.0000} < {LimiarDeQSignificativo:0.0} (TCC 4.4.1); interprete FLEX e AUT com cautela."));

        return avisos;
    }
}
