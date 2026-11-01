import type { TipoDeAresta } from '../tipos/Aresta';
import type { Banda, TipoDeNo } from '../tipos/No';

// Rótulos de exibição dos enums da API. Só texto de tela: nenhuma regra do método mora aqui
// (a Banda, por exemplo, continua sendo decidida só pelo backend — RegraDeBanda).

export const ROTULO_TIPO_DE_NO: Record<TipoDeNo, string> = {
  Ator: 'Ator',
  Touchpoint: 'Touchpoint',
  Processo: 'Processo',
  Suporte: 'Suporte',
  Dados: 'Dados',
  Externo: 'Externo',
};

/** Frase curta, em linguagem de negócio, exibida no seletor de tipo (glossario.md, "Tipos de nó"). */
export const DICA_TIPO_DE_NO: Record<TipoDeNo, string> = {
  Ator: 'Quem inicia o fluxo, normalmente o cliente.',
  Touchpoint: 'Algo que o cliente vê e usa: uma tela, um botão, uma notificação.',
  Processo: 'Acontece por trás dos panos, só neste fluxo.',
  Suporte: 'Processo usado por várias partes do sistema (login, notificações…).',
  Dados: 'Onde as informações ficam guardadas.',
  Externo: 'Serviço de fora do sistema (pagamento, IA, SMS/e-mail…).',
};

export const ROTULO_TIPO_DE_ARESTA: Record<TipoDeAresta, string> = {
  Sincrona: 'Síncrona',
  Critica: 'Crítica',
  Assincrona: 'Assíncrona',
  Batch: 'Batch',
};

/** Códigos usados no TCC (seção 4.2.4). */
export const CODIGO_TIPO_DE_ARESTA: Record<TipoDeAresta, string> = {
  Sincrona: 'SYN',
  Critica: 'CRI',
  Assincrona: 'ASY',
  Batch: 'BAT',
};

export const ROTULO_PESO: Record<number, string> = {
  3: 'Alto',
  2: 'Médio',
  1: 'Baixo',
};

export const COR_DA_BANDA: Record<Banda, string> = {
  Ator: 'var(--banda-ator)',
  Frontstage: 'var(--banda-frontstage)',
  Backstage: 'var(--banda-backstage)',
  Suporte: 'var(--banda-suporte)',
  Externo: 'var(--banda-externo)',
};

export function formatarNumero(valor: number, casas = 2): string {
  return valor.toLocaleString('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });
}

export function formatarData(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
}
