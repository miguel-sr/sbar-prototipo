using Sbar.Domain.Enums;

namespace Sbar.Domain.Services;

// RF-02 — Banda é sempre derivada do Tipo de Nó, nunca escolhida manualmente (TCC seção 4.2.2)
public static class RegraDeBanda
{
    public static Banda Derivar(TipoDeNo tipo) => tipo switch
    {
        TipoDeNo.Ator => Banda.Ator,
        TipoDeNo.Touchpoint => Banda.Frontstage,
        TipoDeNo.Processo => Banda.Backstage,
        TipoDeNo.Suporte => Banda.Suporte,
        TipoDeNo.Dados => Banda.Suporte,
        TipoDeNo.Externo => Banda.Externo,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null)
    };
}
