using System.Net;

namespace Sbar.Domain.Exceptions;

public abstract class DomainException : HttpRequestException
{
    protected DomainException(string mensagem, HttpStatusCode status) : base(mensagem, null, status)
    {
    }
}
