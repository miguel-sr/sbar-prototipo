using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Domain.Interfaces.Services;

// RF-03 — cadastro de Arestas, validando peso/self-loop/mesmo Estudo de Caso
public interface IServicoDeArestas
{
    Task<Aresta> CriarAsync(Guid estudoDeCasoId, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa);
    Task<IReadOnlyList<Aresta>> ListarAsync(Guid estudoDeCasoId);
    Task<Aresta> AtualizarAsync(Guid estudoDeCasoId, Guid arestaId, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa);
    Task RemoverAsync(Guid estudoDeCasoId, Guid arestaId);
}
