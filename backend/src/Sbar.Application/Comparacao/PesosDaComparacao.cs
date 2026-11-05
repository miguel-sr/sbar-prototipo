using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Application.Comparacao;

// Pesos realmente usados na comparação com os perfis (RN07/RN08): Efetivos = p'_i dos eixos aplicáveis (somam 1);
// Omitidos = eixos "não aplicável". Efetivos vazio significa que não há eixo aplicável com peso para comparar.
public sealed record PesosDaComparacao(IReadOnlyList<PesoEfetivoDeEixo> Efetivos, IReadOnlyList<Eixo> Omitidos)
{
    public bool PodeComparar => Efetivos.Count > 0;
}
