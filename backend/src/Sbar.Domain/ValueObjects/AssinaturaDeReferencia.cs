using Sbar.Domain.Enums;

namespace Sbar.Domain.ValueObjects;

// Assinatura arquitetural de referência (Quadro 8, seção 4.6): nome + valor 0–5 de cada um dos dez eixos.
// É a forma "congelada" de um PerfilArquitetural dentro de um conjunto de parâmetros versionado.
public sealed record AssinaturaDeReferencia(Guid Id, string Nome, IReadOnlyDictionary<Eixo, double> Valores);
