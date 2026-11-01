import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ErroDaApi } from '../servicos/apiClient';
import { ExclusaoComConfirmacao } from './ExclusaoComConfirmacao';

// O polyfill do <dialog> (setupTests) só abre/fecha: Esc, clique no fundo e foco ficam no E2E.
function renderizar(aoConfirmar = vi.fn().mockResolvedValue(undefined)) {
  render(<ExclusaoComConfirmacao alvo="Nó Carrinho" aviso="Excluir este Nó também remove 2 arestas ligadas a ele." aoConfirmar={aoConfirmar} />);
  return aoConfirmar;
}

const abrirModal = async (user: ReturnType<typeof userEvent.setup>) => {
  await user.click(screen.getByRole('button', { name: 'Excluir Nó Carrinho' }));
  return screen.getByRole('dialog', { name: 'Confirmar exclusão de Nó Carrinho' });
};

describe('ExclusaoComConfirmacao', () => {

  it('o modal só existe depois do clique e traz título e o aviso das consequências', async () => {
    const user = userEvent.setup();
    renderizar();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    const modal = await abrirModal(user);
    expect(within(modal).getByRole('heading', { name: 'Excluir Nó Carrinho?' })).toBeInTheDocument();
    expect(within(modal).getByText('Excluir este Nó também remove 2 arestas ligadas a ele.')).toBeInTheDocument();
  });

  it('Cancelar fecha o modal sem chamar a exclusão', async () => {
    const user = userEvent.setup();
    const aoConfirmar = renderizar();
    const modal = await abrirModal(user);
    await user.click(within(modal).getByRole('button', { name: 'Cancelar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(aoConfirmar).not.toHaveBeenCalled();
  });

  it('desabilita os dois botões enquanto a exclusão está em andamento', async () => {
    const user = userEvent.setup();
    let concluir!: () => void;
    const aoConfirmar = vi.fn(() => new Promise<void>((resolver) => (concluir = resolver)));
    renderizar(aoConfirmar);
    const modal = await abrirModal(user);
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(within(modal).getByRole('button', { name: 'Excluindo…' })).toBeDisabled();
    expect(within(modal).getByRole('button', { name: 'Cancelar' })).toBeDisabled();
    // clique repetido não dispara outra exclusão
    await user.click(within(modal).getByRole('button', { name: 'Excluindo…' }));
    expect(aoConfirmar).toHaveBeenCalledTimes(1);
    concluir();
  });

  it('quando a exclusão falha, o modal continua aberto, mostra a mensagem e libera os botões', async () => {
    const user = userEvent.setup();
    const aoConfirmar = vi.fn().mockRejectedValue(new ErroDaApi(422, 'Nó não pode ser excluído agora.'));
    renderizar(aoConfirmar);
    const modal = await abrirModal(user);
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(await within(modal).findByRole('alert')).toHaveTextContent('Nó não pode ser excluído agora.');
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(within(modal).getByRole('button', { name: 'Confirmar exclusão' })).toBeEnabled();
    expect(within(modal).getByRole('button', { name: 'Cancelar' })).toBeEnabled();
  });

  it('depois de um erro, Cancelar fecha e limpa a mensagem para a próxima abertura', async () => {
    const user = userEvent.setup();
    const aoConfirmar = vi.fn().mockRejectedValue(new ErroDaApi(500, 'Falhou'));
    renderizar(aoConfirmar);
    let modal = await abrirModal(user);
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));
    await within(modal).findByRole('alert');
    await user.click(within(modal).getByRole('button', { name: 'Cancelar' }));

    modal = await abrirModal(user);
    expect(within(modal).queryByRole('alert')).not.toBeInTheDocument();
  });
});
