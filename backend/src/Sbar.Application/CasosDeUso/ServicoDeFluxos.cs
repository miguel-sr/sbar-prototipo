using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Application.CasosDeUso;

// RF05 — cadastro dos fluxos principais (F), validando o caminho dirigido (via EstudoDeCaso.AdicionarFluxo)
public class ServicoDeFluxos(IEstudoDeCasoRepository repositorio) : IServicoDeFluxos
{
    public async Task<FluxoAvaliado> CriarAsync(Guid estudoDeCasoId, string nome, IReadOnlyList<Guid> nosIds)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var fluxo = estudoDeCaso.AdicionarFluxo(nome, nosIds);
        await repositorio.SalvarAsync();

        return new FluxoAvaliado(fluxo, estudoDeCaso.AvaliarFluxo(fluxo));
    }

    public async Task<IReadOnlyList<FluxoAvaliado>> ListarAsync(Guid estudoDeCasoId)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        return estudoDeCaso.Fluxos.Select(f => new FluxoAvaliado(f, estudoDeCaso.AvaliarFluxo(f))).ToList();
    }

    public async Task<FluxoAvaliado> AtualizarAsync(Guid estudoDeCasoId, Guid fluxoId, string nome, IReadOnlyList<Guid> nosIds)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var fluxo = estudoDeCaso.AtualizarFluxo(fluxoId, nome, nosIds);
        await repositorio.SalvarAsync();

        return new FluxoAvaliado(fluxo, estudoDeCaso.AvaliarFluxo(fluxo));
    }

    public async Task RemoverAsync(Guid estudoDeCasoId, Guid fluxoId)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        estudoDeCaso.RemoverFluxo(fluxoId);
        await repositorio.SalvarAsync();
    }

    private async Task<EstudoDeCaso> ObterEstudoDeCasoAsync(Guid estudoDeCasoId) =>
        await repositorio.ObterPorIdAsync(estudoDeCasoId)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), estudoDeCasoId);
}
