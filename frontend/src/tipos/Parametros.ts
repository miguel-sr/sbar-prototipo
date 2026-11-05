import type { Eixo } from './Metricas';

// Contrato da Onda 2 (9.5): parâmetros do cálculo versionados — limites (Quadro 6), pesos p_i (4.5.1)
// e assinaturas de referência (Quadro 7). Cada alteração cria uma nova versão imutável (RF11, US06).
export interface VersaoDeParametros {
  numero: number;
  criadaEm: string;
  descricao: string;
}

export interface ParametroDoEixo {
  eixo: Eixo;
  minimo: number;
  maximo: number;
  peso: number;
}

export interface AssinaturaDoConjunto {
  id: string;
  nome: string;
  valores: Record<Eixo, number>;
}

export interface ConjuntoDeParametros {
  versao: VersaoDeParametros;
  eixos: ParametroDoEixo[];
  assinaturas: AssinaturaDoConjunto[];
}

export interface AtualizarLimitesEPesosRequest {
  descricao: string;
  eixos: ParametroDoEixo[];
}

/** Tolerância da soma dos pesos (RN07), a mesma do backend. */
export const TOLERANCIA_DA_SOMA_DOS_PESOS = 1e-9;
