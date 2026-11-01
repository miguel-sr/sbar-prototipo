using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Application.CasosDeUso;

// RF03/RN03 — cadastro de Arestas, validando peso/self-loop/justificativa (via EstudoDeCaso.AdicionarAresta)
public class ServicoDeArestas(IEstudoDeCasoRepository repositorio) : IServicoDeArestas
{
    public async Task<Aresta> CriarAsync(Guid estudoDeCasoId, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var aresta = estudoDeCaso.AdicionarAresta(noOrigemId, noDestinoId, tipo, peso, justificativa);
        await repositorio.SalvarAsync();

        return aresta;
    }

    public async Task<IReadOnlyList<Aresta>> ListarAsync(Guid estudoDeCasoId) =>
        (await ObterEstudoDeCasoAsync(estudoDeCasoId)).Arestas;

    public async Task<Aresta> AtualizarAsync(Guid estudoDeCasoId, Guid arestaId, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var aresta = estudoDeCaso.AtualizarAresta(arestaId, noOrigemId, noDestinoId, tipo, peso, justificativa);
        await repositorio.SalvarAsync();

        return aresta;
    }

    public async Task RemoverAsync(Guid estudoDeCasoId, Guid arestaId)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        estudoDeCaso.RemoverAresta(arestaId);
        await repositorio.SalvarAsync();
    }

    private async Task<EstudoDeCaso> ObterEstudoDeCasoAsync(Guid estudoDeCasoId) =>
        await repositorio.ObterPorIdAsync(estudoDeCasoId)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), estudoDeCasoId);
}
