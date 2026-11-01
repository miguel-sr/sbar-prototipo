using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Entities;
using Sbar.Domain.Interfaces.Repositories;

namespace Sbar.Infrastructure.Repositories;

public class EstudoDeCasoRepository(SbarDbContext contexto) : IEstudoDeCasoRepository
{
    public Task<EstudoDeCaso?> ObterPorIdAsync(Guid id) =>
        contexto.EstudosDeCaso
            .Include(e => e.Nos)
            .Include(e => e.Arestas)
            .Include(e => e.Fluxos).ThenInclude(f => f.Passos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<EstudoDeCaso>> ListarAsync() =>
        await contexto.EstudosDeCaso
            .Include(e => e.Nos)
            .Include(e => e.Arestas)
            .AsSplitQuery()
            .ToListAsync();

    public Task<int> ContarAsync() =>
        contexto.EstudosDeCaso.CountAsync();

    public async Task AdicionarAsync(EstudoDeCaso estudoDeCaso) =>
        await contexto.EstudosDeCaso.AddAsync(estudoDeCaso);

    public void Remover(EstudoDeCaso estudoDeCaso) =>
        contexto.EstudosDeCaso.Remove(estudoDeCaso);

    public Task SalvarAsync() =>
        contexto.SaveChangesAsync();
}
