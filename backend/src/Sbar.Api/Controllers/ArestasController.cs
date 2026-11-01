using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Arestas de um Estudo de Caso — as interações entre dois Nós já cadastrados.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/arestas")]
[Produces("application/json")]
public class ArestasController(IServicoDeArestas servico) : ControllerBase
{
    /// <summary>Cria uma Aresta ligando dois Nós já existentes no mesmo Estudo de Caso.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="request">Origem, destino, tipo, peso e justificativa da interação.</param>
    /// <response code="201">Aresta criada.</response>
    /// <response code="404">Estudo de Caso, Nó de origem ou Nó de destino não existem.</response>
    /// <response code="422">Self-loop (origem = destino), Peso fora de {1, 2, 3} ou Justificativa vazia/longa demais.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ArestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ArestaDto>> Criar(Guid casoId, CriarArestaRequest request)
    {
        var aresta = await servico.CriarAsync(casoId, request.NoOrigemId, request.NoDestinoId, request.Tipo, request.Peso, request.Justificativa);

        return CreatedAtAction(nameof(Listar), new { casoId }, ArestaDto.DeEntidade(aresta));
    }

    /// <summary>Lista todas as Arestas já cadastradas no Estudo de Caso.</summary>
    /// <response code="200">Lista de Arestas (pode ser vazia).</response>
    /// <response code="404">Estudo de Caso não existe.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ArestaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ArestaDto>>> Listar(Guid casoId)
    {
        var arestas = await servico.ListarAsync(casoId);

        return Ok(arestas.Select(ArestaDto.DeEntidade).ToList());
    }

    /// <summary>Substitui origem, destino, tipo e peso de uma Aresta, com as mesmas regras da criação.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="arestaId">Id da Aresta.</param>
    /// <param name="request">Novos origem, destino, tipo, peso e justificativa.</param>
    /// <response code="200">Aresta atualizada.</response>
    /// <response code="404">Estudo de Caso ou Aresta não existem.</response>
    /// <response code="422">Self-loop, Peso fora de {1, 2, 3}, Justificativa vazia/longa demais ou Nó que não pertence ao Estudo de Caso.</response>
    [HttpPut("{arestaId:guid}")]
    [ProducesResponseType(typeof(ArestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ArestaDto>> Atualizar(Guid casoId, Guid arestaId, AtualizarArestaRequest request)
    {
        var aresta = await servico.AtualizarAsync(casoId, arestaId, request.NoOrigemId, request.NoDestinoId, request.Tipo, request.Peso, request.Justificativa);

        return Ok(ArestaDto.DeEntidade(aresta));
    }

    /// <summary>Exclui uma Aresta.</summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="arestaId">Id da Aresta.</param>
    /// <response code="204">Aresta excluída.</response>
    /// <response code="404">Estudo de Caso ou Aresta não existem.</response>
    [HttpDelete("{arestaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid casoId, Guid arestaId)
    {
        await servico.RemoverAsync(casoId, arestaId);

        return NoContent();
    }
}
