namespace Sbar.Domain.ValueObjects;

// Agrupa nome + os dez valores normalizados de um Perfil Arquitetural — evita que
// PerfilArquitetural.Criar/Atualizar e IServicoDePerfis.CriarAsync/AtualizarAsync
// precisem de onze parâmetros posicionais soltos.
public sealed record DadosDoPerfilArquitetural(
    string Nome,
    double ValorCO, double ValorESC, double ValorINT, double ValorUX, double ValorDISP,
    double ValorFLEX, double ValorTR, double ValorAUT, double ValorVOL, double ValorDEP);
