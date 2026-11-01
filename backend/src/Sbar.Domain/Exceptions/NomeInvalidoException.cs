using System.Net;

namespace Sbar.Domain.Exceptions;

// Nome (ou descrição) vazio, só com espaços ou acima do tamanho máximo — 422, mesma família de
// PesoDeArestaInvalidoException/ArestaAutoReferenciadaException. A API não confia na validação do front.
public class NomeInvalidoException : DomainException
{
    private NomeInvalidoException(string mensagem)
        : base(mensagem, HttpStatusCode.UnprocessableEntity)
    {
    }

    public static NomeInvalidoException Vazio(string campo) =>
        new($"{campo} é obrigatório e não pode ser só espaços.");

    public static NomeInvalidoException AcimaDoMaximo(string campo, int maximo) =>
        new($"{campo} não pode ter mais de {maximo} caracteres.");
}
