using Sbar.Application.Grafo;
using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;
using Sbar.Domain.Services;

namespace Sbar.Application.Rastreabilidade;

// RF12 — Matriz de Rastreabilidade (TCC seção 4.6.1; exemplos nos Quadros 28 e 38): relaciona elemento do Blueprint → nó/aresta
// → métrica → eixo do Radar → como contribuiu. O TCC não define um algoritmo para "gerar" a matriz (os quadros são linhas
// escolhidas à mão); a leitura adotada aqui deriva as linhas das MESMAS definições das calculadoras de eixo, para que cada
// eixo liste os nós e arestas que de fato contribuíram (US07):
//   gargalo de qualquer banda (DISP só para Backstage/Suporte; UX para Frontstage), aresta CRI (TR), aresta externa (INT),
//   comunidade (FLEX, AUT), fluxo principal válido (DEP), Touchpoint de grau de entrada acima da média (UX), Banda ocupada (CO),
//   Banda com ao menos dois nós (ESC) e cada aresta pelo seu peso (VOL).
// A coluna "elemento/etapa do Blueprint" do TCC é aproximada por Elemento = "Nome (Banda)" — não há campo de etapa no Nó.
public static class ConstrutorDeMatrizDeRastreabilidade
{
    public static IReadOnlyList<LinhaDeRastreabilidade> Construir(ContextoDoMapa contexto)
    {
        var estudoDeCaso = contexto.EstudoDeCaso;
        var nos = estudoDeCaso.Nos.ToDictionary(n => n.Id);
        var linhas = new List<LinhaDeRastreabilidade>();

        var gargalosDeFrontstage = new HashSet<Guid>();

        // Gargalos de qualquer banda — o eixo depende da fórmula de cada um (seção 4.4.2).
        foreach (var no in estudoDeCaso.Nos.Where(n => contexto.Gargalos.Contains(n.Id)))
        {
            var estatisticas = contexto.EstatisticasPorBanda[no.Banda];
            var limiar = FormatadorDeNumeros.Formatar(estatisticas.Media + estatisticas.DesvioPadrao, 2);
            var carga = contexto.Cargas[no.Id];
            var inicio = $"Carga {carga} acima do limiar da Banda {no.Banda} (μ+σ = {limiar})";

            if (no.Banda is Banda.Backstage or Banda.Suporte)
            {
                linhas.Add(LinhaDoGargalo(no, "carga(v), gargalo(v)", [Eixo.DISP], $"{inicio}; reduz a Disponibilidade calculada."));
            }
            else if (no.Banda == Banda.Frontstage)
            {
                gargalosDeFrontstage.Add(no.Id);
                linhas.Add(LinhaDoGargalo(
                    no, "carga(v), gargalo(v), grau⁻(v)", [Eixo.UX],
                    $"{inicio}; concentra interações do Frontstage (grau de entrada {contexto.GrausEntrada[no.Id]}) e alimenta UX. Não entra em DISP, que só considera Backstage e Suporte."));
            }
            else
            {
                linhas.Add(LinhaDoGargalo(
                    no, "carga(v), gargalo(v)", [],
                    $"{inicio}; fora do DISP (banda {no.Banda}) — ponto de atenção, sem eixo que o agregue."));
            }
        }

        // Arestas Críticas — mesma definição de CalculadoraTR.
        foreach (var aresta in estudoDeCaso.Arestas.Where(a => a.Tipo == TipoDeAresta.Critica))
        {
            var origem = nos[aresta.NoOrigemId].Nome;
            var destino = nos[aresta.NoDestinoId].Nome;

            linhas.Add(new LinhaDeRastreabilidade(
                Elemento: $"{origem} → {destino}",
                NoOuAresta: $"{origem} → {destino} (crítica)",
                Metrica: "tipo(e), w(e)",
                Eixos: [Eixo.TR],
                Contribuicao: $"Aresta crítica de peso {aresta.Peso}; contribui para o numerador de TR (Tempo Real).",
                Categoria: CategoriaDeRastreabilidade.ArestaCritica,
                NosIds: [aresta.NoOrigemId, aresta.NoDestinoId],
                ArestasIds: [aresta.Id]));
        }

        // Arestas Externas — mesma definição de CalculadoraINT (liga a algum Nó da Banda Externo).
        foreach (var aresta in estudoDeCaso.Arestas.Where(a =>
            nos[a.NoOrigemId].Banda == Banda.Externo || nos[a.NoDestinoId].Banda == Banda.Externo))
        {
            var origem = nos[aresta.NoOrigemId].Nome;
            var destino = nos[aresta.NoDestinoId].Nome;

            linhas.Add(new LinhaDeRastreabilidade(
                Elemento: $"{origem} → {destino}",
                NoOuAresta: $"{origem} → {destino} (externa)",
                Metrica: "E_Externo / M",
                Eixos: [Eixo.INT],
                Contribuicao: $"Aresta {RotuloDoTipo(aresta.Tipo)} (peso {aresta.Peso}) conectada à Banda Externo; contribui para o numerador de INT (Integração).",
                Categoria: CategoriaDeRastreabilidade.ArestaExterna,
                NosIds: [aresta.NoOrigemId, aresta.NoDestinoId],
                ArestasIds: [aresta.Id]));
        }

        linhas.AddRange(LinhasDeComunidades(contexto, nos));
        linhas.AddRange(LinhasDeFluxos(contexto, nos));
        linhas.AddRange(LinhasDeTouchpoints(contexto, gargalosDeFrontstage));
        linhas.AddRange(LinhasDeBandasOcupadas(contexto));
        linhas.AddRange(LinhasDeParesDaBanda(contexto));
        linhas.AddRange(LinhasDeVolume(contexto, nos));

        return linhas;
    }

    private static LinhaDeRastreabilidade LinhaDoGargalo(No no, string metrica, IReadOnlyList<Eixo> eixos, string contribuicao) =>
        new(
            Elemento: $"{no.Nome} ({no.Banda})",
            NoOuAresta: $"{no.Nome} (gargalo)",
            Metrica: metrica,
            Eixos: eixos,
            Contribuicao: contribuicao,
            Categoria: CategoriaDeRastreabilidade.Gargalo,
            NosIds: [no.Id],
            ArestasIds: []);

    // FLEX e AUT (4.4.2): uma linha por comunidade — nós, saídas externas, autonomia e isolamento médio dos nós internos.
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeComunidades(ContextoDoMapa contexto, Dictionary<Guid, No> nos)
    {
        var indices = contexto.Comunidades.IndicePorNo;
        var arestas = contexto.EstudoDeCaso.Arestas;

        for (var i = 0; i < contexto.Comunidades.Membros.Count; i++)
        {
            var numero = i + 1;
            var membros = contexto.Comunidades.Membros[i];
            var nomes = membros.Select(id => nos[id].Nome).ToList();

            var saidas = arestas.Where(a => indices[a.NoOrigemId] == numero && indices[a.NoDestinoId] != numero).ToList();
            var autonoma = saidas.Count <= 1;

            var isolamentos = membros
                .Where(id => nos[id].Banda is Banda.Backstage or Banda.Suporte)
                .Select(id => Isolamento(id, contexto))
                .ToList();
            var textoDoIsolamento = isolamentos.Count == 0
                ? "sem nós internos (Backstage/Suporte): não entra na média de FLEX"
                : $"isolamento médio dos nós internos = {FormatadorDeNumeros.Formatar(isolamentos.Average(), 3)}";

            yield return new LinhaDeRastreabilidade(
                Elemento: $"Comunidade C{numero} ({string.Join(", ", nomes)})",
                NoOuAresta: $"Comunidade C{numero} ({membros.Count} nó{(membros.Count == 1 ? "" : "s")})",
                Metrica: "isolamento(v); saidas_externas(c)",
                Eixos: [Eixo.FLEX, Eixo.AUT],
                Contribuicao: $"{membros.Count} nó(s); {saidas.Count} saída(s) externa(s) — " +
                              $"{(autonoma ? "autônoma (no máximo uma saída)" : "não autônoma (mais de uma saída)")} para AUT; {textoDoIsolamento}.",
                Categoria: CategoriaDeRastreabilidade.Comunidade,
                NosIds: membros,
                ArestasIds: saidas.Select(a => a.Id).ToList());
        }
    }

    // isolamento(v) = 1 − arestas_p/_outras_comunidades(v) / grau_total(v) (=1 se grau 0) — mesma definição de CalculadoraFLEX.
    private static double Isolamento(Guid noId, ContextoDoMapa contexto)
    {
        var grauTotal = contexto.GrausEntrada[noId] + contexto.GrausSaida[noId];
        if (grauTotal == 0)
            return 1;

        var indices = contexto.Comunidades.IndicePorNo;
        var externas = contexto.EstudoDeCaso.Arestas.Count(a =>
            (a.NoOrigemId == noId && indices[a.NoDestinoId] != indices[noId]) ||
            (a.NoDestinoId == noId && indices[a.NoOrigemId] != indices[noId]));

        return 1.0 - (double)externas / grauTotal;
    }

    // DEP (4.4.2): cada fluxo principal VÁLIDO contribui com o número de bandas distintas que percorre.
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeFluxos(ContextoDoMapa contexto, Dictionary<Guid, No> nos)
    {
        var validos = contexto.FluxosPrincipais;

        foreach (var fluxo in validos)
        {
            var bandas = fluxo.Caminho.Select(id => nos[id].Banda).Distinct().ToList();

            yield return new LinhaDeRastreabilidade(
                Elemento: $"Fluxo {fluxo.Nome}",
                NoOuAresta: $"{fluxo.Nome} (fluxo)",
                Metrica: "bandas_percorridas(f)",
                Eixos: [Eixo.DEP],
                Contribuicao: $"Percorre {fluxo.BandasPercorridas} banda(s) distinta(s) ({string.Join(", ", bandas)}); entra na média de DEP sobre {validos.Count} fluxo(s) válido(s).",
                Categoria: CategoriaDeRastreabilidade.Fluxo,
                NosIds: fluxo.Caminho.Distinct().ToList(),
                ArestasIds: []);
        }
    }

    // UX (4.4.2): UX é o grau de entrada médio dos Touchpoints; os de grau acima da média são os que a elevam.
    // Gargalos de Frontstage já geram linha própria (com o eixo UX) e não se repetem aqui.
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeTouchpoints(ContextoDoMapa contexto, HashSet<Guid> jaListados)
    {
        var frontstage = contexto.EstudoDeCaso.Nos.Where(n => n.Banda == Banda.Frontstage).ToList();
        if (frontstage.Count == 0)
            yield break;

        var media = frontstage.Average(n => (double)contexto.GrausEntrada[n.Id]);

        foreach (var no in frontstage.Where(n => !jaListados.Contains(n.Id) && contexto.GrausEntrada[n.Id] > media))
        {
            yield return new LinhaDeRastreabilidade(
                Elemento: $"{no.Nome} ({no.Banda})",
                NoOuAresta: $"{no.Nome} (touchpoint)",
                Metrica: "grau⁻(v)",
                Eixos: [Eixo.UX],
                Contribuicao: $"Grau de entrada {contexto.GrausEntrada[no.Id]} acima da média dos Touchpoints ({FormatadorDeNumeros.Formatar(media, 2)}); eleva UX.",
                Categoria: CategoriaDeRastreabilidade.NoFrontstage,
                NosIds: [no.Id],
                ArestasIds: []);
        }
    }

    // Eixo 1 — Complexidade Operacional (CO), seção 4.4.2: CO = N + 2 × |bandas ocupadas|; cada Banda ocupada soma seus nós a N e 2 ao total.
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeBandasOcupadas(ContextoDoMapa contexto)
    {
        foreach (var banda in Enum.GetValues<Banda>().Where(contexto.BandasOcupadas.Contains))
        {
            var membros = contexto.EstudoDeCaso.Nos.Where(n => n.Banda == banda).ToList();

            yield return new LinhaDeRastreabilidade(
                Elemento: $"Banda {banda}",
                NoOuAresta: $"Banda {banda} ({membros.Count} nó{(membros.Count == 1 ? "" : "s")})",
                Metrica: "N, |bandas ocupadas|",
                Eixos: [Eixo.CO],
                Contribuicao: $"Soma {membros.Count} nó(s) a N e 2 pela Banda ocupada: {membros.Count + 2} no total (CO = N + 2 × |bandas ocupadas|).",
                Categoria: CategoriaDeRastreabilidade.BandaOcupada,
                NosIds: membros.Select(n => n.Id).ToList(),
                ArestasIds: []);
        }
    }

    // Eixo 2 — Escalabilidade (ESC), seção 4.4.2: por Banda, pares de nós sem aresta entre eles / pares possíveis C(n_b,2).
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeParesDaBanda(ContextoDoMapa contexto)
    {
        var arestas = contexto.EstudoDeCaso.Arestas;

        foreach (var banda in Enum.GetValues<Banda>())
        {
            var membros = contexto.EstudoDeCaso.Nos.Where(n => n.Banda == banda).Select(n => n.Id).ToList();
            if (membros.Count < 2)
                continue;

            var internas = arestas.Where(a => membros.Contains(a.NoOrigemId) && membros.Contains(a.NoDestinoId)).ToList();
            var paresComAresta = internas.Select(a => a.NoOrigemId.CompareTo(a.NoDestinoId) <= 0 ? (a.NoOrigemId, a.NoDestinoId) : (a.NoDestinoId, a.NoOrigemId)).Distinct().Count();
            var pares = membros.Count * (membros.Count - 1) / 2;

            yield return new LinhaDeRastreabilidade(
                Elemento: $"Banda {banda}",
                NoOuAresta: $"Banda {banda} ({membros.Count} nós)",
                Metrica: "C(n_b,2), arestas_intra(b)",
                Eixos: [Eixo.ESC],
                Contribuicao: $"{pares} {(pares == 1 ? "par possível" : "pares possíveis")}, {paresComAresta} com aresta e {pares - paresComAresta} sem aresta; os pares sem aresta entram no numerador de ESC.",
                Categoria: CategoriaDeRastreabilidade.ParesDaBanda,
                NosIds: membros,
                ArestasIds: internas.Select(a => a.Id).ToList());
        }
    }

    // Eixo 9 — Volume de Comunicação (VOL), seção 4.4.2: VOL = Σ w(e) / M; cada aresta contribui com o próprio peso.
    private static IEnumerable<LinhaDeRastreabilidade> LinhasDeVolume(ContextoDoMapa contexto, Dictionary<Guid, No> nos)
    {
        foreach (var aresta in contexto.EstudoDeCaso.Arestas)
        {
            var origem = nos[aresta.NoOrigemId].Nome;
            var destino = nos[aresta.NoDestinoId].Nome;

            yield return new LinhaDeRastreabilidade(
                Elemento: $"{origem} → {destino}",
                NoOuAresta: $"{origem} → {destino} (volume)",
                Metrica: "w(e), M",
                Eixos: [Eixo.VOL],
                Contribuicao: $"Aresta {RotuloDoTipo(aresta.Tipo)} de peso {aresta.Peso}; soma {aresta.Peso} ao numerador de VOL, dividido por M = {contexto.M}.",
                Categoria: CategoriaDeRastreabilidade.VolumeDaAresta,
                NosIds: [aresta.NoOrigemId, aresta.NoDestinoId],
                ArestasIds: [aresta.Id]);
        }
    }

    // Distingue arestas paralelas entre o mesmo par de nós (D6), que de outro modo gerariam linhas idênticas.
    private static string RotuloDoTipo(TipoDeAresta tipo) => tipo switch
    {
        TipoDeAresta.Sincrona => "Síncrona",
        TipoDeAresta.Assincrona => "Assíncrona",
        TipoDeAresta.Critica => "Crítica",
        TipoDeAresta.Batch => "Batch",
        _ => tipo.ToString(),
    };
}
