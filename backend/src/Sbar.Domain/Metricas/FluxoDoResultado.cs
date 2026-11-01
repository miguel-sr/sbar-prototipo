namespace Sbar.Domain.Metricas;

// Resumo de um fluxo principal cadastrado, avaliado contra o Mapa atual — só os válidos entram no DEP (Eixo 10).
public sealed record FluxoDoResultado(Guid Id, string Nome, bool Valido, int BandasPercorridas);
