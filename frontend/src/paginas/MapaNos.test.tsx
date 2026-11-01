import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaNo, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const cliente = fabricaNo('a', 'Cliente', 'Ator');
const cardapio = fabricaNo('b', 'Cardápio', 'Touchpoint');
const checkout = fabricaNo('c', 'Checkout', 'Touchpoint');
const validacao = fabricaNo('d', 'Validação', 'Processo');
const isolado = fabricaNo('e', 'Isolado', 'Dados');
const arestas = [fabricaAresta('x1', 'a', 'b'), fabricaAresta('x2', 'b', 'c'), fabricaAresta('x3', 'c', 'd')];

const casoCompleto = () => fabricaCaso({ nos: [cliente, cardapio, checkout, validacao, isolado], arestas });

beforeEach(() => vi.clearAllMocks());

describe('MapaNos (T3) — listagem', () => {
  it('agrupa os Nós por Banda, na ordem do Blueprint, e mostra a contagem', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    renderizarCaso();

    expect(await screen.findByRole('heading', { name: 'Nós cadastrados (5)' })).toBeInTheDocument();
    const tabelas = screen.getAllByRole('table');
    expect(tabelas).toHaveLength(4); // Ator, Frontstage, Backstage, Suporte (Dados)
    expect(within(tabelas[0]).getByText('Cliente')).toBeInTheDocument();
    expect(within(tabelas[1]).getByText('Cardápio')).toBeInTheDocument();
    expect(within(tabelas[1]).getByText('Checkout')).toBeInTheDocument();
    expect(within(tabelas[2]).getByText('Validação')).toBeInTheDocument();
    expect(within(tabelas[3]).getByText('Isolado')).toBeInTheDocument();
  });

  it('sem Nós, mostra o estado vazio e não oferece o atalho para Arestas', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso());
    renderizarCaso();
    expect(await screen.findByText('Nenhum Nó ainda')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Próximo: Arestas/ })).not.toBeInTheDocument();
  });
});

describe('MapaNos (T3) — criar', () => {
  it('cria o Nó, recarrega o caso e mostra o Nó novo', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(fabricaCaso({ nos: [cliente] }))
      .mockResolvedValueOnce(fabricaCaso({ nos: [cliente, fabricaNo('n', 'Novo Nó', 'Touchpoint')] }));
    vi.mocked(apiClient.criarNo).mockResolvedValue(fabricaNo('n', 'Novo Nó', 'Touchpoint'));
    renderizarCaso();

    await user.type(await screen.findByRole('textbox', { name: 'Nome' }), 'Novo Nó{Enter}');

    expect(apiClient.criarNo).toHaveBeenCalledWith(ID_DO_CASO, { nome: 'Novo Nó', tipo: 'Touchpoint' });
    expect(await screen.findByRole('heading', { name: 'Nós cadastrados (2)' })).toBeInTheDocument();
    expect(screen.getByText('Novo Nó')).toBeInTheDocument();
    expect(apiClient.obterCaso).toHaveBeenCalledTimes(2);
  });
});

describe('MapaNos (T3) — editar', () => {
  it('abre o formulário em modo edição, salva o PUT e volta ao modo de criação depois de recarregar', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(casoCompleto())
      .mockResolvedValueOnce(fabricaCaso({ nos: [cliente, fabricaNo('b', 'Cardápio digital', 'Touchpoint'), checkout, validacao, isolado], arestas }));
    vi.mocked(apiClient.atualizarNo).mockResolvedValue(fabricaNo('b', 'Cardápio digital', 'Touchpoint'));
    renderizarCaso();

    await user.click(await screen.findByRole('button', { name: 'Editar Cardápio' }));
    expect(screen.getByRole('heading', { name: 'Editar Nó' })).toBeInTheDocument();
    const campo = screen.getByRole('textbox', { name: 'Nome' });
    expect(campo).toHaveValue('Cardápio');
    await user.type(campo, ' digital');
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(apiClient.atualizarNo).toHaveBeenCalledWith(ID_DO_CASO, 'b', { nome: 'Cardápio digital', tipo: 'Touchpoint' });
    expect(await screen.findByRole('heading', { name: 'Adicionar Nó' })).toBeInTheDocument();
    expect(screen.getByText('Cardápio digital')).toBeInTheDocument();
  });

  it('"Cancelar edição" volta ao formulário de criação sem chamar a API', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    renderizarCaso();
    await user.click(await screen.findByRole('button', { name: 'Editar Cliente' }));
    await user.click(screen.getByRole('button', { name: 'Cancelar edição' }));
    expect(screen.getByRole('heading', { name: 'Adicionar Nó' })).toBeInTheDocument();
    expect(apiClient.atualizarNo).not.toHaveBeenCalled();
  });
});

describe('MapaNos (T3) — excluir', () => {
  // Excluir um Nó remove as Arestas ligadas a ele (cascata no servidor): o aviso diz quantas.
  it.each([
    ['Cardápio', 'Excluir este Nó também remove 2 arestas ligadas a ele.'],
    ['Cliente', 'Excluir este Nó também remove 1 aresta ligada a ele.'],
    ['Isolado', 'Excluir este Nó. Não dá para desfazer.'],
  ])('o aviso de "%s" conta as arestas ligadas (singular/plural) no cliente', async (nome, aviso) => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    renderizarCaso();

    await user.click(await screen.findByRole('button', { name: `Excluir Nó ${nome}` }));
    expect(within(screen.getByRole('dialog')).getByText(aviso)).toBeInTheDocument();
  });

  it('confirmar chama a exclusão do Nó certo e recarrega o caso (a lista reflete o resultado)', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(casoCompleto())
      .mockResolvedValueOnce(fabricaCaso({ nos: [cliente, checkout, validacao, isolado], arestas: [fabricaAresta('x3', 'c', 'd')] }));
    vi.mocked(apiClient.excluirNo).mockResolvedValue(undefined);
    renderizarCaso();

    await user.click(await screen.findByRole('button', { name: 'Excluir Nó Cardápio' }));
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(apiClient.excluirNo).toHaveBeenCalledWith(ID_DO_CASO, 'b');
    expect(await screen.findByRole('heading', { name: 'Nós cadastrados (4)' })).toBeInTheDocument();
    expect(screen.queryByText('Cardápio')).not.toBeInTheDocument();
    expect(screen.getByText('4 nós · 1 aresta')).toBeInTheDocument();
  });
});
