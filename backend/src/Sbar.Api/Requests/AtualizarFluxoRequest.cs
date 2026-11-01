namespace Sbar.Api.Requests;

/// <summary>Novos dados de um Fluxo principal — substitui nome e sequência de Nós, com as mesmas validações da criação.</summary>
/// <param name="Nome">Nome do fluxo; até 100 caracteres.</param>
/// <param name="NosIds">Ids dos Nós, na ordem do caminho (cada par consecutivo precisa ter uma Aresta origem→destino).</param>
public record AtualizarFluxoRequest(string Nome, IReadOnlyList<Guid> NosIds);
