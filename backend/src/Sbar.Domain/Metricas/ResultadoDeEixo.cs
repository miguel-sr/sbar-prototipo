using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// Resultado de um eixo do Radar — "não aplicável" (Aplicavel=false + Motivo) é explícito
// no contrato, nunca um null silencioso (RNF-01, decisão 2026-09-17).
public sealed record ResultadoDeEixo(Eixo Eixo, bool Aplicavel, double? ValorBruto, double? ValorNormalizado, string? Motivo);
