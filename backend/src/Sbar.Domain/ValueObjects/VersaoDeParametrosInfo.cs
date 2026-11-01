namespace Sbar.Domain.ValueObjects;

// Identificação da versão dos parâmetros usada numa análise (RF11, US06; TCC 4.6.1: registrar limites,
// assinaturas e pesos utilizados).
public sealed record VersaoDeParametrosInfo(int Numero, DateTime CriadaEm, string Descricao);
