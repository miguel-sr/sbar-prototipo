import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '../servicos/apiClient';
import { fabricaPerfil, renderizarComRota } from '../testes/auxiliares';
import { PerfisArquiteturais } from './PerfisArquiteturais';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const monolito = fabricaPerfil('p1', 'Monólito Modular', 2);
const micro = fabricaPerfil('p2', 'Microsserviços', 4);

const renderizar = () => renderizarComRota(<PerfisArquiteturais />, '/perfis');
const formularioDe = (nome: string) => screen.getByDisplayValue(nome).closest('form') as HTMLFormElement;

beforeEach(() => vi.clearAllMocks());

describe('PerfisArquiteturais (T9)', () => {

  it('lista um formulário por perfil, com nome e os dez valores', async () => {
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([monolito, micro]);
    renderizar();

    await screen.findByDisplayValue('Monólito Modular');
    expect(screen.getByDisplayValue('Microsserviços')).toBeInTheDocument();
    expect(within(formularioDe('Monólito Modular')).getAllByRole('spinbutton')).toHaveLength(10);
  });

  it('salva os valores como números e recarrega a lista', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([monolito]);
    vi.mocked(apiClient.atualizarPerfil).mockResolvedValue(monolito);
    renderizar();

    await screen.findByDisplayValue('Monólito Modular');
    const form = within(formularioDe('Monólito Modular'));
    expect(form.getByRole('button', { name: 'Salvar alterações' })).toBeDisabled(); // sem mudança, nada a salvar
    const [co] = form.getAllByRole('spinbutton');
    await user.clear(co);
    await user.type(co, '3.5');
    await user.click(form.getByRole('button', { name: 'Salvar alterações' }));

    expect(apiClient.atualizarPerfil).toHaveBeenCalledWith('p1', expect.objectContaining({ nome: 'Monólito Modular', valorCO: 3.5, valorESC: 2, valorDEP: 2 }));
    await vi.waitFor(() => expect(apiClient.listarPerfis).toHaveBeenCalledTimes(2));
  });

  it('valor vazio mostra o aviso do app e NÃO chama a API', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([monolito]);
    renderizar();

    await screen.findByDisplayValue('Monólito Modular');
    const form = within(formularioDe('Monólito Modular'));
    await user.clear(form.getAllByRole('spinbutton')[0]);
    await user.click(form.getByRole('button', { name: 'Salvar alterações' }));

    expect(form.getByText('O valor de CO precisa ser um número entre 0 e 5.')).toBeInTheDocument();
    expect(apiClient.atualizarPerfil).not.toHaveBeenCalled();
  });

  it('remover pede confirmação em modal e, confirmada, remove o perfil e recarrega a lista', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.listarPerfis).mockResolvedValueOnce([monolito, micro]).mockResolvedValueOnce([micro]);
    vi.mocked(apiClient.removerPerfil).mockResolvedValue(undefined);
    renderizar();

    await screen.findByDisplayValue('Monólito Modular');
    await user.click(screen.getByRole('button', { name: 'Excluir Perfil Monólito Modular' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(apiClient.removerPerfil).toHaveBeenCalledWith('p1');
    await vi.waitFor(() => expect(screen.queryByDisplayValue('Monólito Modular')).not.toBeInTheDocument());
    expect(screen.getByDisplayValue('Microsserviços')).toBeInTheDocument();
  });

  it('"+ Novo perfil" abre o formulário de criação e cria com os dez valores', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([]);
    vi.mocked(apiClient.criarPerfil).mockResolvedValue(fabricaPerfil('novo', 'Serverless', 1));
    renderizar();

    await screen.findByText('Nenhum perfil cadastrado');
    await user.click(screen.getByRole('button', { name: '+ Novo perfil' }));
    await user.type(screen.getByRole('textbox', { name: 'Nome do estilo arquitetural' }), 'Serverless');
    for (const campo of screen.getAllByRole('spinbutton')) await user.type(campo, '1');
    await user.click(screen.getByRole('button', { name: 'Criar perfil' }));

    expect(apiClient.criarPerfil).toHaveBeenCalledWith(expect.objectContaining({ nome: 'Serverless', valorCO: 1, valorDEP: 1 }));
  });

  it('criar sem nome avisa e não chama a API', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([]);
    renderizar();
    await user.click(await screen.findByRole('button', { name: '+ Novo perfil' }));
    await user.click(screen.getByRole('button', { name: 'Criar perfil' }));
    expect(screen.getByText('Dê um nome ao perfil.')).toBeInTheDocument();
    expect(apiClient.criarPerfil).not.toHaveBeenCalled();
  });
});
