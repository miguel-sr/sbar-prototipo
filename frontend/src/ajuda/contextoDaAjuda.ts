import { slug } from './glossario';

export interface AlvoDaAjuda {
  /** Seção "## Termo" do glossário a abrir. */
  secao: string;
  /** Subseção "### ..." para rolar até ela, se houver. */
  subsecao?: string;
}

// POP-09 seção 8.5: a ajuda abre já no termo mais relevante da tela atual.
const POR_TELA: { padrao: RegExp; alvo: AlvoDaAjuda }[] = [
  { padrao: /\/casos\/[^/]+\/nos$/, alvo: { secao: 'Nó', subsecao: 'Tipos de nó' } },
  { padrao: /\/casos\/[^/]+\/arestas$/, alvo: { secao: 'Aresta', subsecao: 'Peso da ligação' } },
  { padrao: /\/casos\/[^/]+\/fluxos$/, alvo: { secao: 'Fluxo principal' } },
  { padrao: /\/casos\/[^/]+\/mapa$/, alvo: { secao: 'Gargalo (ponto potencialmente crítico)' } },
  { padrao: /\/casos\/[^/]+\/radar$/, alvo: { secao: 'Radar Arquitetural' } },
  { padrao: /\/casos\/[^/]+\/recomendacao$/, alvo: { secao: 'Subsídio à decisão' } },
  { padrao: /\/casos\/[^/]+\/rastreabilidade$/, alvo: { secao: 'Matriz de Rastreabilidade' } },
  { padrao: /\/perfis$/, alvo: { secao: 'Perfil Arquitetural' } },
  { padrao: /\/parametros$/, alvo: { secao: 'Versão dos parâmetros' } },
];

export function alvoDaAjudaPara(caminho: string): AlvoDaAjuda | null {
  return POR_TELA.find((t) => t.padrao.test(caminho))?.alvo ?? null;
}

export function idDoAlvo(alvo: AlvoDaAjuda): string {
  return `ajuda-${slug(alvo.subsecao ?? alvo.secao)}`;
}
