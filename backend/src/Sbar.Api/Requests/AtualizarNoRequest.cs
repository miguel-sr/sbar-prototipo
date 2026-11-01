using Sbar.Domain.Enums;

namespace Sbar.Api.Requests;

/// <summary>Novos dados de um Nó do Mapa. A Banda é re-derivada do Tipo — nunca escolhida aqui.</summary>
/// <param name="Nome">Nome do elemento do Blueprint — obrigatório, até 100 caracteres (422 se vazio ou maior).</param>
/// <param name="Tipo">Tipo de Nó — decide a Banda automaticamente.</param>
public record AtualizarNoRequest(string Nome, TipoDeNo Tipo);
