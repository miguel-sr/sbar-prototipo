import type { Banda } from './No';
import type { VersaoDeParametros } from './Parametros';

// Contrato de docs/projeto/API.md seção 6 (+ comunidades, Q, avisos, bandas e fluxos do TCC revisado).
export const EIXOS = ['CO', 'ESC', 'INT', 'UX', 'DISP', 'FLEX', 'TR', 'AUT', 'VOL', 'DEP'] as const;
export type Eixo = (typeof EIXOS)[number];

export interface EixoResultado {
  eixo: Eixo;
  aplicavel: boolean;
  valorBruto: number | null;
  valorNormalizado: number | null;
  motivo: string | null;
}

/** Contribuição de um eixo para a distância: p_i · (Norm_i do serviço − Norm_i do perfil)² (Quadros 26/36). */
export interface ContribuicaoDoEixo {
  eixo: Eixo;
  contribuicao: number;
}

export interface DistanciaPerfil {
  perfilId: string;
  perfilNome: string;
  distancia: number;
  /** distância / distância máxima (5) · 100 (TCC 4.6). */
  percentualDoMaximo: number;
  /** Só eixos aplicáveis; a soma das contribuições é a distância ao quadrado. */
  contribuicoes: ContribuicaoDoEixo[];
}

/**
 * Perfil de referência mais próximo (TCC 4.6, RN09): subsídio à decisão do arquiteto, não decisão.
 * Os nomes `recomendacao`/`estiloRecomendado` são mantidos na API (D14, opção 1: só o texto de tela muda).
 */
export interface Recomendacao {
  estiloRecomendado: string;
  distancias: DistanciaPerfil[];
  /** Segunda menor distância − menor; null com menos de 2 perfis. */
  margem: number | null;
  /** Distância máxima possível (5). */
  distanciaMaxima: number;
}

/** Uma comunidade estrutural (TCC 4.4.1): índice 1…n, Nós e arestas que saem para outras comunidades. */
export interface Comunidade {
  indice: number;
  nosIds: string[];
  saidasExternas: number;
}

/** μ, σ (populacional) e limiar μ + σ das cargas de uma Banda ocupada (TCC 4.2.6; US04). */
export interface EstatisticasDaBanda {
  banda: Banda;
  quantidadeDeNos: number;
  media: number;
  desvioPadrao: number;
  limiar: number;
}

/** Resumo de um fluxo principal usado (ou descartado) no cálculo do DEP. */
export interface FluxoDasMetricas {
  id: string;
  nome: string;
  valido: boolean;
  bandasPercorridas: number;
}

export interface ResultadoDeMetricas {
  /** Sempre 10 itens, um por Eixo. */
  eixos: EixoResultado[];
  /** null quando não há nenhum Perfil Arquitetural cadastrado. */
  recomendacao: Recomendacao | null;
  /** Ids de todos os Nós gargalo (qualquer Banda): carga > μ + σ da própria Banda. */
  nosGargalo: string[];
  /** Comunidades estruturais usadas por FLEX e AUT (TCC 4.4.1). */
  comunidades: Comunidade[];
  /** Modularidade Q da partição; null quando o Mapa não tem arestas (Q indefinido). */
  modularidadeQ: number | null;
  /** Avisos de casos-limite (sem arestas, comunidade única, Q < 0,3…), prontos para exibir. */
  avisos: string[];
  /** μ, σ e limiar de cada Banda ocupada. */
  bandas: EstatisticasDaBanda[];
  /** Fluxos principais cadastrados; só os válidos entram no DEP. */
  fluxos: FluxoDasMetricas[];
  /** Versão dos parâmetros (limites, pesos, assinaturas) usada neste cálculo (RF11, US06). */
  versaoDosParametros: VersaoDeParametros;
}
