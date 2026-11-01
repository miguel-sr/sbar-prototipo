namespace Sbar.Domain.Entities;

// Primeira etapa (par consecutivo de nós) do fluxo sem aresta origem→destino no Mapa atual. Posicao é 1-based:
// etapa N liga o N-ésimo nó do fluxo ao (N+1)-ésimo (US03: "fluxos sem caminho são sinalizados, com indicação
// da etapa ausente"). Os ids são os do fluxo, mesmo que o Nó já tenha sido removido do Mapa.
public sealed record EtapaAusente(int Posicao, Guid OrigemId, Guid DestinoId, string Motivo);

// Resultado da verificação de caminho dirigido de um fluxo contra o Mapa atual (CT09). BandasPercorridas é o
// número de bandas distintas dos nós do caminho, incluindo origem e destino (TCC 4.4.2, Eixo 10).
public sealed record AvaliacaoDeFluxo(bool Valido, EtapaAusente? EtapaAusente, int BandasPercorridas);

// Um fluxo cadastrado junto da sua avaliação no momento da leitura (nada disto é persistido — RF-11).
public sealed record FluxoAvaliado(Fluxo Fluxo, AvaliacaoDeFluxo Avaliacao);
