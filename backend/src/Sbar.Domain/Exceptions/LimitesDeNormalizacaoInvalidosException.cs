using System.Net;

namespace Sbar.Domain.Exceptions;

// Normalização — max <= min é configuração inválida (docs/dominio/formulas-metricas.md)
public class LimitesDeNormalizacaoInvalidosException : DomainException
{
    public LimitesDeNormalizacaoInvalidosException(string eixo, double limiteMinimo, double limiteMaximo)
        : base($"Limites do eixo {eixo} inválidos: mínimo {limiteMinimo} deve ser menor que máximo {limiteMaximo}.", HttpStatusCode.UnprocessableEntity)
    {
    }
}
