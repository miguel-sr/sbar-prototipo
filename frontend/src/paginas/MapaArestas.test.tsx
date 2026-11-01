import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { JUSTIFICATIVA_PENDENTE } from '../dominio/validacao';
import { apiClient } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaNo, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/arestas`;
const nos = [fabricaNo('a', 'Cliente', 'Ator'), fabricaNo('b', 'Cardápio', 'Touchpoint'), fabricaNo('c', 'Validação', 'Processo')];

beforeEach(() => vi.clearAllMocks());

describe('MapaArestas (T4) — listagem', () => {
  it('pede pelo menos dois Nós antes de cadastrar Arestas', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso({ nos: [nos[0]] }));
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Cadastre pelo menos dois Nós primeiro')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ir para o cadastro de Nós' })).toBeInTheDocument();
  });

  it('com Nós mas sem Arestas, mostra o estado vazio e o formulário', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso({ nos }));
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Nenhuma Aresta ainda')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Adicionar Aresta' })).toBeInTheDocument();
  });

  it('lista origem → destino com o selo do tipo e o peso', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Critica', 3)] }));
    renderizarCaso(CAMINHO);

    expect(await screen.findByRole('heading', { name: 'Arestas cadastradas (1)' })).toBeInTheDocument();
    const linha = screen.getByRole('row', { name: /Cliente.*Cardápio/ });
    expect(within(linha).getByText('CRI')).toBeInTheDocument();
    expect(within(linha).getByText(/3/)).toHaveTextContent('3 · Alto');
  });

  it('arestas paralelas entre o mesmo par de Nós aparecem como linhas separadas, cada uma com seu tipo', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(
      fabricaCaso({ nos, arestas: [fabricaAresta('p1', 'b', 'c', 'Assincrona', 2), fabricaAresta('p2', 'b', 'c', 'Batch', 1)] }),
    );
    renderizarCaso(CAMINHO);

    await screen.findByRole('heading', { name: 'Arestas cadastradas (2)' });
    const linhas = screen.getAllByRole('row', { name: /Cardápio.*Validação/ });
    expect(linhas).toHaveLength(2);
    expect(within(linhas[0]).getByText('ASY')).toBeInTheDocument();
    expect(within(linhas[1]).getByText('BAT')).toBeInTheDocument();
  });
});

describe('MapaArestas (T4) — justificativa (RN03)', () => {
  it('mostra a justificativa de cada Aresta na tabela', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(
      fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Sincrona', 3, 'Cliente sempre abre o cardápio')] }),
    );
    renderizarCaso(CAMINHO);
    const linha = await screen.findByRole('row', { name: /Cliente.*Cardápio/ });
    expect(within(linha).getByText('Cliente sempre abre o cardápio')).toBeInTheDocument();
    expect(within(linha).queryByText('pendente de revisão')).not.toBeInTheDocument();
  });

  it('marca como "pendente de revisão" a Aresta com a justificativa-padrão da migração', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(
      fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Sincrona', 3, JUSTIFICATIVA_PENDENTE)] }),
    );
    renderizarCaso(CAMINHO);
    const linha = await screen.findByRole('row', { name: /Cliente.*Cardápio/ });
    expect(within(linha).getByText('pendente de revisão')).toBeInTheDocument();
  });

  it('editar uma Aresta pendente abre o formulário com a justificativa vazia', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(
      fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Sincrona', 3, JUSTIFICATIVA_PENDENTE)] }),
    );
    renderizarCaso(CAMINHO);
    await user.click(await screen.findByRole('button', { name: 'Editar Aresta Cliente → Cardápio' }));
    expect(screen.getByRole('textbox', { name: 'Justificativa' })).toHaveValue('');
    expect(screen.getByRole('button', { name: 'Salvar alterações' })).toBeDisabled();
  });
});

describe('MapaArestas (T4) — criar, editar e excluir', () => {
  it('cria a Aresta com os ids escolhidos e recarrega o caso', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(fabricaCaso({ nos }))
      .mockResolvedValueOnce(fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Sincrona', 3)] }));
    vi.mocked(apiClient.criarAresta).mockResolvedValue(fabricaAresta('x1', 'a', 'b', 'Sincrona', 3));
    renderizarCaso(CAMINHO);

    await user.selectOptions(await screen.findByRole('combobox', { name: 'De (origem)' }), 'Cliente');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Para (destino)' }), 'Cardápio');
    await user.type(screen.getByRole('textbox', { name: 'Justificativa' }), 'Cliente abre o cardápio');
    await user.click(screen.getByRole('button', { name: 'Adicionar Aresta' }));

    expect(apiClient.criarAresta).toHaveBeenCalledWith(ID_DO_CASO, {
      noOrigemId: 'a',
      noDestinoId: 'b',
      tipo: 'Sincrona',
      peso: 3,
      justificativa: 'Cliente abre o cardápio',
    });
    expect(await screen.findByRole('heading', { name: 'Arestas cadastradas (1)' })).toBeInTheDocument();
    expect(apiClient.obterCaso).toHaveBeenCalledTimes(2);
  });

  it('edita a Aresta: pré-preenche, envia o PUT e volta ao formulário de criação', async () => {
    const user = userEvent.setup();
    const original = fabricaAresta('x1', 'a', 'b', 'Sincrona', 2);
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(fabricaCaso({ nos, arestas: [original] }))
      .mockResolvedValueOnce(fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b', 'Batch', 2)] }));
    vi.mocked(apiClient.atualizarAresta).mockResolvedValue(fabricaAresta('x1', 'a', 'b', 'Batch', 2));
    renderizarCaso(CAMINHO);

    await user.click(await screen.findByRole('button', { name: 'Editar Aresta Cliente → Cardápio' }));
    expect(screen.getByRole('heading', { name: 'Editar Aresta' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'De (origem)' })).toHaveValue('a');
    await user.click(screen.getByRole('radio', { name: /Batch/ }));
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(apiClient.atualizarAresta).toHaveBeenCalledWith(ID_DO_CASO, 'x1', {
      noOrigemId: 'a',
      noDestinoId: 'b',
      tipo: 'Batch',
      peso: 2,
      justificativa: 'Interação do Blueprint (teste)',
    });
    expect(await screen.findByRole('heading', { name: 'Adicionar Aresta' })).toBeInTheDocument();
    expect(within(screen.getByRole('row', { name: /Cliente.*Cardápio/ })).getByText('BAT')).toBeInTheDocument();
  });

  it('exclui a Aresta certa depois da confirmação e recarrega o caso', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(fabricaCaso({ nos, arestas: [fabricaAresta('x1', 'a', 'b'), fabricaAresta('x2', 'b', 'c')] }))
      .mockResolvedValueOnce(fabricaCaso({ nos, arestas: [fabricaAresta('x2', 'b', 'c')] }));
    vi.mocked(apiClient.excluirAresta).mockResolvedValue(undefined);
    renderizarCaso(CAMINHO);

    await user.click(await screen.findByRole('button', { name: 'Excluir Aresta Cliente → Cardápio' }));
    const modal = screen.getByRole('dialog');
    expect(within(modal).getByText('Excluir esta Aresta. Não dá para desfazer.')).toBeInTheDocument();
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(apiClient.excluirAresta).toHaveBeenCalledWith(ID_DO_CASO, 'x1');
    expect(await screen.findByRole('heading', { name: 'Arestas cadastradas (1)' })).toBeInTheDocument();
  });
});
