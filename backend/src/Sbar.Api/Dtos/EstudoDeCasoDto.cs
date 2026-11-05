using Sbar.Domain.Entities;

namespace Sbar.Api.Dtos;

/// <summary>Um Estudo de Caso, com todos os Nós e Arestas já cadastrados.</summary>
/// <param name="Id">Id do Estudo de Caso.</param>
/// <param name="Nome">Nome do sistema/produto sendo mapeado.</param>
/// <param name="DominioDescricao">Descrição livre do domínio de negócio (pode ser <c>null</c>).</param>
/// <param name="DataCriacao">Data/hora de criação (UTC).</param>
/// <param name="Nos">Todos os Nós já cadastrados.</param>
/// <param name="Arestas">Todas as Arestas já cadastradas.</param>
public record EstudoDeCasoDto(
    Guid Id, string Nome, string? DominioDescricao, DateTime DataCriacao,
    IReadOnlyList<NoDto> Nos, IReadOnlyList<ArestaDto> Arestas)
{
    public static EstudoDeCasoDto DeEntidade(EstudoDeCaso estudoDeCaso) => new(
        estudoDeCaso.Id,
        estudoDeCaso.Nome,
        estudoDeCaso.DominioDescricao,
        estudoDeCaso.DataCriacao,
        estudoDeCaso.Nos.Select(NoDto.DeEntidade).ToList(),
        estudoDeCaso.Arestas.Select(ArestaDto.DeEntidade).ToList());
}
