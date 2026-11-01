// Fluxos principais cadastrados pelo usuário (TCC 4.2.7, RF05): sequência ORDENADA de Nós, com
// repetição permitida; cada par consecutivo precisa de uma Aresta origem → destino (CT09).

/** Primeira etapa (par consecutivo) sem Aresta correspondente, ou o Nó que deixou de existir. */
export interface EtapaAusente {
  /** Posição do par, a partir de 1 (etapa 1 = passo 1 → passo 2). */
  posicao: number;
  origemId: string;
  destinoId: string;
  motivo: string;
}

export interface Fluxo {
  id: string;
  nome: string;
  nosIds: string[];
  /** false quando falta uma Aresta (ou um Nó foi excluído) depois do cadastro. Fluxo inválido fica de fora do DEP. */
  valido: boolean;
  etapaAusente: EtapaAusente | null;
  /** Bandas distintas atravessadas pelo caminho, contando origem e destino. */
  bandasPercorridas: number;
}

export interface CriarFluxoRequest {
  nome: string;
  nosIds: string[];
}

/** PUT /api/casos/{casoId}/fluxos/{fluxoId}: substitui nome e sequência de Nós. */
export type AtualizarFluxoRequest = CriarFluxoRequest;
