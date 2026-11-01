namespace Sbar.Api.Dtos;

/// <summary>Identificação do Estudo de Caso exportado.</summary>
/// <param name="Id">Id do Estudo de Caso.</param>
/// <param name="Nome">Nome do sistema/produto mapeado.</param>
/// <param name="DominioDescricao">Descrição do domínio (pode ser <c>null</c>).</param>
public record CasoExportadoDto(Guid Id, string Nome, string? DominioDescricao);

/// <summary>
/// Resultado completo de uma análise (RF13; TCC 4.6.1 manda registrar tudo isto): entradas (nós, arestas com justificativa, fluxos),
/// parâmetros usados (com a versão), comunidades e Q, métricas brutas e normalizadas, distâncias com contribuições e a matriz.
/// </summary>
/// <param name="GeradoEm">Data/hora da exportação (UTC).</param>
/// <param name="Caso">Estudo de Caso exportado.</param>
/// <param name="VersaoDosParametros">Versão dos parâmetros usada.</param>
/// <param name="Parametros">Limites, pesos e assinaturas dessa versão.</param>
/// <param name="Nos">Nós, em ordem de cadastro.</param>
/// <param name="Arestas">Arestas, com justificativa.</param>
/// <param name="Fluxos">Fluxos principais, com validade.</param>
/// <param name="Comunidades">Comunidades estruturais (4.4.1).</param>
/// <param name="ModularidadeQ">Modularidade Q; <c>null</c> sem arestas.</param>
/// <param name="Avisos">Avisos de leitura (comunidade única, Q &lt; 0,3, fluxos inválidos…).</param>
/// <param name="Bandas">μ, σ e limiar de cada banda.</param>
/// <param name="Eixos">Os dez eixos, bruto e normalizado.</param>
/// <param name="Recomendacao">Perfil de referência mais próximo, distâncias, contribuições e margem (<c>null</c> sem assinaturas).</param>
/// <param name="Matriz">Matriz de rastreabilidade.</param>
public record ExportacaoDto(
    DateTime GeradoEm,
    CasoExportadoDto Caso,
    VersaoDeParametrosDto VersaoDosParametros,
    ConjuntoDeParametrosDto Parametros,
    IReadOnlyList<NoDto> Nos,
    IReadOnlyList<ArestaDto> Arestas,
    IReadOnlyList<FluxoDto> Fluxos,
    IReadOnlyList<ComunidadeDto> Comunidades,
    double? ModularidadeQ,
    IReadOnlyList<string> Avisos,
    IReadOnlyList<EstatisticaDaBandaDto> Bandas,
    IReadOnlyList<EixoResultadoDto> Eixos,
    RecomendacaoDto? Recomendacao,
    IReadOnlyList<LinhaDeRastreabilidadeDto> Matriz);
