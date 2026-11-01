import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { fabricaCaso, ID_DO_CASO, renderizarComRota } from '../testes/auxiliares';
import { CasoForm } from './CasoForm';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const campoNome = () => screen.getByRole('textbox', { name: 'Nome do sistema' });
const campoDominio = () => screen.getByRole('textbox', { name: /Domínio de negócio/ });
const botaoCriar = () => screen.getByRole('button', { name: 'Criar e cadastrar Nós' });

function renderizar() {
  return renderizarComRota(<CasoForm />, '/casos/novo', { [`/casos/${ID_DO_CASO}/nos`]: <p>Tela de Nós</p>, '/': <p>Página inicial</p> });
}

beforeEach(() => vi.clearAllMocks());

describe('CasoForm (T2)', () => {
  it('só habilita a criação com um nome de verdade e limita nome (120) e domínio (1000)', async () => {
    const user = userEvent.setup();
    renderizar();
    expect(screen.getByText('0/120')).toBeInTheDocument();
    expect(screen.getByText('0/1000')).toBeInTheDocument();
    expect(botaoCriar()).toBeDisabled();
    await user.type(campoNome(), '    ');
    expect(botaoCriar()).toBeDisabled();
  });

  it('cria com nome e domínio sem espaços nas pontas e vai para a tela de Nós do novo caso', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.criarCaso).mockResolvedValue(fabricaCaso());
    renderizar();
    await user.type(campoNome(), '  Sistema de Pedidos ');
    await user.type(campoDominio(), ' Restaurante  ');
    await user.click(botaoCriar());

    expect(apiClient.criarCaso).toHaveBeenCalledWith({ nome: 'Sistema de Pedidos', dominioDescricao: 'Restaurante' });
    expect(await screen.findByText('Tela de Nós')).toBeInTheDocument();
  });

  it('envia domínio null quando a descrição fica em branco', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.criarCaso).mockResolvedValue(fabricaCaso());
    renderizar();
    await user.type(campoNome(), 'Sistema');
    await user.click(botaoCriar());
    expect(apiClient.criarCaso).toHaveBeenCalledWith({ nome: 'Sistema', dominioDescricao: null });
  });

  it('mostra o title do 422, continua na página e libera o botão', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.criarCaso).mockRejectedValue(new ErroDaApi(422, 'Nome do Estudo de Caso não pode ter mais de 120 caracteres.'));
    renderizar();
    await user.type(campoNome(), 'Sistema');
    await user.click(botaoCriar());

    expect(await screen.findByRole('alert')).toHaveTextContent('não pode ter mais de 120 caracteres');
    expect(screen.getByRole('heading', { name: 'Novo Estudo de Caso' })).toBeInTheDocument();
    expect(botaoCriar()).toBeEnabled();
  });
});
