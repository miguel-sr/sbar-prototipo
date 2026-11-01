import type { Eixo } from './Metricas';

// Contrato de docs/projeto/API.md seção 8. Valores na escala normalizada 0–5.
export interface DadosDoPerfilArquitetural {
  nome: string;
  valorCO: number;
  valorESC: number;
  valorINT: number;
  valorUX: number;
  valorDISP: number;
  valorFLEX: number;
  valorTR: number;
  valorAUT: number;
  valorVOL: number;
  valorDEP: number;
}

export interface PerfilArquitetural extends DadosDoPerfilArquitetural {
  id: string;
}

export type CampoDeValor = `valor${Eixo}`;

export function campoDoEixo(eixo: Eixo): CampoDeValor {
  return `valor${eixo}`;
}
