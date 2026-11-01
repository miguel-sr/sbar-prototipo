using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Entities;
using Sbar.Domain.Interfaces.Repositories;

namespace Sbar.Infrastructure.Repositories;

public class PerfilArquiteturalRepository(SbarDbContext contexto) : IPerfilArquiteturalRepository
{
    public Task<PerfilArquitetural?> ObterPorIdAsync(Guid id) =>
        contexto.PerfisArquiteturais.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<PerfilArquitetural>> ListarAsync() =>
        await contexto.PerfisArquiteturais.ToListAsync();

    public async Task AdicionarAsync(PerfilArquitetural perfil) =>
        await contexto.PerfisArquiteturais.AddAsync(perfil);

    public void Remover(PerfilArquitetural perfil) =>
        contexto.PerfisArquiteturais.Remove(perfil);

    public Task SalvarAsync() =>
        contexto.SaveChangesAsync();
}
