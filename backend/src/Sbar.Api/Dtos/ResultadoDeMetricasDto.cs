using Sbar.Domain.Enums;
using Sbar.Domain.Metricas;

namespace Sbar.Api.Dtos;

/// <summary>Uma comunidade estrutural do Mapa (TCC revisado 4.4.1).</summary>
/// <param name="Indice">Número da comunidade (1 a n), na ordem da menor posição de cadastro dos seus Nós.</param>
/// <param name="NosIds">Ids dos Nós da comunidade, em ordem de cadastro.</param>
/// <param name="SaidasExternas">Arestas que partem de Nós da comunidade para Nós de outra comunidade (base do eixo AUT: autônoma quando ≤ 1).</param>
public record ComunidadeDto(int Indice, IReadOnlyList<Guid> NosIds, int SaidasExternas)
{
    public static ComunidadeDto DeResultado(ComunidadeDoResultado comunidade) =>
        new(comunidade.Indice, comunidade.NosIds, comunidade.SaidasExternas);
}

/// <summary>μ, σ e limiar de uma Banda ocupada (TCC 4.2.6): um Nó é potencialmente crítico quando sua carga é estritamente maior que o limiar.</summary>
/// <param name="Banda">Banda (Ator, Frontstage, Backstage, Suporte ou Externo).</param>
/// <param name="QuantidadeDeNos">Número de Nós da Banda.</param>
/// <param name="Media">μ — média das cargas dos Nós da Banda.</param>
/// <param name="DesvioPadrao">σ — desvio padrão populacional das cargas.</param>
/// <param name="Limiar">μ + σ.</param>
public record EstatisticaDaBandaDto(Banda Banda, int QuantidadeDeNos, double Media, double DesvioPadrao, double Limiar)
{
    public static EstatisticaDaBandaDto DeResultado(EstatisticaDaBandaDoResultado estatistica) =>
        new(estatistica.Banda, estatistica.QuantidadeDeNos, estatistica.Media, estatistica.DesvioPadrao, estatistica.Limiar);
}

/// <summary>Resumo de um Fluxo principal no resultado das métricas — só os válidos entram no cálculo de DEP.</summary>
/// <param name="Id">Id do Fluxo.</param>
/// <param name="Nome">Nome do Fluxo.</param>
/// <param name="Valido">Se o caminho dirigido do fluxo existe no Mapa atual.</param>
/// <param name="BandasPercorridas">Bandas distintas atravessadas pelo caminho.</param>
public record FluxoDoResultadoDto(Guid Id, string Nome, bool Valido, int BandasPercorridas)
{
    public static FluxoDoResultadoDto DeResultado(FluxoDoResultado fluxo) =>
        new(fluxo.Id, fluxo.Nome, fluxo.Valido, fluxo.BandasPercorridas);
}

/// <summary>Resultado completo do Radar Arquitetural: os dez eixos, a comparação com as assinaturas de referência e os dados que explicam o cálculo.</summary>
/// <param name="Eixos">Sempre 10 itens, um por eixo (CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP).</param>
/// <param name="Recomendacao"><c>null</c> quando não há nenhum Perfil Arquitetural cadastrado em <c>/api/perfis-arquiteturais</c>. É um subsídio à decisão do arquiteto (o perfil de referência mais próximo), não uma decisão.</param>
/// <param name="NosGargalo">Ids de todos os Nós que são gargalo (carga &gt; μ + σ da própria Banda), de qualquer Banda — usado para destacá-los no Mapa.</param>
/// <param name="Comunidades">Comunidades estruturais do Mapa (TCC revisado 4.4.1), base de FLEX e AUT.</param>
/// <param name="ModularidadeQ">Modularidade Q da partição em comunidades; <c>null</c> quando o Mapa não tem arestas (Q indefinido). Q &gt; ~0,3 indica estrutura de comunidades significativa.</param>
/// <param name="Avisos">Situações que pedem cautela na leitura de FLEX/AUT (comunidade única, Mapa sem arestas, Q &lt; 0,3) e fluxos inválidos.</param>
/// <param name="Bandas">μ, σ e limiar μ + σ de cada Banda ocupada (critério de gargalo).</param>
/// <param name="Fluxos">Fluxos principais cadastrados, com validade; só os válidos entram em DEP.</param>
/// <param name="VersaoDosParametros">Versão dos limites, pesos e assinaturas usada nesta análise (RF11, US06).</param>
public record ResultadoDeMetricasDto(
    IReadOnlyList<EixoResultadoDto> Eixos,
    RecomendacaoDto? Recomendacao,
    IReadOnlyList<Guid> NosGargalo,
    IReadOnlyList<ComunidadeDto> Comunidades,
    double? ModularidadeQ,
    IReadOnlyList<string> Avisos,
    IReadOnlyList<EstatisticaDaBandaDto> Bandas,
    IReadOnlyList<FluxoDoResultadoDto> Fluxos,
    VersaoDeParametrosDto VersaoDosParametros)
{
    public static ResultadoDeMetricasDto DeResultado(ResultadoDeMetricas resultado) => new(
        resultado.Eixos.Select(EixoResultadoDto.DeResultado).ToList(),
        resultado.Recomendacao is null ? null : RecomendacaoDto.DeRecomendacao(resultado.Recomendacao),
        resultado.NosGargalo,
        resultado.Comunidades.Select(ComunidadeDto.DeResultado).ToList(),
        resultado.ModularidadeQ,
        resultado.Avisos,
        resultado.Bandas.Select(EstatisticaDaBandaDto.DeResultado).ToList(),
        resultado.Fluxos.Select(FluxoDoResultadoDto.DeResultado).ToList(),
        VersaoDeParametrosDto.DeInfo(resultado.VersaoDosParametros));
}
