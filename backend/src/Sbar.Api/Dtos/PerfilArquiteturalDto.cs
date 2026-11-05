using Sbar.Domain.Entities;

namespace Sbar.Api.Dtos;

/// <summary>Um Perfil Arquitetural de referência, com os dez valores já na escala normalizada (0–5).</summary>
/// <param name="Id">Id do Perfil.</param>
/// <param name="Nome">Nome do estilo arquitetural (ex.: "Monólito Modular (reconstruído)").</param>
/// <param name="ValorCO">Complexidade Operacional (0–5).</param>
/// <param name="ValorESC">Escalabilidade (0–5).</param>
/// <param name="ValorINT">Integração (0–5).</param>
/// <param name="ValorUX">Experiência do Usuário (0–5).</param>
/// <param name="ValorDISP">Disponibilidade (0–5).</param>
/// <param name="ValorFLEX">Flexibilidade/Evolução (0–5).</param>
/// <param name="ValorTR">Tempo Real (0–5).</param>
/// <param name="ValorAUT">Autonomia dos Módulos (0–5).</param>
/// <param name="ValorVOL">Volume de Comunicação (0–5).</param>
/// <param name="ValorDEP">Dependência Organizacional (0–5).</param>
public record PerfilArquiteturalDto(
    Guid Id, string Nome,
    double ValorCO, double ValorESC, double ValorINT, double ValorUX, double ValorDISP,
    double ValorFLEX, double ValorTR, double ValorAUT, double ValorVOL, double ValorDEP)
{
    public static PerfilArquiteturalDto DeEntidade(PerfilArquitetural perfil) => new(
        perfil.Id, perfil.Nome,
        perfil.ValorCO, perfil.ValorESC, perfil.ValorINT, perfil.ValorUX, perfil.ValorDISP,
        perfil.ValorFLEX, perfil.ValorTR, perfil.ValorAUT, perfil.ValorVOL, perfil.ValorDEP);
}
