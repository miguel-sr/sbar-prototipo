using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Interfaces.Repositories;

namespace Sbar.Infrastructure;

// Transação + bloqueio consultivo do Postgres (pg_advisory_xact_lock): serializa quem altera os parâmetros de análise e
// mantém a numeração das versões sequencial, sem colisão, mesmo com chamadas concorrentes (RF11). O bloqueio é liberado
// sozinho no commit/rollback. Se já houver transação em curso (chamada aninhada), a operação apenas participa dela.
public class UnidadeDeTrabalho(SbarDbContext contexto) : IUnidadeDeTrabalho
{
    public async Task ExecutarComBloqueioAsync(string chave, Func<Task> operacao)
    {
        if (contexto.Database.CurrentTransaction is not null)
        {
            await operacao();
            return;
        }

        await using var transacao = await contexto.Database.BeginTransactionAsync();
        try
        {
            await contexto.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtext({chave}))");
            await operacao();
            await transacao.CommitAsync();
        }
        catch
        {
            // Devolve ao estado do banco o que o contexto rastreava de uma operação que não foi confirmada.
            contexto.ChangeTracker.Clear();
            throw;
        }
    }
}
