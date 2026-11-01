using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Services;

// RF05 — cadastro dos fluxos principais (F) de um Estudo de Caso, com verificação de caminho dirigido
public interface IServicoDeFluxos
{
    Task<FluxoAvaliado> CriarAsync(Guid estudoDeCasoId, string nome, IReadOnlyList<Guid> nosIds);
    Task<IReadOnlyList<FluxoAvaliado>> ListarAsync(Guid estudoDeCasoId);
    Task<FluxoAvaliado> AtualizarAsync(Guid estudoDeCasoId, Guid fluxoId, string nome, IReadOnlyList<Guid> nosIds);
    Task RemoverAsync(Guid estudoDeCasoId, Guid fluxoId);
}
