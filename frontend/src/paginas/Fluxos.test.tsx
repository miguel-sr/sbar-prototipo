import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaFluxo, fabricaNo, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/fluxos`;
const nos = [fabricaNo('a', 'Cliente', 'Ator'), fabricaNo('b', 'Cardápio', 'Touchpoint'), fabricaNo('c', 'Validação', 'Processo')];
const arestas = [fabricaAresta('x1', 'a', 'b'), fabricaAresta('x2', 'b', 'c')];
const casoCompleto = () => fabricaCaso({ nos, arestas });

beforeEach(() => vi.clearAllMocks());

describe('Fluxos (T4b) — estados iniciais', () => {
  it('sem Nós suficientes, pede para cadastrar Nós e Arestas primeiro e não chama a API de fluxos com erro', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso({ nos: [nos[0]] }));
    vi.mocked(apiClient.listarFluxos).mockResolvedValue([]);
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Cadastre Nós e Arestas primeiro')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Nós' })).toHaveAttribute('href', expect.stringMatching(/\/nos$/));
    expect(screen.getByRole('link', { name: 'Arestas' })).toHaveAttribute('href', expect.stringMatching(/\/arestas$/));
  });

  it('sem fluxos cadastrados, explica que o DEP fica não aplicável e mostra o formulário', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos).mockResolvedValue([]);
    renderizarCaso(CAMINHO);

    expect(await screen.findByText('Nenhum fluxo ainda')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Fluxos principais (0)' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Adicionar Fluxo' })).toBeInTheDocument();
    expect(apiClient.listarFluxos).toHaveBeenCalledWith(ID_DO_CASO);
    expect(screen.queryByRole('link', { name: 'Próximo: ver o Mapa →' })).not.toBeInTheDocument();
  });
});

describe('Fluxos (T4b) — listagem e validade (US03)', () => {
  it('lista cada fluxo com o caminho em ordem, o selo "válido" e as bandas percorridas', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos).mockResolvedValue([fabricaFluxo('f1', 'Pedido', ['a', 'b', 'c'], { bandasPercorridas: 3 })]);
    renderizarCaso(CAMINHO);

    expect(await screen.findByRole('heading', { name: 'Fluxos principais (1)' })).toBeInTheDocument();
    const caminho = screen.getByRole('list', { name: 'Passos do fluxo Pedido' });
    expect(within(caminho).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['Cliente', 'Cardápio', 'Validação']);
    expect(screen.getByText('válido')).toBeInTheDocument();
    expect(screen.getByText('3 bandas')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Próximo: ver o Mapa →' })).toBeInTheDocument();
  });

  // US03 / CT09: fluxo sem aresta em alguma etapa é sinalizado como inválido, indicando a etapa; fica fora do DEP.
  it('fluxo inválido: selo "inválido", etapa ausente com os nomes e o aviso de que não entra no DEP', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos).mockResolvedValue([
      fabricaFluxo('f1', 'Quebrado', ['a', 'b', 'c'], {
        valido: false,
        etapaAusente: { posicao: 2, origemId: 'b', destinoId: 'c', motivo: 'Não existe Aresta entre os dois Nós' },
      }),
      fabricaFluxo('f2', 'Bom', ['a', 'b']),
    ]);
    renderizarCaso(CAMINHO);

    expect(await screen.findByText('inválido')).toBeInTheDocument();
    expect(screen.getByText('válido')).toBeInTheDocument();
    expect(screen.getByText(/Etapa 2:/).closest('p')).toHaveTextContent('Etapa 2: Não existe Aresta entre os dois Nós (Cardápio → Validação)');
    expect(screen.getByText('1 fluxo ficou inválido e não entra no DEP até ser corrigido.')).toBeInTheDocument();
  });

  it('um Nó excluído aparece como "Nó removido" no caminho e na etapa ausente', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos).mockResolvedValue([
      fabricaFluxo('f1', 'Órfão', ['a', 'sumiu'], {
        valido: false,
        etapaAusente: { posicao: 1, origemId: 'a', destinoId: 'sumiu', motivo: 'O Nó foi removido' },
      }),
    ]);
    renderizarCaso(CAMINHO);
    const caminho = await screen.findByRole('list', { name: 'Passos do fluxo Órfão' });
    expect(within(caminho).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['Cliente', 'Nó removido']);
    expect(screen.getByText(/Etapa 1:/).closest('p')).toHaveTextContent('Etapa 1: O Nó foi removido (Cliente → Nó removido)');
  });
});

describe('Fluxos (T4b) — criar, editar e excluir', () => {
  it('cria o fluxo com a sequência escolhida e recarrega a lista', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos)
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([fabricaFluxo('f1', 'Pedido', ['a', 'b'], { bandasPercorridas: 2 })]);
    vi.mocked(apiClient.criarFluxo).mockResolvedValue(fabricaFluxo('f1', 'Pedido', ['a', 'b']));
    renderizarCaso(CAMINHO);

    await user.type(await screen.findByRole('textbox', { name: 'Nome do fluxo' }), 'Pedido');
    for (const nomeDoNo of ['Cliente', 'Cardápio']) {
      await user.selectOptions(screen.getByRole('combobox', { name: 'Próximo passo' }), nomeDoNo);
      await user.click(screen.getByRole('button', { name: 'Adicionar passo' }));
    }
    await user.click(screen.getByRole('button', { name: 'Adicionar Fluxo' }));

    expect(apiClient.criarFluxo).toHaveBeenCalledWith(ID_DO_CASO, { nome: 'Pedido', nosIds: ['a', 'b'] });
    expect(await screen.findByRole('heading', { name: 'Fluxos principais (1)' })).toBeInTheDocument();
    expect(apiClient.listarFluxos).toHaveBeenCalledTimes(2);
  });

  it('edita o fluxo: pré-preenche, envia o PUT e volta ao formulário de criação', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos)
      .mockResolvedValueOnce([fabricaFluxo('f1', 'Pedido', ['a', 'b', 'c'])])
      .mockResolvedValueOnce([fabricaFluxo('f1', 'Pedido novo', ['a', 'b', 'c'])]);
    vi.mocked(apiClient.atualizarFluxo).mockResolvedValue(fabricaFluxo('f1', 'Pedido novo', ['a', 'b', 'c']));
    renderizarCaso(CAMINHO);

    await user.click(await screen.findByRole('button', { name: 'Editar o fluxo Pedido' }));
    expect(screen.getByRole('heading', { name: 'Editar Fluxo' })).toBeInTheDocument();
    const nomeCampo = screen.getByRole('textbox', { name: 'Nome do fluxo' });
    expect(nomeCampo).toHaveValue('Pedido');
    await user.clear(nomeCampo);
    await user.type(nomeCampo, 'Pedido novo');
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(apiClient.atualizarFluxo).toHaveBeenCalledWith(ID_DO_CASO, 'f1', { nome: 'Pedido novo', nosIds: ['a', 'b', 'c'] });
    expect(await screen.findByRole('heading', { name: 'Adicionar Fluxo' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Pedido novo' })).toBeInTheDocument();
  });

  it('exclui o fluxo certo depois da confirmação em modal e recarrega a lista', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoCompleto());
    vi.mocked(apiClient.listarFluxos)
      .mockResolvedValueOnce([fabricaFluxo('f1', 'Um', ['a', 'b']), fabricaFluxo('f2', 'Dois', ['b', 'c'])])
      .mockResolvedValueOnce([fabricaFluxo('f2', 'Dois', ['b', 'c'])]);
    vi.mocked(apiClient.excluirFluxo).mockResolvedValue(undefined);
    renderizarCaso(CAMINHO);

    await user.click(await screen.findByRole('button', { name: 'Excluir Fluxo Um' }));
    const modal = screen.getByRole('dialog');
    expect(within(modal).getByText('Excluir este fluxo. Os Nós e as Arestas do Mapa não são afetados.')).toBeInTheDocument();
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(apiClient.excluirFluxo).toHaveBeenCalledWith(ID_DO_CASO, 'f1');
    expect(await screen.findByRole('heading', { name: 'Fluxos principais (1)' })).toBeInTheDocument();
  });
});
