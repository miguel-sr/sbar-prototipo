namespace Sbar.Application.Grafo;

// Partição do Mapa em comunidades estruturais — TCC revisado seção 4.4.1 (cluster(v) por modularidade).
// IndicePorNo: índice da comunidade de cada nó, de 1 a n (numeradas pela menor posição de cadastro dos
// seus nós). Membros[i - 1]: nós da comunidade i, em ordem de cadastro. ModularidadeQ é null quando o
// Mapa não tem arestas (m = 0 — Q indefinido). Avisos: situações em que a leitura de FLEX/AUT pede
// cautela (comunidade única, m = 0, Q abaixo de 0,3) — o TCC manda registrar Q junto dos resultados.
public sealed record ResultadoDeComunidades(
    IReadOnlyDictionary<Guid, int> IndicePorNo,
    IReadOnlyList<IReadOnlyList<Guid>> Membros,
    double? ModularidadeQ,
    IReadOnlyList<string> Avisos)
{
    public int Quantidade => Membros.Count;
}
