import { screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaNo, renderizarComRota } from '../testes/auxiliares';
import { ListaDeCasos } from './ListaDeCasos';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const renderizar = () => renderizarComRota(<ListaDeCasos />, '/');

beforeEach(() => vi.clearAllMocks());

describe('ListaDeCasos (T1)', () => {

  it('um card por Estudo de Caso, com nome, descrição, contagem de nós/arestas e link para o caso', async () => {
    vi.mocked(apiClient.listarCasos).mockResolvedValue([
      fabricaCaso({
        id: 'c1',
        nome: 'Pedidos Digital',
        dominioDescricao: 'App de restaurante',
        nos: [fabricaNo('a', 'Cliente', 'Ator'), fabricaNo('b', 'Cardápio', 'Touchpoint'), fabricaNo('c', 'Checkout', 'Touchpoint')],
        arestas: [fabricaAresta('x1', 'a', 'b'), fabricaAresta('x2', 'b', 'c')],
      }),
      fabricaCaso({ id: 'c2', nome: 'Clínica' }),
    ]);
    renderizar();

    const card = (await screen.findByRole('heading', { name: 'Pedidos Digital' })).closest('a')!;
    expect(card).toHaveAttribute('href', '/casos/c1');
    expect(within(card).getByText('App de restaurante')).toBeInTheDocument();
    expect(within(card).getByText('3 nós · 2 arestas')).toBeInTheDocument();
  });

  // TCC 4.6: a saída do método é o perfil de referência mais próximo (subsídio), não uma "recomendação preliminar".
  it('a apresentação usa "perfil de referência mais próximo", sem o vocabulário antigo de recomendação', async () => {
    vi.mocked(apiClient.listarCasos).mockResolvedValue([]);
    renderizar();

    const texto = (await screen.findByRole('heading', { name: 'Estudos de Caso', level: 1 })).parentElement!.textContent ?? '';
    expect(texto).toMatch(/perfil de referência mais próximo/);
    expect(texto).not.toMatch(/recomendação/i);
  });

  it('caso sem descrição mostra o texto padrão e caso vazio mostra 0 nós · 0 arestas', async () => {
    vi.mocked(apiClient.listarCasos).mockResolvedValue([fabricaCaso({ id: 'c2', nome: 'Clínica', dominioDescricao: null })]);
    renderizar();
    const card = (await screen.findByRole('heading', { name: 'Clínica' })).closest('a')!;
    expect(within(card).getByText('Sem descrição de domínio.')).toBeInTheDocument();
    expect(within(card).getByText('0 nós · 0 arestas')).toBeInTheDocument();
  });

  it('sem casos, mostra o estado vazio com o atalho para criar o primeiro', async () => {
    vi.mocked(apiClient.listarCasos).mockResolvedValue([]);
    renderizar();
    expect(await screen.findByText('Nenhum Estudo de Caso ainda')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Criar o primeiro Estudo de Caso' })).toHaveAttribute('href', '/casos/novo');
  });
});
