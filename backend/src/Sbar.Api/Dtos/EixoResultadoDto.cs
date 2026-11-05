using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Api.Dtos;

/// <summary>Resultado de um dos dez eixos do Radar Arquitetural.</summary>
/// <param name="Eixo">CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL ou DEP.</param>
/// <param name="Aplicavel">Se <c>false</c>, o eixo não pôde ser calculado para este Mapa (ver <paramref name="Motivo"/>) e <paramref name="ValorBruto"/>/<paramref name="ValorNormalizado"/> vêm <c>null</c>.</param>
/// <param name="ValorBruto">Valor bruto da fórmula (escala própria de cada eixo).</param>
/// <param name="ValorNormalizado">Valor normalizado na escala 0–5 do Radar.</param>
/// <param name="Motivo">Explicação textual quando <paramref name="Aplicavel"/> é <c>false</c> (ex.: "banda vazia").</param>
public record EixoResultadoDto(Eixo Eixo, bool Aplicavel, double? ValorBruto, double? ValorNormalizado, string? Motivo)
{
    public static EixoResultadoDto DeResultado(ResultadoDeEixo resultado) =>
        new(resultado.Eixo, resultado.Aplicavel, resultado.ValorBruto, resultado.ValorNormalizado, resultado.Motivo);
}
