namespace Sbar.Api.Identidade;

// Seção "Sbar" da configuração (variáveis Sbar__CodigoDeConvite etc. no deploy).
public class OpcoesDeConta
{
    public const string Secao = "Sbar";

    // Sem código configurado o cadastro fica fechado: o endereço é público e não há e-mail para verificar (A2).
    public string? CodigoDeConvite { get; set; }

    // E-mail que recebe os Estudos de Caso publicados antes das contas, ao se cadastrar (A4).
    public string? EmailDoDonoDoLegado { get; set; }

    public int TentativasPorMinuto { get; set; } = 10;
}
