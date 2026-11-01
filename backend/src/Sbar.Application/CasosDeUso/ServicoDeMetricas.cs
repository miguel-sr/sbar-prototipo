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

        return new ResultadoDeMetricas(
            eixos, recomendacao, nosGargalo, comunidades, contexto.Comunidades.ModularidadeQ, avisos, bandas, fluxos, parametros.Versao);
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
        var distancias = parametros.Assinaturas
            .Select(assinatura => ComparadorDePerfis.Comparar(eixos, assinatura, pesos))
            .ToList();

        return GeradorDeRecomendacao.Gerar(distancias);
    }
}
