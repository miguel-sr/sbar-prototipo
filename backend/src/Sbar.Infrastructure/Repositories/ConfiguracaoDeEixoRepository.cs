using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Entities;
using Sbar.Domain.Interfaces.Repositories;

namespace Sbar.Infrastructure.Repositories;

public class ConfiguracaoDeEixoRepository(SbarDbContext contexto) : IConfiguracaoDeEixoRepository
{
    public async Task<IReadOnlyList<ConfiguracaoDeEixo>> ListarAsync() =>
        await contexto.ConfiguracoesDeEixo.ToListAsync();

    public async Task AdicionarAsync(ConfiguracaoDeEixo configuracao) =>
        await contexto.ConfiguracoesDeEixo.AddAsync(configuracao);

    public Task SalvarAsync() =>
        contexto.SaveChangesAsync();
}
