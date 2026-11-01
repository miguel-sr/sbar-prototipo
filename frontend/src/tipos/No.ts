// Contrato de docs/projeto/API.md seção 4. Enums trafegam como string.
export const TIPOS_DE_NO = ['Ator', 'Touchpoint', 'Processo', 'Suporte', 'Dados', 'Externo'] as const;
export type TipoDeNo = (typeof TIPOS_DE_NO)[number];

// Ordem das Bandas no Service Blueprint (de cima para baixo), TCC seção 4.2.2.
export const BANDAS = ['Ator', 'Frontstage', 'Backstage', 'Suporte', 'Externo'] as const;
export type Banda = (typeof BANDAS)[number];

export interface No {
  id: string;
  nome: string;
  tipo: TipoDeNo;
  /** Sempre derivada do Tipo pelo backend (RegraDeBanda) — nunca enviada pelo front. */
  banda: Banda;
}

export interface CriarNoRequest {
  nome: string;
  tipo: TipoDeNo;
}

/** PUT /api/casos/{casoId}/nos/{noId}: a Banda é rederivada do Tipo pelo backend. */
export type AtualizarNoRequest = CriarNoRequest;
