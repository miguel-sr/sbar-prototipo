namespace Sbar.Api.Requests;

/// <summary>Dados para criar uma conta.</summary>
/// <param name="Email">E-mail da conta (não é verificado nesta versão).</param>
/// <param name="Senha">Senha com pelo menos 8 caracteres, com letra e número.</param>
/// <param name="CodigoDeConvite">Código de convite informado por quem administra a instância.</param>
public record CadastrarRequest(string Email, string Senha, string CodigoDeConvite);
