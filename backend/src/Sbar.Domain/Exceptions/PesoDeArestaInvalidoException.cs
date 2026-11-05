using System.Net;

namespace Sbar.Domain.Exceptions;

// RF-03 — peso de aresta restrito a {1, 2, 3} (TCC seção 4.2.5)
public class PesoDeArestaInvalidoException : DomainException
{
    public PesoDeArestaInvalidoException(int valorInvalido)
        : base($"Peso {valorInvalido} inválido — deve ser 1, 2 ou 3.", HttpStatusCode.UnprocessableEntity)
    {
    }
}
