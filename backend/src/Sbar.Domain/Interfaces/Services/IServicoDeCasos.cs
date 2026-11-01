using Sbar.Domain.Entities;

namespace Sbar.Domain.Interfaces.Services;

// RF-01 — CRUD de EstudoDeCaso
public interface IServicoDeCasos
{
    Task<EstudoDeCaso> CriarAsync(string nome, string? dominioDescricao);
    Task<EstudoDeCaso> ObterAsync(Guid id);
    Task<IReadOnlyList<EstudoDeCaso>> ListarAsync();
    Task<EstudoDeCaso> AtualizarAsync(Guid id, string nome, string? dominioDescricao);
    Task ExcluirAsync(Guid id);
}
