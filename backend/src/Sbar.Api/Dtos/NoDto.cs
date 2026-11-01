using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Api.Dtos;

/// <summary>Um Nó do Mapa.</summary>
/// <param name="Id">Id do Nó.</param>
/// <param name="Nome">Nome do elemento do Blueprint.</param>
/// <param name="Tipo">Ator, Touchpoint, Processo, Suporte, Dados ou Externo.</param>
/// <param name="Banda">Derivada automaticamente do Tipo (Ator, Frontstage, Backstage, Suporte ou Externo) — nunca escolhida pelo cliente.</param>
public record NoDto(Guid Id, string Nome, TipoDeNo Tipo, Banda Banda)
{
    public static NoDto DeEntidade(No no) => new(no.Id, no.Nome, no.Tipo, no.Banda);
}
