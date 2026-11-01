namespace Sbar.Domain.Interfaces.Repositories;

// Executa uma operação inteira numa única transação, serializando as execuções concorrentes que usam a mesma
// chave (bloqueio consultivo do banco). Usada para gravar o estado vigente dos parâmetros e a nova versão
// juntos (RF11) com numeração sequencial sem colisão. Se já houver uma transação em curso, a operação
// participa dela.
public interface IUnidadeDeTrabalho
{
    Task ExecutarComBloqueioAsync(string chave, Func<Task> operacao);
}
