using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Application.CasosDeUso;

// RF-01 — CRUD de EstudoDeCaso
public class ServicoDeCasos(IEstudoDeCasoRepository repositorio, IUsuarioAtual usuarioAtual) : IServicoDeCasos
{
    public async Task<EstudoDeCaso> CriarAsync(string nome, string? dominioDescricao)
    {
        // A listagem e a contagem já vêm filtradas pelo dono (filtro global do DbContext).
        if (await repositorio.ContarAsync() >= EstudoDeCaso.LimiteDeCasosPorUsuario)
            throw new LimiteDeCasosExcedidoException();

        var estudoDeCaso = EstudoDeCaso.Criar(nome, dominioDescricao, usuarioAtual.DonoId);

        await repositorio.AdicionarAsync(estudoDeCaso);
        await repositorio.SalvarAsync();
        
        return estudoDeCaso;
    }

    public async Task<EstudoDeCaso> ObterAsync(Guid id) =>
        await repositorio.ObterPorIdAsync(id)
            ?? throw new RecursoNaoEncontradoException(nameof(EstudoDeCaso), id);

    public Task<IReadOnlyList<EstudoDeCaso>> ListarAsync() =>
        repositorio.ListarAsync();

    public async Task<EstudoDeCaso> AtualizarAsync(Guid id, string nome, string? dominioDescricao)
    {
        var estudoDeCaso = await ObterAsync(id);

        estudoDeCaso.Atualizar(nome, dominioDescricao);
        await repositorio.SalvarAsync();

        return estudoDeCaso;
    }

    // Os Nós e as Arestas saem junto (DeleteBehavior.Cascade no mapeamento).
    public async Task ExcluirAsync(Guid id)
    {
        var estudoDeCaso = await ObterAsync(id);

        repositorio.Remover(estudoDeCaso);
        await repositorio.SalvarAsync();
    }
}
