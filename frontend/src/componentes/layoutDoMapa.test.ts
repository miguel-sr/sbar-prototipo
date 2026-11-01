import { describe, expect, it } from 'vitest';
import type { Aresta } from '../tipos/Aresta';
import type { No } from '../tipos/No';
import { calcularColunas, calcularLayout } from './layoutDoMapa';

const no = (id: string, banda: No['banda'], tipo: No['tipo'] = 'Processo'): No => ({ id, nome: id, tipo, banda });
const aresta = (o: string, d: string): Aresta => ({ id: `${o}-${d}`, noOrigemId: o, noDestinoId: d, tipo: 'Sincrona', peso: 2, justificativa: 'teste' });

describe('layoutDoMapa', () => {
  const nos = [no('A', 'Ator', 'Ator'), no('TP1', 'Frontstage', 'Touchpoint'), no('P1', 'Backstage'), no('P2', 'Backstage'), no('E1', 'Externo', 'Externo')];
  const arestas = [aresta('A', 'TP1'), aresta('TP1', 'P1'), aresta('TP1', 'P2'), aresta('E1', 'P2')];

  it('coloca cada Nó na coluna da sua distância a partir do Ator', () => {
    const colunas = calcularColunas(nos, arestas);
    expect(colunas.get('A')).toBe(0);
    expect(colunas.get('TP1')).toBe(1);
    expect(colunas.get('P1')).toBe(2);
    // E1 não é alcançável a partir do Ator: fica ao lado do vizinho já posicionado
    expect(colunas.get('E1')).toBe(3);
  });

  it('empilha Nós da mesma Banda na mesma coluna em vez de alargar o Mapa', () => {
    const layout = calcularLayout(nos, arestas);
    const p1 = layout.nos.get('P1')!;
    const p2 = layout.nos.get('P2')!;
    expect(p1.x).toBe(p2.x);
    expect(p2.y).toBeGreaterThan(p1.y);
    expect(layout.raias.map((r) => r.banda)).toEqual(['Ator', 'Frontstage', 'Backstage', 'Externo']);
  });

  it('não quebra com Mapa sem Ator nem arestas', () => {
    const layout = calcularLayout([no('X', 'Suporte', 'Dados')], []);
    expect(layout.nos.get('X')?.coluna).toBe(0);
  });
});
