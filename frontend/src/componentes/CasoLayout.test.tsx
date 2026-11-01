import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaNo, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

beforeEach(() => vi.clearAllMocks());

describe('CasoLayout — carregamento e erros', () => {
  it('mostra nome, domínio, contagens e as sete etapas do método', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(
      fabricaCaso({
        nome: 'Pedidos Digital',
        dominioDescricao: 'App de restaurante',
        nos: [fabricaNo('a', 'Cliente', 'Ator'), fabricaNo('b', 'Cardápio', 'Touchpoint')],
        arestas: [fabricaAresta('x', 'a', 'b')],
      }),
    );
    renderizarCaso();

    expect(await screen.findByRole('heading', { name: 'Pedidos Digital' })).toBeInTheDocument();
    expect(screen.getByText('App de restaurante')).toBeInTheDocument();
    expect(screen.getByText('2 nós · 1 aresta')).toBeInTheDocument();
    const etapas = within(screen.getByRole('navigation', { name: 'Etapas do método' })).getAllByRole('link');
    expect(etapas.map((l) => l.textContent)).toEqual(['1Nós', '2Arestas', '3Fluxos', '4Mapa', '5Métricas e Radar', '6Perfil mais próximo', '7Rastreabilidade']);
    expect(apiClient.obterCaso).toHaveBeenCalledWith(ID_DO_CASO);
  });

  // Regressão: Estudo de Caso inexistente não oferece "Tentar de novo" (continuaria 404) e não mostra mensagem técnica.
  it('404: mostra "Estudo de Caso não encontrado" com link de volta e SEM "Tentar de novo"', async () => {
    vi.mocked(apiClient.obterCaso).mockRejectedValue(new ErroDaApi(404, "EstudoDeCaso '00000000-0000-0000-0000-000000000000' não encontrado."));
    renderizarCaso();

    expect(await screen.findByText('Estudo de Caso não encontrado')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Voltar para os Estudos de Caso' })).toHaveAttribute('href', '/');
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument();
  });

  it('id que não é GUID cai em "não encontrado" sem chamar a API', async () => {
    renderizarCaso('/casos/nao-e-guid/nos');
    expect(await screen.findByText('Estudo de Caso não encontrado')).toBeInTheDocument();
    expect(apiClient.obterCaso).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument();
  });

  it('5xx: mostra o erro com "Tentar de novo", que busca de novo e carrega o caso', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockRejectedValueOnce(new ErroDaApi(500, 'Internal Server Error'))
      .mockResolvedValueOnce(fabricaCaso({ nome: 'Voltou' }));
    renderizarCaso();

    expect(await screen.findByRole('alert')).toHaveTextContent('Erro inesperado na API (500)');
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }));

    expect(await screen.findByRole('heading', { name: 'Voltou' })).toBeInTheDocument();
    expect(apiClient.obterCaso).toHaveBeenCalledTimes(2);
  });
});

describe('CasoLayout — editar e excluir o Estudo de Caso', () => {
  it('editar: salva o novo nome, recarrega o caso e volta ao cabeçalho com o nome novo', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso)
      .mockResolvedValueOnce(fabricaCaso({ nome: 'Nome antigo', dominioDescricao: 'Domínio' }))
      .mockResolvedValueOnce(fabricaCaso({ nome: 'Nome novo', dominioDescricao: 'Domínio' }));
    vi.mocked(apiClient.atualizarCaso).mockResolvedValue(fabricaCaso());
    renderizarCaso();

    await user.click(await screen.findByRole('button', { name: 'Editar Estudo de Caso' }));
    const campo = screen.getByRole('textbox', { name: 'Nome do sistema' });
    await user.clear(campo);
    await user.type(campo, 'Nome novo');
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(apiClient.atualizarCaso).toHaveBeenCalledWith(ID_DO_CASO, { nome: 'Nome novo', dominioDescricao: 'Domínio' });
    expect(await screen.findByRole('heading', { name: 'Nome novo' })).toBeInTheDocument();
    expect(apiClient.obterCaso).toHaveBeenCalledTimes(2);
    expect(screen.queryByRole('textbox', { name: 'Nome do sistema' })).not.toBeInTheDocument();
  });

  it('excluir: o modal avisa que apaga Nós e Arestas; confirmar exclui e volta para a página inicial', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso());
    vi.mocked(apiClient.excluirCaso).mockResolvedValue(undefined);
    renderizarCaso();

    await user.click(await screen.findByRole('button', { name: 'Excluir Estudo de Caso' }));
    const modal = screen.getByRole('dialog', { name: 'Confirmar exclusão de Estudo de Caso' });
    expect(within(modal).getByText(/apaga também todos os Nós e Arestas/)).toBeInTheDocument();
    await user.click(within(modal).getByRole('button', { name: 'Confirmar exclusão' }));

    expect(apiClient.excluirCaso).toHaveBeenCalledWith(ID_DO_CASO);
    expect(await screen.findByText('Página inicial')).toBeInTheDocument();
  });
});
