using Sbar.Domain.ValueObjects;

namespace Sbar.Api.Requests;

/// <summary>
/// Dados de um Perfil Arquitetural de referência (ex.: "Monólito Modular", "Microsserviços") — os dez
/// valores já devem estar na escala normalizada do Radar (0 a 5), na mesma escala que
/// <c>GET /api/casos/{id}/metricas</c> devolve para o eixo correspondente.
/// </summary>
/// <param name="Nome">Nome do estilo arquitetural de referência.</param>
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
public record CriarPerfilArquiteturalRequest(
    string Nome,
    double ValorCO, double ValorESC, double ValorINT, double ValorUX, double ValorDISP,
    double ValorFLEX, double ValorTR, double ValorAUT, double ValorVOL, double ValorDEP)
{
    public DadosDoPerfilArquitetural ParaDados() => new(
        Nome, ValorCO, ValorESC, ValorINT, ValorUX, ValorDISP, ValorFLEX, ValorTR, ValorAUT, ValorVOL, ValorDEP);
}
