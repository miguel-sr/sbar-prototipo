using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Repositories;

// Estado vigente dos limites de normalização e pesos dos dez eixos (tabela ConfiguracoesDeEixo).
public interface IConfiguracaoDeEixoRepository
{
    Task<IReadOnlyList<ConfiguracaoDeEixo>> ListarAsync();
    Task AdicionarAsync(ConfiguracaoDeEixo configuracao);
    Task SalvarAsync();
}
