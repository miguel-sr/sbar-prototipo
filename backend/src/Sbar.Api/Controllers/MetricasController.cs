using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Radar Arquitetural (dez eixos) e recomendação preliminar de estilo arquitetural.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/metricas")]
[Produces("application/json")]
public class MetricasController(IServicoDeMetricas servico) : ControllerBase
{
    /// <summary>
    /// Calcula os dez eixos do Radar Arquitetural a partir do Mapa atual (Nós/Arestas) e, se houver
    /// algum Perfil Arquitetural cadastrado, a recomendação preliminar de estilo (o de menor distância).
    /// Tudo é recalculado nesta chamada — nada fica salvo entre uma chamada e outra (RF-11).
    /// </summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="versao">Número de uma versão dos parâmetros (limites, pesos e assinaturas) a reaplicar — protocolo de avaliação, 5.4. Sem ela, usa a vigente.</param>
    /// <response code="200">Os dez eixos (bruto + normalizado 0–5, ou "não aplicável" com motivo) e a recomendação (ou <c>null</c> sem nenhum Perfil Arquitetural cadastrado).</response>
    /// <response code="404">Estudo de Caso ou versão dos parâmetros não existem.</response>
    /// <response code="422">A soma dos pesos da versão usada não é 1 — o cálculo é bloqueado (RN07, CT10).</response>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoDeMetricasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResultadoDeMetricasDto>> Obter(Guid casoId, [FromQuery] int? versao = null)
    {
        var resultado = await servico.CalcularAsync(casoId, versao);
        return Ok(ResultadoDeMetricasDto.DeResultado(resultado));
    }
}
