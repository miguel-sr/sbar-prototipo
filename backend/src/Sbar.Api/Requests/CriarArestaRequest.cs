using Sbar.Domain.Enums;

namespace Sbar.Api.Requests;

/// <summary>Dados para criar uma Aresta (interação) entre dois Nós já existentes no mesmo Estudo de Caso.</summary>
/// <param name="NoOrigemId">Id do Nó de origem (deve pertencer ao mesmo Estudo de Caso da rota).</param>
/// <param name="NoDestinoId">Id do Nó de destino — não pode ser igual a <paramref name="NoOrigemId"/> (self-loop é proibido, 422).</param>
/// <param name="Tipo">Tipo da interação: Sincrona, Assincrona, Critica ou Batch.</param>
/// <param name="Peso">Intensidade da interação, restrito a 1, 2 ou 3 (422 fora desse intervalo).</param>
/// <param name="Justificativa">Por que esta interação existe: rastreabilidade até o Service Blueprint (RN03). Obrigatória, até 500 caracteres, sem espaços nas pontas (422 se vazia ou longa demais).</param>
public record CriarArestaRequest(Guid NoOrigemId, Guid NoDestinoId, TipoDeAresta Tipo, int Peso, string Justificativa);
