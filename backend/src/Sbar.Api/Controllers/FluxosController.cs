using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Fluxos principais (F) de um Estudo de Caso — sequências de Nós cadastradas pelo usuário e verificadas contra as Arestas do Mapa.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/fluxos")]
[Produces("application/json")]
public class FluxosController(IServicoDeFluxos servico) : ControllerBase
{
    /// <summary>Cadastra um Fluxo principal. Cada par consecutivo de Nós precisa ter uma Aresta origem→destino.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="request">Nome e Nós do caminho, em ordem.</param>
    /// <response code="201">Fluxo criado.</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    /// <response code="422">Nome inválido, menos de 2 Nós, Nó de outro Estudo de Caso ou etapa sem Aresta (a mensagem indica a etapa).</response>
    [HttpPost]
    [ProducesResponseType(typeof(FluxoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FluxoDto>> Criar(Guid casoId, CriarFluxoRequest request)
    {
        var fluxo = await servico.CriarAsync(casoId, request.Nome, request.NosIds);

        return CreatedAtAction(nameof(Listar), new { casoId }, FluxoDto.DeAvaliado(fluxo));
    }

    /// <summary>Lista os Fluxos do Estudo de Caso, em ordem de cadastro, cada um com a validade avaliada contra o Mapa atual.</summary>
    /// <response code="200">Lista de Fluxos (pode ser vazia).</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FluxoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<FluxoDto>>> Listar(Guid casoId)
    {
        var fluxos = await servico.ListarAsync(casoId);

        return Ok(fluxos.Select(FluxoDto.DeAvaliado).ToList());
    }

    /// <summary>Substitui nome e sequência de Nós de um Fluxo, com as mesmas regras da criação.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="fluxoId">Id do Fluxo.</param>
    /// <param name="request">Novo nome e novos Nós, em ordem.</param>
    /// <response code="200">Fluxo atualizado.</response>
    /// <response code="404">Estudo de Caso ou Fluxo não existem.</response>
    /// <response code="422">Nome inválido, menos de 2 Nós, Nó de outro Estudo de Caso ou etapa sem Aresta.</response>
    [HttpPut("{fluxoId:guid}")]
    [ProducesResponseType(typeof(FluxoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FluxoDto>> Atualizar(Guid casoId, Guid fluxoId, AtualizarFluxoRequest request)
    {
        var fluxo = await servico.AtualizarAsync(casoId, fluxoId, request.Nome, request.NosIds);

        return Ok(FluxoDto.DeAvaliado(fluxo));
    }

    /// <summary>Exclui um Fluxo.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="fluxoId">Id do Fluxo.</param>
    /// <response code="204">Fluxo excluído.</response>
    /// <response code="404">Estudo de Caso ou Fluxo não existem.</response>
    [HttpDelete("{fluxoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid casoId, Guid fluxoId)
    {
        await servico.RemoverAsync(casoId, fluxoId);

        return NoContent();
    }
}
