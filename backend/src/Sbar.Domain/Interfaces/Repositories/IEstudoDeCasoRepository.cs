using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Repositories;

// EstudoDeCaso é o aggregate root (RF-01) — No e Aresta nunca têm repositório próprio,
// só existem dentro do EstudoDeCaso que os carrega e salva.
public interface IEstudoDeCasoRepository
{
    Task<EstudoDeCaso?> ObterPorIdAsync(Guid id);
    Task<IReadOnlyList<EstudoDeCaso>> ListarAsync();
    Task<int> ContarAsync();
    Task AdicionarAsync(EstudoDeCaso estudoDeCaso);
    void Remover(EstudoDeCaso estudoDeCaso);
    Task SalvarAsync();
}
