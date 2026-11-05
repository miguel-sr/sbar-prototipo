namespace Sbar.Domain.Interfaces;

// Quem está fazendo a requisição (PLANO_CONTAS_DE_USUARIO.md 4.2). Implementado na Api a partir do HttpContext;
// Domain e Application só conhecem o Guid do dono, nunca a conta de Identity.
public interface IUsuarioAtual
{
    // Nulo fora de uma requisição autenticada.
    Guid? DonoId { get; }
}
