using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Entities;
using Sbar.Domain.Interfaces.Repositories;

namespace Sbar.Infrastructure.Repositories;

public class VersaoDeParametrosRepository(SbarDbContext contexto) : IVersaoDeParametrosRepository
{
    public Task<VersaoDeParametros?> ObterPorNumeroAsync(int numero) =>
        contexto.VersoesDeParametros.AsNoTracking().FirstOrDefaultAsync(v => v.Numero == numero);

    public Task<VersaoDeParametros?> ObterUltimaAsync() =>
        contexto.VersoesDeParametros.AsNoTracking().OrderByDescending(v => v.Numero).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<VersaoDeParametros>> ListarAsync() =>
        await contexto.VersoesDeParametros.AsNoTracking().OrderBy(v => v.Numero).ToListAsync();

    public async Task AdicionarAsync(VersaoDeParametros versao) =>
        await contexto.VersoesDeParametros.AddAsync(versao);

    public Task SalvarAsync() =>
        contexto.SaveChangesAsync();
}
