using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.CasosDeUso;

// RF-08 — CRUD de Perfis Arquiteturais de referência (assinaturas, Quadro 7). RF11/US06: toda alteração grava uma nova
// versão dos parâmetros (limites + pesos + assinaturas) na mesma transação; a versão 1 (estado anterior) é garantida
// antes da primeira alteração.
public class ServicoDePerfis(
    IPerfilArquiteturalRepository repositorio,
    IServicoDeParametros parametros,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IServicoDePerfis
{
    public async Task<PerfilArquitetural> CriarAsync(DadosDoPerfilArquitetural dados)
    {
        var perfil = PerfilArquitetural.Criar(dados, usuarioAtual.DonoId);

        await AlterarERegistrarVersaoAsync($"Assinatura '{dados.Nome}' criada", async () =>
        {
            await repositorio.AdicionarAsync(perfil);
            await repositorio.SalvarAsync();
        });

        return perfil;
    }

    public async Task<PerfilArquitetural> ObterAsync(Guid id)
    {
        await parametros.GarantirVersaoInicialAsync();

        return await repositorio.ObterPorIdAsync(id)
            ?? throw new RecursoNaoEncontradoException(nameof(PerfilArquitetural), id);
    }

    // Conta nova: os padrões do Quadro 7 são copiados para o usuário na primeira leitura.
    public async Task<IReadOnlyList<PerfilArquitetural>> ListarAsync()
    {
        await parametros.GarantirVersaoInicialAsync();

        return await repositorio.ListarAsync();
    }

    public async Task<PerfilArquitetural> AtualizarAsync(Guid id, DadosDoPerfilArquitetural dados)
    {
        var perfil = await ObterAsync(id);

        await AlterarERegistrarVersaoAsync($"Assinatura '{dados.Nome}' atualizada", async () =>
        {
            perfil.Atualizar(dados);
            await repositorio.SalvarAsync();
        });

        return perfil;
    }

    public async Task RemoverAsync(Guid id)
    {
        var perfil = await ObterAsync(id);

        await AlterarERegistrarVersaoAsync($"Assinatura '{perfil.Nome}' removida", async () =>
        {
            repositorio.Remover(perfil);
            await repositorio.SalvarAsync();
        });
    }

    private Task AlterarERegistrarVersaoAsync(string descricao, Func<Task> alteracao) =>
        unidadeDeTrabalho.ExecutarComBloqueioAsync(ServicoDeParametros.ChaveDoBloqueioDe(usuarioAtual.DonoId), async () =>
        {
            await parametros.GarantirVersaoInicialAsync();
            await alteracao();
            await parametros.RegistrarVersaoAsync(descricao);
        });
}
