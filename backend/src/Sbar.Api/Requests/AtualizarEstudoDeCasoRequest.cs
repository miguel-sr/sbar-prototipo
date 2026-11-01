namespace Sbar.Api.Requests;

/// <summary>Novos dados de um Estudo de Caso (substitui nome e descrição).</summary>
/// <param name="Nome">Nome do sistema/produto sendo mapeado — obrigatório, até 120 caracteres (422 se vazio ou maior).</param>
/// <param name="DominioDescricao">Descrição livre e opcional do domínio de negócio — até 1000 caracteres.</param>
public record AtualizarEstudoDeCasoRequest(string Nome, string? DominioDescricao);
