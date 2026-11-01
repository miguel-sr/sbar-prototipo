import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { TabelaDeBandas } from './TabelaDeBandas';

const bandas = [{ banda: 'Suporte' as const, quantidadeDeNos: 3, media: 7, desvioPadrao: 2.94, limiar: 9.94 }];

// O cabeçalho da tabela é exibido em maiúsculas (CSS), e a caixa alta transforma as letras gregas μ e σ em Μ e Σ — o que lê
// como "M (MÉDIA)" e "Σ (DESVIO)", contradizendo o texto da seção ("μ + σ"). Os símbolos ficam em um elemento que preserva a caixa.
describe('TabelaDeBandas — símbolos gregos do cabeçalho', () => {
  it('mantém μ e σ em elementos que não mudam de caixa, nas três colunas que os usam', () => {
    render(<TabelaDeBandas bandas={bandas} nos={[]} nosGargalo={[]} />);

    const cabecalhos = within(screen.getByRole('table')).getAllByRole('columnheader');
    const comSimbolo = cabecalhos.filter((c) => /[μσ]/.test(c.textContent ?? ''));
    expect(comSimbolo.map((c) => c.textContent)).toEqual(['μ (média)', 'σ (desvio)', 'Limiar μ + σ']);

    for (const cabecalho of comSimbolo) {
      const simbolos = [...cabecalho.querySelectorAll('.simbolo-grego')].map((s) => s.textContent);
      const esperados = [...(cabecalho.textContent ?? '').matchAll(/[μσ]/g)].map((m) => m[0]);
      expect(simbolos, `símbolos de "${cabecalho.textContent}"`).toEqual(esperados);
    }
  });

  it('a folha de estilos desliga a caixa alta desses símbolos', () => {
    const css = readFileSync(resolve(process.cwd(), 'src/estilos.css'), 'utf8');
    expect(css).toMatch(/\.simbolo-grego\s*\{[^}]*text-transform:\s*none/);
  });
});
