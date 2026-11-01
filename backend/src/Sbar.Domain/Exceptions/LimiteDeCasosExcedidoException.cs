using System.Net;
using Sbar.Domain.Entities;

namespace Sbar.Domain.Exceptions;

public class LimiteDeCasosExcedidoException : DomainException
{
    public LimiteDeCasosExcedidoException()
        : base($"Você atingiu o limite de {EstudoDeCaso.LimiteDeCasosPorUsuario} Estudos de Caso. Exclua um para criar outro.",
            HttpStatusCode.UnprocessableEntity)
    {
    }
}
