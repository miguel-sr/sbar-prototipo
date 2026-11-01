using System.Security.Claims;
using Sbar.Domain.Interfaces;

namespace Sbar.Api.Identidade;

// Lê o dono da requisição do cookie de sessão (claim NameIdentifier = Id da conta).
public class UsuarioAtual(IHttpContextAccessor acessor) : IUsuarioAtual
{
    public Guid? DonoId =>
        Guid.TryParse(acessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
