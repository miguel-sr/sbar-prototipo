using Microsoft.AspNetCore.Identity;

namespace Sbar.Infrastructure.Identidade;

// A conta de acesso (ver docs/projeto/ARQUITETURA.md). Fica em Infrastructure: o Domain só conhece o Guid do dono.
public class Usuario : IdentityUser<Guid>
{
}
