using Sbar.Domain.Entities;

namespace Sbar.Api.Dtos;

/// <summary>Etapa (par consecutivo de Nós) de um Fluxo que não tem Aresta origem→destino no Mapa atual.</summary>
/// <param name="Posicao">Número da etapa, a partir de 1: a etapa N liga o N-ésimo Nó do fluxo ao (N+1)-ésimo.</param>
/// <param name="OrigemId">Id do Nó de origem da etapa (mesmo que ele já tenha sido removido do Mapa).</param>
/// <param name="DestinoId">Id do Nó de destino da etapa.</param>
/// <param name="Motivo">Explicação pronta para mostrar ao usuário.</param>
public record EtapaAusenteDto(int Posicao, Guid OrigemId, Guid DestinoId, string Motivo);

/// <summary>Um Fluxo principal (F) cadastrado, avaliado contra o Mapa atual.</summary>
/// <param name="Id">Id do Fluxo.</param>
/// <param name="Nome">Nome do Fluxo.</param>
/// <param name="NosIds">Ids dos Nós do caminho, em ordem (repetição permitida).</param>
/// <param name="Valido"><c>true</c> quando cada par consecutivo de Nós tem uma Aresta origem→destino. Excluir Nó ou Aresta nunca apaga o fluxo — só o torna inválido. Só fluxos válidos entram no cálculo de DEP.</param>
/// <param name="EtapaAusente">Primeira etapa sem Aresta (ou com Nó removido); <c>null</c> quando o fluxo é válido.</param>
/// <param name="BandasPercorridas">Número de Bandas distintas dos Nós do caminho, incluindo origem e destino (TCC 4.4.2, Eixo 10).</param>
public record FluxoDto(Guid Id, string Nome, IReadOnlyList<Guid> NosIds, bool Valido, EtapaAusenteDto? EtapaAusente, int BandasPercorridas)
{
    public static FluxoDto DeAvaliado(FluxoAvaliado avaliado) => new(
        avaliado.Fluxo.Id,
        avaliado.Fluxo.Nome,
        avaliado.Fluxo.NosIds,
        avaliado.Avaliacao.Valido,
        avaliado.Avaliacao.EtapaAusente is null
            ? null
            : new EtapaAusenteDto(
                avaliado.Avaliacao.EtapaAusente.Posicao,
                avaliado.Avaliacao.EtapaAusente.OrigemId,
                avaliado.Avaliacao.EtapaAusente.DestinoId,
                avaliado.Avaliacao.EtapaAusente.Motivo),
        avaliado.Avaliacao.BandasPercorridas);
}
