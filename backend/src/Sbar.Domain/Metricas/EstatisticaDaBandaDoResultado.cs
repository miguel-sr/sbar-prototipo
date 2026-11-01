using Sbar.Domain.Enums;

namespace Sbar.Domain.Metricas;

// μ_b, σ_b (populacional) e o limiar μ_b + σ_b de uma Banda ocupada — TCC revisado 4.2.6 (US04: "os valores
// de μ e σ de cada banda são exibidos"). Nó é potencialmente crítico quando carga(v) > Limiar.
public sealed record EstatisticaDaBandaDoResultado(Banda Banda, int QuantidadeDeNos, double Media, double DesvioPadrao, double Limiar);
