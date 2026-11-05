using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Api.Dtos;

/// <summary>Quanto um eixo contribui para a distância ao quadrado até um perfil de referência (Quadros 26/34 do TCC revisado).</summary>
/// <param name="Eixo">CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL ou DEP.</param>
/// <param name="Contribuicao">p'_i × (Norm_i do serviço − Norm_i do perfil)², sem arredondar, com p'_i o peso efetivo (renormalizado entre os eixos aplicáveis). A soma das contribuições é a distância ao quadrado.</param>
public record ContribuicaoDeEixoDto(Eixo Eixo, double Contribuicao)
{
    public static ContribuicaoDeEixoDto DeContribuicao(ContribuicaoDeEixo contribuicao) =>
        new(contribuicao.Eixo, contribuicao.Contribuicao);
}

/// <summary>Distância euclidiana ponderada do Estudo de Caso até um Perfil Arquitetural de referência.</summary>
/// <param name="PerfilId">Id do Perfil Arquitetural.</param>
/// <param name="PerfilNome">Nome do Perfil Arquitetural.</param>
/// <param name="Distancia">Quanto menor, mais parecido o Estudo de Caso é desse estilo.</param>
/// <param name="PercentualDoMaximo">Distância como percentual do máximo possível (5): distância ÷ 5 × 100 (TCC 4.6).</param>
/// <param name="Contribuicoes">Contribuição de cada eixo aplicável para a distância ao quadrado.</param>
public record DistanciaPerfilDto(
    Guid PerfilId, string PerfilNome, double Distancia, double PercentualDoMaximo,
    IReadOnlyList<ContribuicaoDeEixoDto> Contribuicoes)
{
    public static DistanciaPerfilDto DeDistancia(DistanciaPerfil distancia) => new(
        distancia.PerfilId,
        distancia.PerfilNome,
        distancia.Distancia,
        distancia.PercentualDoMaximo,
        distancia.Contribuicoes.Select(ContribuicaoDeEixoDto.DeContribuicao).ToList());
}
