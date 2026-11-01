import type { Aresta } from '../tipos/Aresta';
import { BANDAS, type Banda, type No } from '../tipos/No';

// Posicionamento dos Nós no desenho do Mapa (T5). Só geometria de tela — nenhuma métrica do
// método é calculada aqui. Raias horizontais por Banda (como no Service Blueprint, Figura 5
// do TCC) e colunas pela ordem do fluxo: distância, em passos, a partir dos Atores.

export const GEOMETRIA = {
  larguraRotulo: 112,
  larguraColuna: 150,
  alturaLinha: 60, // uma "linha" dentro da raia; Nós na mesma coluna e Banda empilham
  respiroRaia: 16,
  larguraNo: 128,
  alturaNo: 44,
  margem: 20,
};

export interface NoPosicionado {
  no: No;
  coluna: number;
  linha: number;
  x: number; // centro
  y: number; // centro
}

export interface LayoutDoMapa {
  nos: Map<string, NoPosicionado>;
  raias: { banda: Banda; y: number; altura: number }[];
  largura: number;
  altura: number;
}

/** Distância (em arestas, seguindo a direção) a partir de qualquer Nó de Banda Ator. */
export function calcularColunas(nos: No[], arestas: Aresta[]): Map<string, number> {
  const saidas = new Map<string, string[]>();
  const vizinhos = new Map<string, string[]>();
  for (const n of nos) {
    saidas.set(n.id, []);
    vizinhos.set(n.id, []);
  }
  for (const a of arestas) {
    saidas.get(a.noOrigemId)?.push(a.noDestinoId);
    vizinhos.get(a.noOrigemId)?.push(a.noDestinoId);
    vizinhos.get(a.noDestinoId)?.push(a.noOrigemId);
  }

  const coluna = new Map<string, number>();
  const fila: string[] = [];
  for (const n of nos) {
    if (n.banda === 'Ator') {
      coluna.set(n.id, 0);
      fila.push(n.id);
    }
  }
  // BFS dirigida a partir dos Atores.
  while (fila.length) {
    const atual = fila.shift()!;
    for (const prox of saidas.get(atual) ?? []) {
      if (!coluna.has(prox)) {
        coluna.set(prox, coluna.get(atual)! + 1);
        fila.push(prox);
      }
    }
  }
  // Nós não alcançados a partir de um Ator: encaixa ao lado de algum vizinho já posicionado.
  let mudou = true;
  while (mudou) {
    mudou = false;
    for (const n of nos) {
      if (coluna.has(n.id)) continue;
      const posicionados = (vizinhos.get(n.id) ?? []).filter((v) => coluna.has(v)).map((v) => coluna.get(v)!);
      if (posicionados.length) {
        coluna.set(n.id, Math.min(...posicionados) + 1);
        mudou = true;
      }
    }
  }
  for (const n of nos) if (!coluna.has(n.id)) coluna.set(n.id, 0);
  return coluna;
}

export function calcularLayout(nos: No[], arestas: Aresta[]): LayoutDoMapa {
  const g = GEOMETRIA;
  const colunas = calcularColunas(nos, arestas);

  // Ordem estável: pela coluna e, no empate, pela ordem de cadastro.
  const ordenados = nos
    .map((no, i) => ({ no, i }))
    .sort((a, b) => colunas.get(a.no.id)! - colunas.get(b.no.id)! || a.i - b.i);

  // Nós da mesma Banda na mesma coluna empilham em linhas dentro da raia (não empurram a
  // coluna para a direita — isso deixaria o Mapa largo demais).
  const ocupacao = new Map<string, number>(); // `${banda}:${coluna}` -> Nós já colocados
  const linhasPorBanda = new Map<Banda, number>();
  const posicoes: { no: No; coluna: number; linha: number }[] = [];
  let maiorColuna = 0;
  for (const { no } of ordenados) {
    const coluna = colunas.get(no.id)!;
    const chave = `${no.banda}:${coluna}`;
    const linha = ocupacao.get(chave) ?? 0;
    ocupacao.set(chave, linha + 1);
    linhasPorBanda.set(no.banda, Math.max(linhasPorBanda.get(no.banda) ?? 0, linha + 1));
    maiorColuna = Math.max(maiorColuna, coluna);
    posicoes.push({ no, coluna, linha });
  }

  const raias: LayoutDoMapa['raias'] = [];
  let y = g.margem;
  for (const banda of BANDAS) {
    const linhas = linhasPorBanda.get(banda);
    if (!linhas) continue;
    const altura = linhas * g.alturaLinha + g.respiroRaia * 2;
    raias.push({ banda, y, altura });
    y += altura;
  }
  const topoDaRaia = new Map(raias.map((r) => [r.banda, r.y]));

  const posicionados = new Map<string, NoPosicionado>();
  for (const { no, coluna, linha } of posicoes) {
    posicionados.set(no.id, {
      no,
      coluna,
      linha,
      x: g.margem + g.larguraRotulo + coluna * g.larguraColuna + g.larguraColuna / 2,
      y: topoDaRaia.get(no.banda)! + g.respiroRaia + linha * g.alturaLinha + g.alturaLinha / 2,
    });
  }

  return {
    nos: posicionados,
    raias,
    largura: g.margem * 2 + g.larguraRotulo + (maiorColuna + 1) * g.larguraColuna,
    altura: y + g.margem,
  };
}

/** Ponto na borda do retângulo do Nó, na direção de (alvoX, alvoY). */
export function pontoNaBorda(x: number, y: number, alvoX: number, alvoY: number): { x: number; y: number } {
  const dx = alvoX - x;
  const dy = alvoY - y;
  if (dx === 0 && dy === 0) return { x, y };
  const meiaL = GEOMETRIA.larguraNo / 2 + 3;
  const meiaA = GEOMETRIA.alturaNo / 2 + 3;
  const escala = Math.min(dx !== 0 ? meiaL / Math.abs(dx) : Infinity, dy !== 0 ? meiaA / Math.abs(dy) : Infinity);
  return { x: x + dx * escala, y: y + dy * escala };
}

/** Caminho SVG levemente curvo entre dois Nós (a curva separa idas e voltas entre o mesmo par). */
export function caminhoDaAresta(origem: NoPosicionado, destino: NoPosicionado): { d: string; meio: { x: number; y: number } } {
  const dx = destino.x - origem.x;
  const dy = destino.y - origem.y;
  const comprimento = Math.hypot(dx, dy) || 1;
  const curva = Math.min(40, comprimento * 0.12);
  const cx = (origem.x + destino.x) / 2 + (-dy / comprimento) * curva;
  const cy = (origem.y + destino.y) / 2 + (dx / comprimento) * curva;
  const inicio = pontoNaBorda(origem.x, origem.y, cx, cy);
  const fim = pontoNaBorda(destino.x, destino.y, cx, cy);
  return {
    d: `M ${inicio.x.toFixed(1)} ${inicio.y.toFixed(1)} Q ${cx.toFixed(1)} ${cy.toFixed(1)} ${fim.x.toFixed(1)} ${fim.y.toFixed(1)}`,
    meio: { x: 0.25 * inicio.x + 0.5 * cx + 0.25 * fim.x, y: 0.25 * inicio.y + 0.5 * cy + 0.25 * fim.y },
  };
}
