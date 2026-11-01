import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { EditarCasoForm } from './EditarCasoForm';

const caso = { nome: 'Pedidos', dominioDescricao: 'App do cliente' };
const campoNome = () => screen.getByRole('textbox', { name: 'Nome do sistema' });
const campoDominio = () => screen.getByRole('textbox', { name: /Domínio de negócio/ });
const salvar = () => screen.getByRole('button', { name: 'Salvar alterações' });

describe('EditarCasoForm', () => {
  it('pré-preenche nome e domínio, com contadores', () => {
    render(<EditarCasoForm caso={caso} aoSalvar={vi.fn()} aoCancelar={vi.fn()} />);
    expect(campoNome()).toHaveValue('Pedidos');
    expect(campoDominio()).toHaveValue('App do cliente');
    expect(screen.getByText('7/120')).toBeInTheDocument();
    expect(screen.getByText('14/1000')).toBeInTheDocument();
  });

  it('desabilita salvar quando o nome fica vazio ou só com espaços', async () => {
    const user = userEvent.setup();
    render(<EditarCasoForm caso={caso} aoSalvar={vi.fn()} aoCancelar={vi.fn()} />);
    await user.clear(campoNome());
    expect(salvar()).toBeDisabled();
    await user.type(campoNome(), '   ');
    expect(salvar()).toBeDisabled();
  });

  it('envia nome e domínio sem espaços nas pontas', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<EditarCasoForm caso={caso} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);
    await user.clear(campoNome());
    await user.type(campoNome(), '  Pedidos v2  ');
    await user.clear(campoDominio());
    await user.type(campoDominio(), '  Domínio novo ');
    await user.click(salvar());
    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Pedidos v2', dominioDescricao: 'Domínio novo' });
  });

  it('descrição apagada é enviada como null', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<EditarCasoForm caso={caso} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);
    await user.clear(campoDominio());
    await user.click(salvar());
    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Pedidos', dominioDescricao: null });
  });

});
