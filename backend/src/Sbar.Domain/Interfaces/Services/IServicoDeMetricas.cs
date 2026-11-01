using Sbar.Domain.Metricas;

namespace Sbar.Domain.Interfaces.Services;

// RF-06/RF-07 — as dez métricas do Radar, brutas e normalizadas, sempre recalculadas (RF-11).
// versaoDosParametros: reaplica uma versão antiga dos limites/pesos/assinaturas (5.4); null = vigente.
public interface IServicoDeMetricas
{
    Task<ResultadoDeMetricas> CalcularAsync(Guid estudoDeCasoId, int? versaoDosParametros = null);
}
