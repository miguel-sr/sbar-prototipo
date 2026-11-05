using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Repositories;

public interface IPerfilArquiteturalRepository
{
    Task<PerfilArquitetural?> ObterPorIdAsync(Guid id);
    Task<IReadOnlyList<PerfilArquitetural>> ListarAsync();
    Task AdicionarAsync(PerfilArquitetural perfil);
    void Remover(PerfilArquitetural perfil);
    Task SalvarAsync();
}
