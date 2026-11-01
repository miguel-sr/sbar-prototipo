using Sbar.Domain.Entities;
using Sbar.Domain.ValueObjects;

namespace Sbar.Domain.Interfaces.Services;

// RF-08 — CRUD de Perfis Arquiteturais de referência (tabela global, débito D1)
public interface IServicoDePerfis
{
    Task<PerfilArquitetural> CriarAsync(DadosDoPerfilArquitetural dados);

    Task<PerfilArquitetural> ObterAsync(Guid id);

    Task<IReadOnlyList<PerfilArquitetural>> ListarAsync();

    Task<PerfilArquitetural> AtualizarAsync(Guid id, DadosDoPerfilArquitetural dados);

    Task RemoverAsync(Guid id);
}
