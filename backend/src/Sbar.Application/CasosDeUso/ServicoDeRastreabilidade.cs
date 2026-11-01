using Sbar.Application.Grafo;
using Sbar.Application.Rastreabilidade;
using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.Metricas;

namespace Sbar.Application.CasosDeUso;

// RF-10 — orquestra: EstudoDeCaso → ContextoDoMapa → ConstrutorDeMatrizDeRastreabilidade
public class ServicoDeRastreabilidade(IEstudoDeCasoRepository repositorio) : IServicoDeRastreabilidade
{
    public async Task<IReadOnlyList<LinhaDeRastreabilidade>> ObterAsync(Guid estudoDeCasoId)
    {
        var estudoDeCaso = await repositorio.ObterPorIdAsync(estudoDeCasoId)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), estudoDeCasoId);

        var contexto = ConstrutorDeContextoDoMapa.Construir(estudoDeCaso);

        return ConstrutorDeMatrizDeRastreabilidade.Construir(contexto);
    }
}
