using Sbar.Api.Dtos;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Exportacao;

// RF13 — reúne, para um Estudo de Caso e uma versão dos parâmetros, tudo o que o TCC 4.6.1 manda registrar. Nada é persistido:
// cada exportação recalcula a análise a partir do estado atual do Mapa (RF-11).
public class MontadorDeExportacao(
    IServicoDeCasos casos,
    IServicoDeFluxos fluxos,
    IServicoDeMetricas metricas,
    IServicoDeRastreabilidade rastreabilidade,
    IServicoDeParametros parametros)
{
    public async Task<ExportacaoDto> MontarAsync(Guid casoId, int? versao)
    {
        var caso = await casos.ObterAsync(casoId);
        var conjunto = await parametros.ObterAsync(versao);
        var resultado = await metricas.CalcularAsync(casoId, conjunto.Versao.Numero);
        var matriz = await rastreabilidade.ObterAsync(casoId);
        var fluxosAvaliados = await fluxos.ListarAsync(casoId);

        var dto = ResultadoDeMetricasDto.DeResultado(resultado);

        return new ExportacaoDto(
            GeradoEm: DateTime.UtcNow,
            Caso: new CasoExportadoDto(caso.Id, caso.Nome, caso.DominioDescricao),
            VersaoDosParametros: dto.VersaoDosParametros,
            Parametros: ConjuntoDeParametrosDto.DeConjunto(conjunto),
            Nos: caso.Nos.Select(NoDto.DeEntidade).ToList(),
            Arestas: caso.Arestas.Select(ArestaDto.DeEntidade).ToList(),
            Fluxos: fluxosAvaliados.Select(FluxoDto.DeAvaliado).ToList(),
            Comunidades: dto.Comunidades,
            ModularidadeQ: dto.ModularidadeQ,
            Avisos: dto.Avisos,
            Bandas: dto.Bandas,
            Eixos: dto.Eixos,
            Recomendacao: dto.Recomendacao,
            Matriz: matriz.Select(LinhaDeRastreabilidadeDto.DeLinha).ToList());
    }
}
