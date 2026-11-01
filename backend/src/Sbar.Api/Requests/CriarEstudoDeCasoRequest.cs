namespace Sbar.Api.Requests;

/// <summary>Dados para criar um Estudo de Caso — o "projeto" que vai reunir o Mapa (Nós e Arestas) a ser analisado.</summary>
/// <param name="Nome">Nome do sistema/produto sendo mapeado (ex.: "Sistema de Pedidos Digital").</param>
/// <param name="DominioDescricao">Descrição livre e opcional do domínio de negócio.</param>
public record CriarEstudoDeCasoRequest(string Nome, string? DominioDescricao);
