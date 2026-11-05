using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>
/// Parâmetros de análise versionados (RF11, US06): limites de normalização (Quadro 6), pesos p_i dos eixos e assinaturas de
/// referência (Quadro 7). Cada alteração — aqui ou em <c>/api/perfis-arquiteturais</c> — grava uma nova versão imutável; cada
/// análise informa a versão usada e uma versão antiga pode ser reaplicada com <c>?versao=N</c> em <c>/metricas</c>.
/// </summary>
[ApiController]
[Route("api/parametros")]
[Produces("application/json")]
public class ParametrosController(IServicoDeParametros servico) : ControllerBase
{
    /// <summary>Lista as versões dos parâmetros, da mais recente para a mais antiga (a versão 1 nasce sob demanda).</summary>
    /// <response code="200">Versões (número, data e descrição).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VersaoDeParametrosDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VersaoDeParametrosDto>>> Listar()
    {
        var versoes = await servico.ListarVersoesAsync();
        return Ok(versoes.Select(VersaoDeParametrosDto.DeInfo).ToList());
    }

    /// <summary>Conjunto vigente: limites e pesos dos dez eixos e as assinaturas de referência, com o número da versão.</summary>
    /// <response code="200">Parâmetros vigentes.</response>
    [HttpGet("atual")]
    [ProducesResponseType(typeof(ConjuntoDeParametrosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConjuntoDeParametrosDto>> ObterAtual()
    {
        var conjunto = await servico.ObterAtualAsync();
        return Ok(ConjuntoDeParametrosDto.DeConjunto(conjunto));
    }

    /// <summary>Snapshot imutável de uma versão dos parâmetros (o que valia naquele momento).</summary>
    /// <param name="numero">Número da versão.</param>
    /// <response code="200">Parâmetros daquela versão.</response>
    /// <response code="404">Nenhuma versão com esse número.</response>
    [HttpGet("{numero:int}")]
    [ProducesResponseType(typeof(ConjuntoDeParametrosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConjuntoDeParametrosDto>> ObterPorNumero(int numero)
    {
        var conjunto = await servico.ObterVersaoAsync(numero);
        return Ok(ConjuntoDeParametrosDto.DeConjunto(conjunto));
    }

    /// <summary>
    /// Substitui limites e pesos dos dez eixos e grava uma nova versão. Os dez eixos são obrigatórios (cada um uma vez),
    /// mínimo &lt; máximo, peso ≥ 0 e a soma dos pesos deve ser 1 (RN07, CT10).
    /// </summary>
    /// <response code="200">Conjunto da nova versão.</response>
    /// <response code="422">Eixo ausente/repetido, mínimo ≥ máximo, peso negativo ou soma dos pesos ≠ 1.</response>
    [HttpPut("limites-e-pesos")]
    [ProducesResponseType(typeof(ConjuntoDeParametrosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ConjuntoDeParametrosDto>> AtualizarLimitesEPesos(AtualizarLimitesEPesosRequest request)
    {
        var conjunto = await servico.AtualizarLimitesEPesosAsync(
            request.Descricao, request.Eixos.Select(e => e.ParaParametro()).ToList());

        return Ok(ConjuntoDeParametrosDto.DeConjunto(conjunto));
    }
}
