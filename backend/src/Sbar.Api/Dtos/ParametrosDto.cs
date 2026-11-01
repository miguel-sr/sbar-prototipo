using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;

namespace Sbar.Api.Dtos;

/// <summary>Identificação de uma versão dos parâmetros de análise (limites, pesos e assinaturas) — RF11/US06.</summary>
/// <param name="Numero">Número sequencial da versão (1, 2, 3…).</param>
/// <param name="CriadaEm">Data/hora de criação (UTC).</param>
/// <param name="Descricao">O que mudou nesta versão (ex.: "Assinatura 'Monólito Modular (provisório)' atualizada").</param>
public record VersaoDeParametrosDto(int Numero, DateTime CriadaEm, string Descricao)
{
    public static VersaoDeParametrosDto DeInfo(VersaoDeParametrosInfo versao) =>
        new(versao.Numero, versao.CriadaEm, versao.Descricao);
}

/// <summary>Limites de normalização (Quadro 7) e peso p_i de um eixo.</summary>
/// <param name="Eixo">CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL ou DEP.</param>
/// <param name="Minimo">Limite mínimo de referência M_i^min.</param>
/// <param name="Maximo">Limite máximo de referência M_i^max (maior que o mínimo).</param>
/// <param name="Peso">Peso p_i na distância euclidiana ponderada; a soma dos dez pesos deve ser 1.</param>
public record ParametroDeEixoDto(Eixo Eixo, double Minimo, double Maximo, double Peso)
{
    public static ParametroDeEixoDto DeParametro(ParametroDeEixo parametro) =>
        new(parametro.Eixo, parametro.Minimo, parametro.Maximo, parametro.Peso);
}

/// <summary>Assinatura arquitetural de referência (Quadro 8) congelada numa versão dos parâmetros.</summary>
/// <param name="Id">Id do Perfil Arquitetural correspondente.</param>
/// <param name="Nome">Nome do estilo (ex.: "Monólito Modular (provisório)").</param>
/// <param name="Valores">Valor 0–5 de cada eixo, com as chaves CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL e DEP.</param>
public record AssinaturaDeReferenciaDto(Guid Id, string Nome, IReadOnlyDictionary<string, double> Valores)
{
    public static AssinaturaDeReferenciaDto DeAssinatura(AssinaturaDeReferencia assinatura) => new(
        assinatura.Id,
        assinatura.Nome,
        assinatura.Valores.OrderBy(v => v.Key).ToDictionary(v => v.Key.ToString(), v => v.Value));
}

/// <summary>Conjunto de parâmetros de uma versão: limites e pesos dos dez eixos mais as assinaturas de referência.</summary>
/// <param name="Versao">Versão a que o conjunto pertence.</param>
/// <param name="Eixos">Limites e pesos, um item por eixo.</param>
/// <param name="Assinaturas">Assinaturas de referência vigentes naquela versão.</param>
public record ConjuntoDeParametrosDto(
    VersaoDeParametrosDto Versao,
    IReadOnlyList<ParametroDeEixoDto> Eixos,
    IReadOnlyList<AssinaturaDeReferenciaDto> Assinaturas)
{
    public static ConjuntoDeParametrosDto DeConjunto(ConjuntoDeParametros conjunto) => new(
        VersaoDeParametrosDto.DeInfo(conjunto.Versao),
        conjunto.Eixos.OrderBy(e => e.Eixo).Select(ParametroDeEixoDto.DeParametro).ToList(),
        conjunto.Assinaturas.Select(AssinaturaDeReferenciaDto.DeAssinatura).ToList());
}
