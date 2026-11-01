using Sbar.Domain.ValueObjects;

namespace Sbar.Domain.Interfaces.Services;

// RF11 — parâmetros de análise (limites, pesos, assinaturas) versionados. Cada análise informa a versão usada (US06)
// e uma versão antiga pode ser reaplicada (protocolo de avaliação, 5.4).
public interface IServicoDeParametros
{
    // Conjunto vigente (cria a versão 1 sob demanda se ainda não existir nenhuma).
    Task<ConjuntoDeParametros> ObterAtualAsync();

    // Snapshot imutável da versão pedida; RecursoNaoEncontradoException se não existir.
    Task<ConjuntoDeParametros> ObterVersaoAsync(int numero);

    // Versão pedida ou, se nula, a vigente.
    Task<ConjuntoDeParametros> ObterAsync(int? numero);

    // Todas as versões, da mais recente para a mais antiga.
    Task<IReadOnlyList<VersaoDeParametrosInfo>> ListarVersoesAsync();

    // Substitui limites e pesos dos dez eixos (valida Σ p_i = 1) e grava uma nova versão na mesma transação.
    Task<ConjuntoDeParametros> AtualizarLimitesEPesosAsync(string? descricao, IReadOnlyList<ParametroDeEixo> eixos);

    // Garante que existe ao menos a versão 1 (estado vigente antes de qualquer alteração). Idempotente.
    Task GarantirVersaoInicialAsync();

    // Grava uma nova versão com o estado vigente atual (chamado dentro da transação de quem alterou o estado).
    Task RegistrarVersaoAsync(string descricao);
}
