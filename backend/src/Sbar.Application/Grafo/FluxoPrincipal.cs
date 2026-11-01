using Sbar.Domain.Entities;

namespace Sbar.Application.Grafo;

// f ∈ F — TCC revisado seção 4.4.2 (Eixo 10, DEP) e RF05: fluxo principal cadastrado, já avaliado contra o Mapa
// atual. Caminho é a sequência de Ids de Nó do fluxo. Só os fluxos VÁLIDOS (caminho dirigido completo) entram no
// DEP; os inválidos continuam visíveis, com a etapa ausente indicada (US03, CT09).
public sealed record FluxoPrincipal(
    Guid Id,
    string Nome,
    IReadOnlyList<Guid> Caminho,
    int BandasPercorridas,
    bool Valido,
    EtapaAusente? EtapaAusente);
