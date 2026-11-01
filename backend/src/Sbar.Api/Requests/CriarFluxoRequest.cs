namespace Sbar.Api.Requests;

/// <summary>Dados para cadastrar um Fluxo principal (F) — uma sequência ordenada de Nós do Mapa.</summary>
/// <param name="Nome">Nome do fluxo (ex.: "Pedido e preparo"); até 100 caracteres.</param>
/// <param name="NosIds">Ids dos Nós, na ordem do caminho. Pode repetir Nós, não precisa começar no Ator, e cada par consecutivo precisa ter uma Aresta origem→destino já cadastrada (422 indicando a etapa ausente).</param>
public record CriarFluxoRequest(string Nome, IReadOnlyList<Guid> NosIds);
