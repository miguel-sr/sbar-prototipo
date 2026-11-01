using System.Net;

namespace Sbar.Domain.Exceptions;

public class RecursoNaoEncontradoException : DomainException
{
    public RecursoNaoEncontradoException(string recurso, object chave)
        : base($"{recurso} '{chave}' não encontrado.", HttpStatusCode.NotFound)
    {
    }
}
