// Contrato de docs/projeto/API.md seção 5 (e a justificativa obrigatória do TCC revisado, RN03).
export const TIPOS_DE_ARESTA = ['Sincrona', 'Critica', 'Assincrona', 'Batch'] as const;
export type TipoDeAresta = (typeof TIPOS_DE_ARESTA)[number];

export const PESOS = [3, 2, 1] as const;
export type Peso = (typeof PESOS)[number];

export interface Aresta {
  id: string;
  noOrigemId: string;
  noDestinoId: string;
  tipo: TipoDeAresta;
  peso: Peso;
  /** Por que a interação existe, rastreável ao Blueprint — obrigatória (RN03, TCC 4.2.3). */
  justificativa: string;
}

export interface CriarArestaRequest {
  noOrigemId: string;
  noDestinoId: string;
  tipo: TipoDeAresta;
  peso: Peso;
  justificativa: string;
}

/** PUT /api/casos/{casoId}/arestas/{arestaId}: substitui origem, destino, tipo, peso e justificativa. */
export type AtualizarArestaRequest = CriarArestaRequest;
