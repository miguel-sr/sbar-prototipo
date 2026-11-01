using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Sbar.Api.Dtos;
using Sbar.Api.Identidade;
using Sbar.Api.Requests;
using Sbar.Infrastructure.Identidade;

namespace Sbar.Api.Controllers;

/// <summary>Conta e sessão. A sessão é um cookie HttpOnly emitido pela API; o front não guarda token.</summary>
[ApiController]
[Route("api/account")]
[Produces("application/json")]
public class ContaController(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> sessao,
    ReivindicadorDeCasosLegados legado,
    IOptions<OpcoesDeConta> opcoes) : ControllerBase
{
    /// <summary>Cria uma conta e já inicia a sessão. Exige o código de convite.</summary>
    /// <response code="201">Conta criada; o cookie de sessão vem na resposta.</response>
    /// <response code="403">Código de convite inválido (ou cadastro fechado).</response>
    /// <response code="409">Já existe uma conta com esse e-mail.</response>
    /// <response code="422">E-mail ou senha fora das regras.</response>
    /// <response code="429">Muitas tentativas; aguarde um minuto.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("conta")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UsuarioDto>> Cadastrar(CadastrarRequest request)
    {
        if (!ConviteValido(request.CodigoDeConvite))
            return Problema(StatusCodes.Status403Forbidden, "Código de convite inválido.");

        var email = (request.Email ?? string.Empty).Trim();
        var usuario = new Usuario { Id = Guid.NewGuid(), UserName = email, Email = email };
        var resultado = await usuarios.CreateAsync(usuario, request.Senha ?? string.Empty);

        if (!resultado.Succeeded)
        {
            if (resultado.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                return Problema(StatusCodes.Status409Conflict, "Já existe uma conta com esse e-mail.");

            return Problema(StatusCodes.Status422UnprocessableEntity,
                string.Join(" ", resultado.Errors.Select(TraduzirErro).Distinct()));
        }

        var emailDoLegado = opcoes.Value.EmailDoDonoDoLegado;
        if (!string.IsNullOrWhiteSpace(emailDoLegado) && string.Equals(emailDoLegado.Trim(), email, StringComparison.OrdinalIgnoreCase))
            await legado.ReivindicarAsync(usuario.Id);

        await sessao.SignInAsync(usuario, isPersistent: true);

        return StatusCode(StatusCodes.Status201Created, new UsuarioDto(usuario.Id, usuario.Email!));
    }

    /// <summary>Inicia a sessão com e-mail e senha.</summary>
    /// <response code="200">Sessão iniciada; o cookie vem na resposta.</response>
    /// <response code="401">E-mail ou senha incorretos (ou conta bloqueada por tentativas).</response>
    /// <response code="429">Muitas tentativas; aguarde um minuto.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("conta")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioDto>> Entrar(EntrarRequest request)
    {
        var usuario = await usuarios.FindByEmailAsync((request.Email ?? string.Empty).Trim());
        var resultado = usuario is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await sessao.PasswordSignInAsync(usuario, request.Senha ?? string.Empty, isPersistent: true, lockoutOnFailure: true);

        if (!resultado.Succeeded)
            return Problema(StatusCodes.Status401Unauthorized, "E-mail ou senha incorretos.");

        return Ok(new UsuarioDto(usuario!.Id, usuario.Email!));
    }

    /// <summary>Encerra a sessão.</summary>
    /// <response code="204">Sessão encerrada.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Sair()
    {
        await sessao.SignOutAsync();

        return NoContent();
    }

    /// <summary>Devolve a conta da sessão atual — o front consulta ao abrir para saber se há login.</summary>
    /// <response code="200">Conta da sessão.</response>
    /// <response code="401">Sem sessão.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioDto> Eu() =>
        Ok(new UsuarioDto(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name)!));

    private bool ConviteValido(string? informado)
    {
        var esperado = opcoes.Value.CodigoDeConvite;

        return !string.IsNullOrWhiteSpace(esperado) && informado is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(informado));
    }

    private ObjectResult Problema(int status, string titulo) =>
        StatusCode(status, new ProblemDetails { Status = status, Title = titulo });

    // Mensagens do Identity em português, sem dado técnico.
    private static string TraduzirErro(IdentityError erro) => erro.Code switch
    {
        "InvalidEmail" or "InvalidUserName" => "Informe um e-mail válido.",
        "PasswordTooShort" => "A senha precisa ter pelo menos 8 caracteres.",
        "PasswordRequiresDigit" => "A senha precisa ter pelo menos um número.",
        "PasswordRequiresLower" or "PasswordRequiresUpper" => "A senha precisa ter letras.",
        _ => "Não foi possível criar a conta com esses dados.",
    };
}
