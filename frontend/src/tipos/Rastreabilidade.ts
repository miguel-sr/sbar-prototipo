import type { Eixo } from './Metricas';

// Contrato de docs/projeto/API.md seção 7 (+ campos da Onda 2: categoria e ids dos elementos).
export const CATEGORIAS_DA_MATRIZ = ['gargalo', 'aresta-critica', 'aresta-externa', 'comunidade', 'fluxo', 'no-frontstage'] as const;
export type CategoriaDaMatriz = (typeof CATEGORIAS_DA_MATRIZ)[number];

export interface LinhaDeRastreabilidade {
  elemento: string;
  /** Identificação do Nó/Aresta/comunidade/fluxo; pode terminar em "(gargalo)", "(crítica)" ou "(externa)". */
  noOuAresta: string;
  metrica: string;
  /** Pode ser vazio (ex.: gargalo de Ator/Externo, que não entra em nenhum eixo). */
  eixos: Eixo[];
  contribuicao: string;
  categoria: CategoriaDaMatriz;
  nosIds: string[];
  arestasIds: string[];
}
