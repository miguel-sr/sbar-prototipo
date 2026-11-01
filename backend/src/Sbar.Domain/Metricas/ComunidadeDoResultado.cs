namespace Sbar.Domain.Metricas;

// Uma comunidade estrutural do Mapa — TCC revisado 4.4.1. Indice vai de 1 a n (numeradas pela menor posição de
// cadastro dos seus nós); SaidasExternas = arestas que partem de nós da comunidade para nós de outra (Eixo 8, AUT).
public sealed record ComunidadeDoResultado(int Indice, IReadOnlyList<Guid> NosIds, int SaidasExternas);
