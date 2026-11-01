import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ErroDaApi } from '../servicos/apiClient';
import { NoForm } from './NoForm';

const campoNome = () => screen.getByRole('textbox', { name: 'Nome' });
const botaoAdicionar = () => screen.getByRole('button', { name: 'Adicionar Nó' });

describe('NoForm — criação', () => {
  it('só habilita o envio com um nome de verdade (vazio e só espaços não valem) e limita o nome a 100 caracteres', async () => {
    const user = userEvent.setup();
    render(<NoForm aoSalvar={vi.fn()} />);
    expect(screen.getByText('0/100')).toBeInTheDocument();
    expect(botaoAdicionar()).toBeDisabled();
    await user.type(campoNome(), '     ');
    expect(botaoAdicionar()).toBeDisabled();
    await user.type(campoNome(), 'Carrinho');
    expect(botaoAdicionar()).toBeEnabled();
  });

  it('envia o nome sem espaços nas pontas e o tipo escolhido (Touchpoint é o padrão)', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<NoForm aoSalvar={aoSalvar} />);
    await user.type(campoNome(), '   Carrinho/Checkout  ');
    await user.click(botaoAdicionar());
    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Carrinho/Checkout', tipo: 'Touchpoint' });
  });

  it('Enter envia; depois de criar, limpa o nome, devolve o foco ao campo e mantém o tipo para o próximo Nó', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<NoForm aoSalvar={aoSalvar} />);
    await user.click(screen.getByRole('radio', { name: /^Suporte/ }));
    await user.type(campoNome(), 'Autenticação{Enter}');

    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Autenticação', tipo: 'Suporte' });
    await vi.waitFor(() => expect(campoNome()).toHaveValue(''));
    expect(campoNome()).toHaveFocus();
    expect(screen.getByRole('radio', { name: /^Suporte/ })).toBeChecked();
  });

  it('mostra o title do 422 da API e mantém o que foi digitado', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockRejectedValue(new ErroDaApi(422, 'Nome não pode ter mais de 100 caracteres.'));
    render(<NoForm aoSalvar={aoSalvar} />);
    await user.type(campoNome(), 'Algo{Enter}');

    expect(await screen.findByRole('alert')).toHaveTextContent('Nome não pode ter mais de 100 caracteres.');
    expect(campoNome()).toHaveValue('Algo');
    expect(botaoAdicionar()).toBeEnabled();
  });

  it('não oferece campo de Banda (ela é derivada do Tipo no backend)', () => {
    render(<NoForm aoSalvar={vi.fn()} />);
    expect(screen.queryByLabelText(/banda/i)).not.toBeInTheDocument();
    expect(screen.getByText('A Banda é definida automaticamente a partir do Tipo.')).toBeInTheDocument();
  });
});

describe('NoForm — edição', () => {
  it('pré-preenche nome e tipo e muda o texto do título e do botão', () => {
    render(<NoForm inicial={{ nome: 'Pedido', tipo: 'Processo' }} aoSalvar={vi.fn()} aoCancelar={vi.fn()} />);
    expect(screen.getByRole('heading', { name: 'Editar Nó' })).toBeInTheDocument();
    expect(campoNome()).toHaveValue('Pedido');
    expect(screen.getByRole('radio', { name: /^Processo/ })).toBeChecked();
    expect(screen.getByRole('button', { name: 'Salvar alterações' })).toBeEnabled();
  });

  it('salva o novo nome e o novo tipo', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<NoForm inicial={{ nome: 'Pedido', tipo: 'Processo' }} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);
    await user.clear(campoNome());
    await user.type(campoNome(), 'Pedido na tela');
    await user.click(screen.getByRole('radio', { name: /^Touchpoint/ }));
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));
    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Pedido na tela', tipo: 'Touchpoint' });
  });

  it('não limpa o campo depois de salvar uma edição', async () => {
    const user = userEvent.setup();
    render(<NoForm inicial={{ nome: 'Pedido', tipo: 'Processo' }} aoSalvar={vi.fn().mockResolvedValue(undefined)} aoCancelar={vi.fn()} />);
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));
    expect(campoNome()).toHaveValue('Pedido');
  });
});
