import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { EIXOS } from '../tipos/Metricas';
import { MetricaCard } from './MetricaCard';

describe('MetricaCard', () => {
  it('mostra o valor normalizado em pt-BR e abre o cálculo', async () => {
    const user = userEvent.setup();
    render(<MetricaCard resultado={{ eixo: 'ESC', aplicavel: true, valorBruto: 0.65, valorNormalizado: 3.25, motivo: null }} />);
    expect(screen.getByText('3,25')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'ver cálculo' }));
    expect(screen.getByText(/C\(n_b, 2\)/)).toBeInTheDocument();
  });

  it('mostra o motivo quando o eixo não é aplicável, em vez de um número', () => {
    render(<MetricaCard resultado={{ eixo: 'INT', aplicavel: false, valorBruto: null, valorNormalizado: null, motivo: 'M = 0 — sem arestas no grafo' }} />);
    expect(screen.getByText('Não aplicável')).toBeInTheDocument();
    expect(screen.getByText('M = 0 — sem arestas no grafo')).toBeInTheDocument();
  });
});

// Cada eixo precisa ter fórmula e explicação: um eixo sem texto de ajuda derrubaria a tela do Radar inteira.
describe('MetricaCard — todos os eixos do Radar', () => {
  it('abre o cálculo dos dez eixos mostrando fórmula e resultado', async () => {
    const user = userEvent.setup();
    for (const eixo of EIXOS) {
      const { unmount } = render(<MetricaCard resultado={{ eixo, aplicavel: true, valorBruto: 0.5, valorNormalizado: 2.5, motivo: null }} />);
      await user.click(screen.getByRole('button', { name: 'ver cálculo' }));
      expect(screen.getByText('Fórmula'), eixo).toBeInTheDocument();
      unmount();
    }
  });
});
