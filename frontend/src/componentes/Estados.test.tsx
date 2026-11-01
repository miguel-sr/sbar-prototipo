import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ErroDaApi } from '../servicos/apiClient';
import { MensagemDeErro } from './Estados';

describe('MensagemDeErro', () => {
  it('mostra a mensagem amigável e oferece "Tentar de novo" para falha de rede/5xx', async () => {
    const user = userEvent.setup();
    const aoTentarDeNovo = vi.fn();
    render(<MensagemDeErro erro={new TypeError('Failed to fetch')} aoTentarDeNovo={aoTentarDeNovo} />);
    expect(screen.getByRole('alert')).toHaveTextContent('Não foi possível falar com a API');
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(aoTentarDeNovo).toHaveBeenCalledTimes(1);
  });

  it('não oferece "Tentar de novo" para um 404 (continuaria sendo 404)', () => {
    render(<MensagemDeErro erro={new ErroDaApi(404, 'não existe')} aoTentarDeNovo={vi.fn()} />);
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument();
  });
});

