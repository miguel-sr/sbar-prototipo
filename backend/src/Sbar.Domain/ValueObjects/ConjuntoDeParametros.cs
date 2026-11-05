using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;

namespace Sbar.Domain.ValueObjects;

// Parâmetros de uma análise (RF11): limites de normalização (Quadro 6), pesos p_i dos eixos (4.5.1) e assinaturas
// de referência (Quadro 7), mais a versão a que pertencem. Imutável: uma versão antiga pode ser reaplicada
// aos Estudos de Caso (protocolo de avaliação, 5.4).
public sealed record ConjuntoDeParametros(
    VersaoDeParametrosInfo Versao,
    IReadOnlyList<ParametroDeEixo> Eixos,
    IReadOnlyList<AssinaturaDeReferencia> Assinaturas)
{
    // Tolerância para a soma dos pesos (RN07: Σ p_i = 1) — dez pesos de 0,1 em ponto flutuante somam 0,9999999999999999.
    public const double ToleranciaDaSomaDosPesos = 1e-9;

    public IReadOnlyDictionary<Eixo, LimitesDeEixo> Limites =>
        Eixos.ToDictionary(e => e.Eixo, e => new LimitesDeEixo(e.Minimo, e.Maximo));

    public IReadOnlyDictionary<Eixo, double> Pesos =>
        Eixos.ToDictionary(e => e.Eixo, e => e.Peso);

    public double SomaDosPesos => Eixos.Sum(e => e.Peso);

    // RN07/CT10 — a soma dos pesos precisa ser 1; senão o cálculo é bloqueado.
    public void ValidarSomaDosPesos() => ValidarEixos(Eixos);

    // Valida os dez parâmetros de eixo: cada eixo uma vez, mínimo < máximo, peso ≥ 0 e Σ p_i = 1.
    public static void ValidarEixos(IReadOnlyList<ParametroDeEixo> eixos)
    {
        foreach (var eixo in Enum.GetValues<Eixo>())
        {
            var ocorrencias = eixos.Count(e => e.Eixo == eixo);
            if (ocorrencias == 0)
                throw ParametrosInvalidosException.EixoAusente(eixo);
            if (ocorrencias > 1)
                throw ParametrosInvalidosException.EixoRepetido(eixo);
        }

        foreach (var eixo in eixos)
        {
            if (!double.IsFinite(eixo.Minimo) || !double.IsFinite(eixo.Maximo))
                throw ParametrosInvalidosException.ValorNaoNumerico(eixo.Eixo);
            if (eixo.Maximo <= eixo.Minimo)
                throw new LimitesDeNormalizacaoInvalidosException(eixo.Eixo.ToString(), eixo.Minimo, eixo.Maximo);
            if (!double.IsFinite(eixo.Peso) || eixo.Peso < 0)
                throw ParametrosInvalidosException.PesoNegativo(eixo.Eixo, eixo.Peso);
        }

        var soma = eixos.Sum(e => e.Peso);
        if (Math.Abs(soma - 1.0) > ToleranciaDaSomaDosPesos)
            throw ParametrosInvalidosException.SomaDosPesos(soma);
    }
}
