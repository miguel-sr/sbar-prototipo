namespace Sbar.Domain.Interfaces;

// Quem está fazendo a requisição (ver docs/projeto/ARQUITETURA.md). Implementado na Api a partir do HttpContext;
// Domain e Application só conhecem o Guid do dono, nunca a conta de Identity.
public interface IUsuarioAtual
{
    // Nulo fora de uma requisição autenticada.
    Guid? DonoId { get; }
}
