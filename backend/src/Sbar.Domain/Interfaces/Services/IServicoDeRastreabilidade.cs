using Sbar.Domain.Metricas;

namespace Sbar.Domain.Interfaces.Services;

// RF-10 — Matriz de Rastreabilidade
public interface IServicoDeRastreabilidade
{
    Task<IReadOnlyList<LinhaDeRastreabilidade>> ObterAsync(Guid estudoDeCasoId);
}
