using Microsoft.AspNetCore.Identity;

namespace Sbar.Infrastructure.Identidade;

// A conta de acesso (PLANO_CONTAS_DE_USUARIO.md 4.1). Fica em Infrastructure: o Domain só conhece o Guid do dono.
public class Usuario : IdentityUser<Guid>
{
}
