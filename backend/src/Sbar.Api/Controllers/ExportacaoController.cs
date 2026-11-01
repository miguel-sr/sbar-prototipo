using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Sbar.Api.Dtos;
using Sbar.Api.Exportacao;
using Sbar.Domain.Exceptions;

namespace Sbar.Api.Controllers;

/// <summary>Exportação dos resultados de uma análise (RF13, US07): JSON completo ou CSV por seções.</summary>
[ApiController]
[Route("api/casos/{casoId:guid}/exportacao")]
public class ExportacaoController(MontadorDeExportacao montador) : ControllerBase
{
    /// <summary>
    /// Exporta a análise do Estudo de Caso: versão dos parâmetros, comunidades e Q, fluxos, métricas brutas e normalizadas,
    /// distâncias com contribuições e a Matriz de Rastreabilidade — o que o TCC 4.6.1 manda registrar. Recalcula a partir do
    /// estado atual do Mapa (nada é guardado). Devolve um anexo (<c>Content-Disposition: attachment</c>).
    /// </summary>
    /// <param name="casoId">Id do Estudo de Caso.</param>
    /// <param name="formato"><c>json</c> (padrão) ou <c>csv</c> (UTF-8 com BOM, separador <c>;</c>, ponto decimal, seções iniciadas por <c># Nome</c>).</param>
    /// <param name="versao">Versão dos parâmetros a usar; sem ela, a vigente.</param>
    /// <response code="200">Arquivo da análise (<c>application/json</c> ou <c>text/csv</c>).</response>
    /// <response code="400">Formato diferente de json/csv.</response>
    /// <response code="404">Estudo de Caso ou versão dos parâmetros não existem.</response>
    /// <response code="422">A soma dos pesos da versão usada não é 1.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ExportacaoDto), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Exportar(Guid casoId, [FromQuery] string formato = "json", [FromQuery] int? versao = null)
    {
        var formatoNormalizado = formato.Trim().ToLowerInvariant();
        if (formatoNormalizado is not ("json" or "csv"))
            throw new FormatoDeExportacaoInvalidoException(formato);

        var exportacao = await montador.MontarAsync(casoId, versao);
        var nomeDoArquivo = $"sbar-{Slug(exportacao.Caso.Nome)}-v{exportacao.VersaoDosParametros.Numero}.{formatoNormalizado}";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{nomeDoArquivo}\"";

        if (formatoNormalizado == "csv")
            return File(ExportadorDeCsv.Gerar(exportacao), "text/csv; charset=utf-8", nomeDoArquivo);

        return Ok(exportacao);
    }

    // Nome do caso seguro para nome de arquivo: minúsculas, sem acento, só letras/números/hífen.
    private static string Slug(string nome)
    {
        var semAcento = new StringBuilder();
        foreach (var c in nome.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(char.ToLowerInvariant(c));

        var slug = new StringBuilder();
        foreach (var c in semAcento.ToString())
            slug.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');

        var resultado = string.Join('-', slug.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return resultado.Length == 0 ? "caso" : resultado[..Math.Min(resultado.Length, 60)];
    }
}
