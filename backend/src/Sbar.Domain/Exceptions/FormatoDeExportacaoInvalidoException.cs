using System.Net;

namespace Sbar.Domain.Exceptions;

// Exportação (RF13): só JSON e CSV (D13). Formato desconhecido → 400.
public class FormatoDeExportacaoInvalidoException : DomainException
{
    public FormatoDeExportacaoInvalidoException(string formato)
        : base($"Formato de exportação '{formato}' inválido — use 'json' ou 'csv'.", HttpStatusCode.BadRequest)
    {
    }
}
