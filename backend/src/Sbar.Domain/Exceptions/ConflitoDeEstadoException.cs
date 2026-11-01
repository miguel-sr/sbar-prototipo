using System.Net;

namespace Sbar.Domain.Exceptions;

public class ConflitoDeEstadoException : DomainException
{
    public ConflitoDeEstadoException(string mensagem) : base(mensagem, HttpStatusCode.Conflict)
    {
    }
}
