using Sbar.Domain.Exceptions;

namespace Sbar.Domain.Validations;

// Único ponto que normaliza (trim) e valida os textos digitados pelo usuário — nomes de Estudo de
// Caso/Nó e a descrição do domínio — para o front e a API nunca divergirem nos limites.
public static class TextoValidado
{
    public const int TamanhoMaximoDoNomeDoEstudoDeCaso = 120;
    public const int TamanhoMaximoDoNomeDoNo = 100;
    public const int TamanhoMaximoDaDescricaoDoDominio = 1000;
    public const int TamanhoMaximoDoNomeDoFluxo = 100;
    public const int TamanhoMaximoDaJustificativaDaAresta = 500;
    public const int QuantidadeMinimaDeNosDoFluxo = 2;
    public const int QuantidadeMaximaDeNosDoFluxo = 100;

    public static string Nome(string? valor, string campo, int maximo)
    {
        var texto = valor?.Trim();

        if (string.IsNullOrEmpty(texto))
            throw NomeInvalidoException.Vazio(campo);

        if (texto.Length > maximo)
            throw NomeInvalidoException.AcimaDoMaximo(campo, maximo);

        return texto;
    }

    // Descrição é opcional: vazia/só espaços vira null.
    public static string? DescricaoOpcional(string? valor, string campo, int maximo)
    {
        var texto = valor?.Trim();

        if (string.IsNullOrEmpty(texto))
            return null;

        if (texto.Length > maximo)
            throw NomeInvalidoException.AcimaDoMaximo(campo, maximo);

        return texto;
    }
}
