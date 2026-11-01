using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Api.Dtos;

/// <summary>Uma Aresta (interação) do Mapa.</summary>
/// <param name="Id">Id da Aresta.</param>
/// <param name="NoOrigemId">Id do Nó de origem.</param>
/// <param name="NoDestinoId">Id do Nó de destino.</param>
/// <param name="Tipo">Sincrona, Assincrona, Critica ou Batch.</param>
/// <param name="Peso">1, 2 ou 3.</param>
/// <param name="Justificativa">Por que a interação existe (RN03). Arestas anteriores à exigência trazem o texto-padrão "Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)".</param>
public record ArestaDto(Guid Id, Guid NoOrigemId, Guid NoDestinoId, TipoDeAresta Tipo, int Peso, string Justificativa)
{
    public static ArestaDto DeEntidade(Aresta aresta) =>
        new(aresta.Id, aresta.NoOrigemId, aresta.NoDestinoId, aresta.Tipo, aresta.Peso.Valor, aresta.Justificativa);
}
