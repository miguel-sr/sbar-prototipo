using System.Net;

namespace Sbar.Domain.Exceptions;

// D6 — self-loop proibido (docs/dominio/duvidas-pendentes.md item 2)
public class ArestaAutoReferenciadaException : DomainException
{
    public ArestaAutoReferenciadaException(Guid noId)
        : base($"Aresta não pode ligar o nó '{noId}' a si mesmo.", HttpStatusCode.UnprocessableEntity)
    {
    }
}
