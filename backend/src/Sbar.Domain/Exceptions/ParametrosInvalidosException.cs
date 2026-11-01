using System.Net;
using Sbar.Domain.Enums;
using Sbar.Domain.Services;

namespace Sbar.Domain.Exceptions;

// Parâmetros de análise inválidos (RF11): eixo faltando/repetido, peso negativo ou soma dos pesos ≠ 1 (RN07, CT10 —
// "cálculo bloqueado, com mensagem de erro"). 422, mesma família das demais exceções de validação.
public class ParametrosInvalidosException : DomainException
{
    private ParametrosInvalidosException(string mensagem)
        : base(mensagem, HttpStatusCode.UnprocessableEntity)
    {
    }

    public static ParametrosInvalidosException EixoAusente(Eixo eixo) =>
        new($"O eixo {eixo} não foi informado: os dez eixos são obrigatórios.");

    public static ParametrosInvalidosException EixoRepetido(Eixo eixo) =>
        new($"O eixo {eixo} foi informado mais de uma vez.");

    public static ParametrosInvalidosException PesoNegativo(Eixo eixo, double peso) =>
        new($"O peso do eixo {eixo} ({Formatar(peso)}) deve ser um número maior ou igual a zero.");

    public static ParametrosInvalidosException ValorNaoNumerico(Eixo eixo) =>
        new($"Os limites do eixo {eixo} devem ser números finitos.");

    public static ParametrosInvalidosException SomaDosPesos(double soma) =>
        new($"Soma dos pesos igual a {Formatar(soma)}; deve ser 1.");

    private static string Formatar(double valor) =>
        FormatadorDeNumeros.FormatarSemZerosAFrente(valor, 6);
}
