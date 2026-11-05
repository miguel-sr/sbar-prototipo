using System.Net;

namespace Sbar.Domain.Exceptions;

// RF-03 — aresta só pode ligar nós do mesmo Estudo de Caso
public class NoDeOutroEstudoDeCasoException : DomainException
{
    public NoDeOutroEstudoDeCasoException(Guid noId)
        : base($"Nó '{noId}' não pertence a este Estudo de Caso.", HttpStatusCode.UnprocessableEntity)
    {
    }
}
