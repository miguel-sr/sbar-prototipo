using Sbar.Domain.Enums;

namespace Sbar.Api.Requests;

/// <summary>Dados para criar um Nó do Mapa. A Banda é sempre derivada do Tipo — nunca escolhida aqui.</summary>
/// <param name="Nome">Nome do elemento do Blueprint (ex.: "Carrinho/Checkout").</param>
/// <param name="Tipo">Tipo de Nó — decide a Banda automaticamente: Ator→Ator, Touchpoint→Frontstage, Processo→Backstage, Suporte/Dados→Suporte, Externo→Externo.</param>
public record CriarNoRequest(string Nome, TipoDeNo Tipo);
