using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Interfaces.Services;

namespace Sbar.Api.Controllers;

/// <summary>
/// Perfis Arquiteturais de referência (ex.: "Monólito Modular", "Microsserviços") — tabela global,
/// não vinculada a nenhum Estudo de Caso, usada por <c>GET /api/casos/{id}/metricas</c> para calcular
/// a distância e a recomendação preliminar. Vem semeada com dois perfis reconstruídos (ver
/// D1 em docs/dominio/regras-de-negocio.md); pode ser editada livremente por aqui.
/// </summary>
[ApiController]
[Route("api/perfis-arquiteturais")]
[Produces("application/json")]
public class PerfisController(IServicoDePerfis servico) : ControllerBase
{
    /// <summary>Cria um novo Perfil Arquitetural de referência.</summary>
    /// <response code="201">Perfil criado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PerfilArquiteturalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PerfilArquiteturalDto>> Criar(CriarPerfilArquiteturalRequest request)
    {
        var perfil = await servico.CriarAsync(request.ParaDados());

        var dto = PerfilArquiteturalDto.DeEntidade(perfil);
        return CreatedAtAction(nameof(ObterPorId), new { id = dto.Id }, dto);
    }

    /// <summary>Lista todos os Perfis Arquiteturais de referência cadastrados.</summary>
    /// <response code="200">Lista de perfis (pode ser vazia).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PerfilArquiteturalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PerfilArquiteturalDto>>> Listar()
    {
        var perfis = await servico.ListarAsync();
        return Ok(perfis.Select(PerfilArquiteturalDto.DeEntidade).ToList());
    }

    /// <summary>Obtém um Perfil Arquitetural pelo Id.</summary>
    /// <response code="200">Perfil encontrado.</response>
    /// <response code="404">Nenhum perfil com esse Id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PerfilArquiteturalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilArquiteturalDto>> ObterPorId(Guid id)
    {
        var perfil = await servico.ObterAsync(id);
        return Ok(PerfilArquiteturalDto.DeEntidade(perfil));
    }

    /// <summary>Substitui todos os dez valores (e o nome) de um Perfil Arquitetural existente.</summary>
    /// <response code="200">Perfil atualizado.</response>
    /// <response code="404">Nenhum perfil com esse Id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PerfilArquiteturalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerfilArquiteturalDto>> Atualizar(Guid id, CriarPerfilArquiteturalRequest request)
    {
        var perfil = await servico.AtualizarAsync(id, request.ParaDados());

        return Ok(PerfilArquiteturalDto.DeEntidade(perfil));
    }

    /// <summary>Remove um Perfil Arquitetural — deixa de entrar no cálculo de recomendação dos Estudos de Caso.</summary>
    /// <response code="204">Perfil removido.</response>
    /// <response code="404">Nenhum perfil com esse Id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid id)
    {
        await servico.RemoverAsync(id);
        return NoContent();
    }
}
