using Sbar.Domain.Entities;

namespace Sbar.Application.Grafo;

public static class CalculadoraDeCarga
{
    // carga(v) — TCC seção 4.3.3: soma dos pesos de todas as arestas incidentes a v (entrada + saída)
    public static IReadOnlyDictionary<Guid, int> Calcular(EstudoDeCaso estudoDeCaso)
    {
        var cargas = estudoDeCaso.Nos.ToDictionary(n => n.Id, _ => 0);

        foreach (var aresta in estudoDeCaso.Arestas)
        {
            cargas[aresta.NoOrigemId] += aresta.Peso.Valor;
            cargas[aresta.NoDestinoId] += aresta.Peso.Valor;
        }

        return cargas;
    }
}
