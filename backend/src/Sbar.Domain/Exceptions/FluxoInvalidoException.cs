using System.Net;

namespace Sbar.Domain.Exceptions;

// RF05 / CT09 (TCC revisado, seções 4.2.7 e 5.3.6) — fluxo que não pode ser salvo: poucos nós ou uma etapa
// sem aresta origem→destino. 422, mesma família de PesoDeArestaInvalidoException/NomeInvalidoException.
public class FluxoInvalidoException : DomainException
{
    private FluxoInvalidoException(string mensagem)
        : base(mensagem, HttpStatusCode.UnprocessableEntity)
    {
    }

    public static FluxoInvalidoException PoucosNos(int minimo) =>
        new($"Um Fluxo precisa de pelo menos {minimo} nós.");

    public static FluxoInvalidoException NosDemais(int maximo) =>
        new($"Um Fluxo não pode ter mais de {maximo} nós.");

    public static FluxoInvalidoException SemAresta(string nomeDoFluxo, int posicao, string nomeDaOrigem, string nomeDoDestino) =>
        new($"Fluxo '{nomeDoFluxo}': não há aresta de '{nomeDaOrigem}' para '{nomeDoDestino}' (etapa {posicao}). Cadastre a aresta antes de salvar o fluxo.");
}
