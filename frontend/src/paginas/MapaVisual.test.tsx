import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { fabricaAresta, fabricaCaso, fabricaMetricas, fabricaNo, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/mapa`;
const paciente = fabricaNo('a', 'Paciente', 'Ator');
const selecao = fabricaNo('tp2', 'Seleção de Horário', 'Touchpoint'); // Frontstage
const registro = fabricaNo('p2', 'Registro do Agendamento', 'Processo'); // Backstage
const repositorio = fabricaNo('ds', 'Repositório de Agendas', 'Dados'); // Suporte
const arestas = [fabricaAresta('1', 'a', 'tp2'), fabricaAresta('2', 'tp2', 'p2'), fabricaAresta('3', 'p2', 'ds')];
const caso = () => fabricaCaso({ nos: [paciente, selecao, registro, repositorio], arestas });

const noDoMapa = (nome: RegExp | string) => screen.getByRole('group', { name: nome });

beforeEach(() => vi.clearAllMocks());

describe('MapaVisual (T5)', () => {
  it('sem Nós, mostra o estado vazio com o caminho para cadastrar e não pede métricas', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso());
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('O Mapa ainda está vazio')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Cadastre os Nós' })).toBeInTheDocument();
  });

  // Regressão: um gargalo fora de Backstage/Suporte (ex.: Frontstage) também precisa ser destacado no Mapa.
  it('destaca os Nós de nosGargalo — inclusive um Nó de Frontstage — e só eles', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas({ nosGargalo: ['tp2', 'p2'] }));
    renderizarCaso(CAMINHO);

    await screen.findByText(/Gargalos \(carga acima de μ \+ σ da própria Banda\)/);
    expect(noDoMapa(/Seleção de Horário/)).toHaveAttribute('data-gargalo', 'true');
    expect(noDoMapa(/Registro do Agendamento/)).toHaveAttribute('data-gargalo', 'true');
    expect(noDoMapa(/Paciente/)).toHaveAttribute('data-gargalo', 'false');
    expect(noDoMapa(/Repositório de Agendas/)).toHaveAttribute('data-gargalo', 'false');
  });

  it('a nota lista os nomes dos gargalos', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas({ nosGargalo: ['tp2', 'ds'] }));
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Gargalos (carga acima de μ + σ da própria Banda): Seleção de Horário, Repositório de Agendas.')).toBeInTheDocument();
  });

  it('desenha o Mapa mesmo quando as métricas falham, avisa e permite tentar de novo', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas)
      .mockRejectedValueOnce(new ErroDaApi(500, 'Internal Server Error'))
      .mockResolvedValueOnce(fabricaMetricas({ nosGargalo: ['p2'] }));
    renderizarCaso(CAMINHO);

    expect(await screen.findByRole('alert')).toHaveTextContent('Erro inesperado na API (500)');
    expect(screen.getByRole('img', { name: 'Mapa de Interações com 4 nós e 3 arestas' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText(/Registro do Agendamento\./)).toBeInTheDocument();
  });

  it('"Mostrar comunidades" acrescenta o selo de cada Nó ao rótulo acessível e a Q à nota; desligado, o Mapa não muda', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({
        modularidadeQ: 0.3440,
        comunidades: [
          { indice: 1, nosIds: ['a', 'tp2'], saidasExternas: 1 },
          { indice: 2, nosIds: ['p2', 'ds'], saidasExternas: 0 },
        ],
      }),
    );
    renderizarCaso(CAMINHO);

    const alternador = await screen.findByRole('checkbox', { name: 'Mostrar comunidades (C1…C2)' });
    expect(alternador).not.toBeChecked();
    expect(noDoMapa('Paciente, Ator, Banda Ator')).toBeInTheDocument();
    expect(screen.queryByText(/Modularidade Q/)).not.toBeInTheDocument();

    await user.click(alternador);
    expect(noDoMapa('Paciente, Ator, Banda Ator, comunidade 1')).toBeInTheDocument();
    expect(noDoMapa('Registro do Agendamento, Processo, Banda Backstage, comunidade 2')).toBeInTheDocument();
    expect(screen.getByText(/Modularidade Q = 0,3440\./)).toBeInTheDocument();

    await user.click(alternador);
    expect(noDoMapa('Paciente, Ator, Banda Ator')).toBeInTheDocument();
  });

  it('o rótulo do Nó informa gargalo e comunidade juntos', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({ nosGargalo: ['tp2'], comunidades: [{ indice: 3, nosIds: ['tp2'], saidasExternas: 2 }] }),
    );
    renderizarCaso(CAMINHO);
    await user.click(await screen.findByRole('checkbox', { name: /Mostrar comunidades/ }));
    expect(noDoMapa('Seleção de Horário, Touchpoint, Banda Frontstage, gargalo, comunidade 3')).toBeInTheDocument();
    // Nó fora da lista de comunidades fica sem selo.
    expect(noDoMapa('Paciente, Ator, Banda Ator')).toBeInTheDocument();
  });
});
