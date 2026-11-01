using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;

namespace Sbar.Api.Requests;

/// <summary>Limites de normalização e peso de um eixo, enviados ao editar os parâmetros de análise.</summary>
/// <param name="Eixo">CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL ou DEP.</param>
/// <param name="Minimo">Limite mínimo de referência (menor que o máximo).</param>
/// <param name="Maximo">Limite máximo de referência.</param>
/// <param name="Peso">Peso p_i (≥ 0); a soma dos dez pesos precisa ser 1.</param>
public record ParametroDeEixoRequest(Eixo Eixo, double Minimo, double Maximo, double Peso)
{
    public ParametroDeEixo ParaParametro() => new(Eixo, Minimo, Maximo, Peso);
}

/// <summary>Substituição dos limites e pesos dos dez eixos — cria uma nova versão dos parâmetros (RF11).</summary>
/// <param name="Descricao">O que mudou e por quê (opcional, até 200 caracteres). Vazio vira "Limites e pesos atualizados".</param>
/// <param name="Eixos">Os dez eixos, cada um uma vez; mínimo &lt; máximo, peso ≥ 0 e Σ pesos = 1 (422 se violar — CT10).</param>
public record AtualizarLimitesEPesosRequest(string? Descricao, IReadOnlyList<ParametroDeEixoRequest> Eixos);
