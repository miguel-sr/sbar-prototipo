using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Matriz de Rastreabilidade — liga cada elemento relevante do Mapa (Gargalo, Aresta Crítica, Aresta Externa) ao eixo do Radar que ele alimenta.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/matriz-rastreabilidade")]
[Produces("application/json")]
public class RastreabilidadeController(IServicoDeRastreabilidade servico) : ControllerBase
{
    /// <summary>
    /// Monta a Matriz de Rastreabilidade a partir do Mapa atual. Só lista elementos que de fato
    /// alimentam algum dos dez eixos: Nó gargalo em Backstage/Suporte (eixo DISP), Aresta Crítica
    /// (eixo TR) e Aresta ligada à Banda Externo (eixo INT). Recalculada a cada chamada (RF-11).
    /// </summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <response code="200">Linhas da matriz (pode ser vazia, se o Mapa não tiver nenhum elemento relevante ainda).</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LinhaDeRastreabilidadeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LinhaDeRastreabilidadeDto>>> Obter(Guid casoId)
    {
        var linhas = await servico.ObterAsync(casoId);
        return Ok(linhas.Select(LinhaDeRastreabilidadeDto.DeLinha).ToList());
    }
}
