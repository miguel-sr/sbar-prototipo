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
public record RecomendacaoDto(
    string EstiloRecomendado, IReadOnlyList<DistanciaPerfilDto> Distancias, double? Margem, double DistanciaMaxima)
{
    public static RecomendacaoDto DeRecomendacao(Recomendacao recomendacao) => new(
        recomendacao.EstiloRecomendado,
        recomendacao.Distancias.Select(DistanciaPerfilDto.DeDistancia).ToList(),
        recomendacao.Margem,
        recomendacao.DistanciaMaxima);
}
