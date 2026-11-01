namespace Sbar.Application.Calculadoras;

// Não é só um double: várias métricas têm caso "não aplicável" na própria especificação
// (docs/dominio/formulas-metricas.md) — evita usar NaN/-1 como sinalizador escondido.
public sealed record ResultadoDeMetricaBruta(bool Aplicavel, double? Valor, string? Motivo);
