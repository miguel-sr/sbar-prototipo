using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.Parametros;

// Parâmetros de partida de toda conta nova (PLANO_CONTAS_DE_USUARIO.md 4.3): uma fonte só, em código, no lugar do
// seed da migration. Cada usuário recebe uma CÓPIA e passa a versioná-la por conta própria (RF11).
public static class PadroesDoSistema
{
    // Limites de normalização do Quadro 6 do TCC revisado (docs/dominio/formulas-metricas.md); peso inicial 0,1 em
    // todos os eixos (Σp_i = 1, RN07). Valores provisórios, ainda não calibrados por especialistas.
    public static IReadOnlyList<ParametroDeEixo> Eixos { get; } =
    [
        new(Eixo.CO, 6.0, 40.0, 0.1),
        new(Eixo.ESC, 0.0, 1.0, 0.1),
        new(Eixo.INT, 0.0, 1.0, 0.1),
        new(Eixo.UX, 0.0, 3.0, 0.1),
        new(Eixo.DISP, 0.0, 1.0, 0.1),
        new(Eixo.FLEX, 0.0, 1.0, 0.1),
        new(Eixo.TR, 0.0, 1.0, 0.1),
        new(Eixo.AUT, 0.0, 1.0, 0.1),
        new(Eixo.VOL, 1.0, 3.0, 0.1),
        new(Eixo.DEP, 0.2, 1.0, 0.1),
    ];

    // Assinaturas provisórias do Quadro 7 (seção 4.6): valores atribuídos pelos pesquisadores, ainda a validar com
    // especialistas (protocolo 5.4) — por isso o nome mantém "(provisório)".
    public static IReadOnlyList<DadosDoPerfilArquitetural> Perfis { get; } =
    [
        new("Monólito Modular (provisório)", 2.0, 1.5, 1.0, 2.5, 3.0, 1.5, 1.5, 1.0, 3.0, 3.0),
        new("Microsserviços (provisório)", 4.0, 4.0, 3.5, 2.5, 4.0, 4.0, 2.5, 4.5, 2.0, 4.0),
    ];
}
