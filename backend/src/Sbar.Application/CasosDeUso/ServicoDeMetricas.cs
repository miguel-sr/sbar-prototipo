using Sbar.Application.Calculadoras;
using Sbar.Application.Comparacao;
using Sbar.Application.Grafo;
using Sbar.Application.Normalizacao;
using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.Metricas;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.CasosDeUso;

// RF-06/RF-07/RF-09 — orquestra: EstudoDeCaso → ContextoDoMapa → 10 calculadoras → Normalizador
// → ComparadorDePerfis → GeradorDeRecomendacao. Limites, pesos e assinaturas vêm de um conjunto de parâmetros
// versionado (RF11): o vigente ou, com versaoDosParametros, o snapshot de uma versão antiga (reaplicação, 5.4).
// Soma dos pesos ≠ 1 bloqueia o cálculo (RN07, CT10).
public class ServicoDeMetricas(
    IEstudoDeCasoRepository repositorio,
    IEnumerable<ICalculadoraDeEixo> calculadoras,
    IServicoDeParametros servicoDeParametros) : IServicoDeMetricas
{
    public async Task<ResultadoDeMetricas> CalcularAsync(Guid estudoDeCasoId, int? versaoDosParametros = null)
    {
        var estudoDeCaso = await repositorio.ObterPorIdAsync(estudoDeCasoId)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), estudoDeCasoId);

        var parametros = await servicoDeParametros.ObterAsync(versaoDosParametros);
        parametros.ValidarSomaDosPesos();

        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);
        var limites = parametros.Limites;

        var eixos = new List<ResultadoDeEixo>();

        foreach (var calculadora in calculadoras)
        {
            var bruto = calculadora.Calcular(contexto);

            if (!bruto.Aplicavel || bruto.Valor is null)
            {
                eixos.Add(new ResultadoDeEixo(calculadora.Eixo, false, null, null, bruto.Motivo));
                continue;
            }

            var normalizado = Normalizador.Normalizar(bruto.Valor.Value, limites[calculadora.Eixo]);
            eixos.Add(new ResultadoDeEixo(calculadora.Eixo, true, bruto.Valor, normalizado, null));
        }

        var recomendacao = GerarRecomendacao(eixos, parametros);

        var nosGargalo = estudoDeCaso.Nos
            .Where(n => contexto.Gargalos.Contains(n.Id))
            .Select(n => n.Id)
            .ToList();

        var comunidades = contexto.Comunidades.Membros
            .Select((membros, i) => new ComunidadeDoResultado(i + 1, membros, SaidasExternasDa(i + 1, contexto)))
            .ToList();

        var bandas = contexto.EstatisticasPorBanda
            .OrderBy(p => p.Key)
            .Select(p => new EstatisticaDaBandaDoResultado(
                p.Key,
                estudoDeCaso.Nos.Count(n => n.Banda == p.Key),
                p.Value.Media,
                p.Value.DesvioPadrao,
                p.Value.Media + p.Value.DesvioPadrao))
            .ToList();

        var fluxos = contexto.Fluxos
            .Select(f => new FluxoDoResultado(f.Id, f.Nome, f.Valido, f.BandasPercorridas))
            .ToList();

        var avisos = contexto.Comunidades.Avisos.ToList();
        var invalidos = contexto.Fluxos.Count(f => !f.Valido);
        if (invalidos > 0)
            avisos.Add($"{invalidos} fluxo(s) principal(is) inválido(s) — sem aresta em alguma etapa — ficam fora do cálculo de DEP (TCC 4.2.7).");

        avisos.AddRange(AvisosDeNosSemLigacao(estudoDeCaso, contexto));
        avisos.AddRange(AvisosDeEixosNaoAplicaveis(eixos, parametros, recomendacao));

        return new ResultadoDeMetricas(
            eixos, recomendacao, nosGargalo, comunidades, contexto.Comunidades.ModularidadeQ, avisos, bandas, fluxos, parametros.Versao);
    }

    // Nó sem arestas (com m > 0) — casos-limite de formulas-metricas.md e passo 7 do TCC (4.2.7): um Mapa validado não
    // deve ter nós soltos. Cada um vira uma comunidade própria com 0 saídas e distorce ESC, AUT, FLEX e DISP; o cálculo
    // não muda, apenas avisa e identifica os nós. Mapa sem nenhuma aresta (m = 0) já é tratado como eixos não aplicáveis.
    private static IEnumerable<string> AvisosDeNosSemLigacao(EstudoDeCaso estudoDeCaso, ContextoDoMapa contexto)
    {
        if (contexto.M == 0)
            yield break;

        foreach (var no in estudoDeCaso.Nos)
        {
            var grau = contexto.GrausEntrada.GetValueOrDefault(no.Id) + contexto.GrausSaida.GetValueOrDefault(no.Id);
            if (grau == 0)
                yield return $"O nó \"{no.Nome}\" não entra em nenhuma ligação do Mapa: ele forma uma comunidade própria sem saídas e distorce ESC, AUT, FLEX e DISP. Ligue-o ao Mapa ou remova-o (TCC 4.2.7, passo 7).";
        }
    }

    // saidas_externas(c) — TCC 4.4.2 (AUT): arestas que PARTEM de nós da comunidade para nós de outra comunidade.
    private static int SaidasExternasDa(int comunidade, ContextoDoMapa contexto)
    {
        var indices = contexto.Comunidades.IndicePorNo;
        return contexto.EstudoDeCaso.Arestas.Count(a => indices[a.NoOrigemId] == comunidade && indices[a.NoDestinoId] != comunidade);
    }

    private static Recomendacao? GerarRecomendacao(IReadOnlyList<ResultadoDeEixo> eixos, ConjuntoDeParametros parametros)
    {
        if (parametros.Assinaturas.Count == 0)
            return null;

        var pesos = parametros.Pesos;
        var pesosEfetivos = ComparadorDePerfis.CalcularPesosEfetivos(eixos, pesos);
        if (!pesosEfetivos.PodeComparar)
            return null; // nenhum eixo aplicável com peso: não há distância a calcular (aviso em AvisosDeEixosNaoAplicaveis)

        var distancias = parametros.Assinaturas
            .Select(assinatura => ComparadorDePerfis.Comparar(eixos, assinatura, pesos))
            .ToList();

        return GeradorDeRecomendacao.Gerar(distancias, pesosEfetivos);
    }

    // RN07/RN08 — eixo "não aplicável" fica fora da distância e os pesos dos demais são renormalizados (p'_i = p_i / Σ p_j),
    // para que Σ p' = 1 e a distância máxima continue 5. O aviso deixa isso visível ao usuário (nada é omitido em silêncio).
    private static IEnumerable<string> AvisosDeEixosNaoAplicaveis(
        IReadOnlyList<ResultadoDeEixo> eixos, ConjuntoDeParametros parametros, Recomendacao? recomendacao)
    {
        if (parametros.Assinaturas.Count == 0)
            yield break;

        var omitidos = eixos.Where(e => !e.Aplicavel).Select(e => e.Eixo.ToString()).ToList();

        if (recomendacao is null)
        {
            yield return "Nenhum eixo aplicável com peso: não há distância até os perfis de referência a calcular, e portanto não há perfil mais próximo (RN07/RN08).";
            yield break;
        }

        if (omitidos.Count > 0)
            yield return $"Eixo(s) não aplicável(is) fora da distância: {string.Join(", ", omitidos)}. Os pesos dos demais eixos foram renormalizados para somar 1 (RN07/RN08).";
    }
}
