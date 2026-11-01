using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Domain.Interfaces.Services;

// RF-02 — cadastro de Nós, banda sempre derivada do tipo
public interface IServicoDeNos
{
    Task<No> CriarAsync(Guid estudoDeCasoId, string nome, TipoDeNo tipo);
    Task<IReadOnlyList<No>> ListarAsync(Guid estudoDeCasoId);
    Task<No> AtualizarAsync(Guid estudoDeCasoId, Guid noId, string nome, TipoDeNo tipo);
    Task RemoverAsync(Guid estudoDeCasoId, Guid noId);
}
