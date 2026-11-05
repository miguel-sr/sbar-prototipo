using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Api.Dtos;

/// <summary>
/// Perfil de referência mais próximo — um <b>subsídio</b> à decisão do arquiteto, nunca uma decisão (TCC 4.6, RN09). Os
/// nomes <c>recomendacao</c>/<c>estiloRecomendado</c> são mantidos por compatibilidade do contrato (D14, opção 1).
/// </summary>
/// <param name="EstiloRecomendado">Nome do Perfil Arquitetural (assinatura de referência) mais próximo.</param>
/// <param name="Distancias">Distância até cada Perfil Arquitetural cadastrado, não só o vencedor — útil pro front mostrar todas.</param>
/// <param name="Margem">Segunda menor distância − menor distância; margem pequena indica serviço intermediário entre os estilos. <c>null</c> com menos de 2 perfis.</param>
/// <param name="DistanciaMaxima">Maior distância possível (5), para ler cada distância também como percentual.</param>
/// <param name="PesosEfetivos">Pesos p'_i realmente usados na distância: os pesos configurados dos eixos aplicáveis, renormalizados para somar 1 (RN07/RN08). Com os dez eixos aplicáveis, iguais aos configurados.</param>
/// <param name="EixosOmitidos">Eixos "não aplicável" que ficaram fora da distância (vazio quando os dez eixos são aplicáveis).</param>
public record RecomendacaoDto(
    string EstiloRecomendado, IReadOnlyList<DistanciaPerfilDto> Distancias, double? Margem, double DistanciaMaxima,
    IReadOnlyList<PesoEfetivoDeEixoDto> PesosEfetivos, IReadOnlyList<Eixo> EixosOmitidos)
{
    public static RecomendacaoDto DeRecomendacao(Recomendacao recomendacao) => new(
        recomendacao.EstiloRecomendado,
        recomendacao.Distancias.Select(DistanciaPerfilDto.DeDistancia).ToList(),
        recomendacao.Margem,
        recomendacao.DistanciaMaxima,
        recomendacao.PesosEfetivos.Select(PesoEfetivoDeEixoDto.DePeso).ToList(),
        recomendacao.EixosOmitidos);
}

/// <summary>Peso de um eixo na distância: o configurado e o efetivo (renormalizado entre os eixos aplicáveis).</summary>
/// <param name="Eixo">CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL ou DEP.</param>
/// <param name="PesoConfigurado">p_i do conjunto de parâmetros.</param>
/// <param name="PesoEfetivo">p'_i = p_i ÷ Σ p_j dos eixos aplicáveis (RN07/RN08); a soma dos efetivos é 1.</param>
public record PesoEfetivoDeEixoDto(Eixo Eixo, double PesoConfigurado, double PesoEfetivo)
{
    public static PesoEfetivoDeEixoDto DePeso(PesoEfetivoDeEixo peso) => new(peso.Eixo, peso.PesoConfigurado, peso.PesoEfetivo);
}
