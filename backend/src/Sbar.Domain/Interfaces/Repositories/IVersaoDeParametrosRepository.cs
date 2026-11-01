using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Repositories;

public interface IVersaoDeParametrosRepository
{
    Task<VersaoDeParametros?> ObterPorNumeroAsync(int numero);
    Task<VersaoDeParametros?> ObterUltimaAsync();
    Task<IReadOnlyList<VersaoDeParametros>> ListarAsync();
    Task AdicionarAsync(VersaoDeParametros versao);
    Task SalvarAsync();
}
