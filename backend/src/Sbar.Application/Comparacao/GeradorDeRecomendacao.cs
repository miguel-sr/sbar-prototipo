using Sbar.Domain.Metricas;

namespace Sbar.Application.Comparacao;

public static class GeradorDeRecomendacao
{
    // RF09/RN09 — o perfil de referência de menor distância é um subsídio à decisão do arquiteto (TCC 4.6).
    // Margem: diferença entre a segunda menor e a menor distância — margem pequena indica serviço intermediário entre
    // os estilos e comparação que pouco os diferencia (TCC 7.10.1: 0,239 no CE2 contra 0,553 no CE1).
    public static Recomendacao Gerar(IReadOnlyList<DistanciaPerfil> distancias)
    {
        var ordenadas = distancias.OrderBy(d => d.Distancia).ToList();
        var maisProximo = ordenadas[0];
        double? margem = ordenadas.Count >= 2 ? ordenadas[1].Distancia - maisProximo.Distancia : null;

        return new Recomendacao(maisProximo.PerfilNome, distancias, margem, ComparadorDePerfis.DistanciaMaxima);
    }
}
