using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>Estudos de Caso — o "projeto" que reúne o Mapa (Nós/Arestas) de um sistema a ser analisado.</summary>
[ApiController]
[Route("api/casos")]
[Produces("application/json")]
public class CasosController(IServicoDeCasos servico) : ControllerBase
{
    /// <summary>Cria um novo Estudo de Caso, vazio (sem Nós/Arestas ainda).</summary>
    /// <response code="201">Estudo de Caso criado.</response>
    /// <response code="422">Nome vazio/só espaços ou acima de 120 caracteres (descrição acima de 1000).</response>
    [HttpPost]
    [ProducesResponseType(typeof(EstudoDeCasoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EstudoDeCasoDto>> Criar(CriarEstudoDeCasoRequest request)
    {
        var estudoDeCaso = await servico.CriarAsync(request.Nome, request.DominioDescricao);
        var dto = EstudoDeCasoDto.DeEntidade(estudoDeCaso);

        return CreatedAtAction(nameof(ObterPorId), new { id = dto.Id }, dto);
    }

    /// <summary>Lista todos os Estudos de Caso — é a tela inicial do produto (T1 ListaDeCasos).</summary>
    /// <response code="200">Lista de Estudos de Caso (pode ser vazia).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EstudoDeCasoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EstudoDeCasoDto>>> Listar()
    {
        var estudosDeCaso = await servico.ListarAsync();

        return Ok(estudosDeCaso.Select(EstudoDeCasoDto.DeEntidade).ToList());
    }

    /// <summary>Obtém um Estudo de Caso pelo Id, com todos os Nós e Arestas já cadastrados.</summary>
    /// <response code="200">Estudo de Caso encontrado.</response>
    /// <response code="404">Nenhum Estudo de Caso com esse Id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EstudoDeCasoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudoDeCasoDto>> ObterPorId(Guid id)
    {
        var estudoDeCaso = await servico.ObterAsync(id);

        return Ok(EstudoDeCasoDto.DeEntidade(estudoDeCaso));
    }

    /// <summary>Atualiza o nome e a descrição do domínio de um Estudo de Caso (os Nós e Arestas não mudam).</summary>
    /// <param name="id">Id do Estudo de Caso.</param>
    /// <param name="request">Novo nome (obrigatório, até 120 caracteres) e descrição (opcional, até 1000).</param>
    /// <response code="200">Estudo de Caso atualizado.</response>
    /// <response code="404">Nenhum Estudo de Caso com esse Id.</response>
    /// <response code="422">Nome vazio/só espaços ou acima do tamanho máximo.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EstudoDeCasoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EstudoDeCasoDto>> Atualizar(Guid id, AtualizarEstudoDeCasoRequest request)
    {
        var estudoDeCaso = await servico.AtualizarAsync(id, request.Nome, request.DominioDescricao);

        return Ok(EstudoDeCasoDto.DeEntidade(estudoDeCaso));
    }

    /// <summary>Exclui o Estudo de Caso, com todos os Nós e Arestas dele. Não há como desfazer.</summary>
    /// <param name="id">Id do Estudo de Caso.</param>
    /// <response code="204">Estudo de Caso excluído.</response>
    /// <response code="404">Nenhum Estudo de Caso com esse Id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id)
    {
        await servico.ExcluirAsync(id);

        return NoContent();
    }
}
