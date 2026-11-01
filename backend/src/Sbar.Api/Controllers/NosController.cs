using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Nós de um Estudo de Caso — Atores, Touchpoints, Processos, Suporte, Dados e Externo.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/nos")]
[Produces("application/json")]
public class NosController(IServicoDeNos servico) : ControllerBase
{
    /// <summary>Cria um Nó no Mapa do Estudo de Caso. A Banda é derivada do Tipo automaticamente (nunca escolhida aqui).</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="request">Nome e Tipo do Nó.</param>
    /// <response code="201">Nó criado, com a Banda já derivada.</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    /// <response code="422">Nome vazio/só espaços ou acima de 100 caracteres.</response>
    [HttpPost]
    [ProducesResponseType(typeof(NoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NoDto>> Criar(Guid casoId, CriarNoRequest request)
    {
        var no = await servico.CriarAsync(casoId, request.Nome, request.Tipo);

        return CreatedAtAction(nameof(Listar), new { casoId }, NoDto.DeEntidade(no));
    }

    /// <summary>Lista todos os Nós já cadastrados no Estudo de Caso.</summary>
    /// <response code="200">Lista de Nós (pode ser vazia).</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<NoDto>>> Listar(Guid casoId)
    {
        var nos = await servico.ListarAsync(casoId);

        return Ok(nos.Select(NoDto.DeEntidade).ToList());
    }

    /// <summary>Atualiza nome e tipo de um Nó. A Banda é re-derivada do novo Tipo (as Arestas dele continuam).</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="noId">Id do Nó.</param>
    /// <param name="request">Novo nome e tipo.</param>
    /// <response code="200">Nó atualizado.</response>
    /// <response code="404">Estudo de Caso ou Nó não existem.</response>
    /// <response code="422">Nome vazio/só espaços ou acima de 100 caracteres.</response>
    [HttpPut("{noId:guid}")]
    [ProducesResponseType(typeof(NoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NoDto>> Atualizar(Guid casoId, Guid noId, AtualizarNoRequest request)
    {
        var no = await servico.AtualizarAsync(casoId, noId, request.Nome, request.Tipo);

        return Ok(NoDto.DeEntidade(no));
    }

    /// <summary>Exclui um Nó e todas as Arestas que entram ou saem dele.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="noId">Id do Nó.</param>
    /// <response code="204">Nó (e as Arestas ligadas a ele) excluído.</response>
    /// <response code="404">Estudo de Caso ou Nó não existem.</response>
    [HttpDelete("{noId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid casoId, Guid noId)
    {
        await servico.RemoverAsync(casoId, noId);

        return NoContent();
    }
}
