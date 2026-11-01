using Sbar.Domain.Entities;
using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Application.CasosDeUso;

// RF-02 — cadastro de Nós, aplicando RegraDeBanda (via No.Criar)
public class ServicoDeNos(IEstudoDeCasoRepository repositorio) : IServicoDeNos
{
    public async Task<No> CriarAsync(Guid estudoDeCasoId, string nome, TipoDeNo tipo)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var no = estudoDeCaso.AdicionarNo(nome, tipo);
        await repositorio.SalvarAsync();

        return no;
    }

    public async Task<IReadOnlyList<No>> ListarAsync(Guid estudoDeCasoId) =>
        (await ObterEstudoDeCasoAsync(estudoDeCasoId)).Nos;

    public async Task<No> AtualizarAsync(Guid estudoDeCasoId, Guid noId, string nome, TipoDeNo tipo)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        var no = estudoDeCaso.AtualizarNo(noId, nome, tipo);
        await repositorio.SalvarAsync();

        return no;
    }

    // As Arestas ligadas ao Nó removido saem junto (regra do agregado).
    public async Task RemoverAsync(Guid estudoDeCasoId, Guid noId)
    {
        var estudoDeCaso = await ObterEstudoDeCasoAsync(estudoDeCasoId);

        estudoDeCaso.RemoverNo(noId);
        await repositorio.SalvarAsync();
    }

    private async Task<EstudoDeCaso> ObterEstudoDeCasoAsync(Guid estudoDeCasoId) =>
        await repositorio.ObterPorIdAsync(estudoDeCasoId)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), estudoDeCasoId);
}
