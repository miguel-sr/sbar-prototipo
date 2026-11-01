import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from '../servicos/apiClient';
import { fabricaCaso, fabricaMetricas, fabricaNo, fabricaPerfil, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

// O gráfico (Recharts) precisa de layout real para desenhar; aqui só interessa o que a página
// entrega a ele (eixos e perfis visíveis). O desenho em si fica para o E2E.
vi.mock('../componentes/RadarChart', () => ({
  CORES_DOS_PERFIS: ['#c00', '#0c0', '#00c', '#cc0'],
  RadarChart: ({ eixos, perfis }: { eixos: unknown[]; perfis: { nome: string }[] }) => (
    <div role="img" aria-label="Radar (teste)" data-eixos={eixos.length} data-perfis={perfis.map((p) => p.nome).join('|')} />
  ),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/radar`;
const casoComNos = () => fabricaCaso({ nos: [fabricaNo('a', 'Cliente', 'Ator')] });
const perfis = [fabricaPerfil('p1', 'Monólito Modular'), fabricaPerfil('p2', 'Microsserviços')];

beforeEach(() => vi.clearAllMocks());

describe('MetricasRadar (T6)', () => {
  it('sem Nós, mostra o estado vazio e não calcula nada', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso());
    vi.mocked(apiClient.listarPerfis).mockResolvedValue(perfis);
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Sem Mapa, sem métricas')).toBeInTheDocument();
  });

  it('mostra um cartão para cada um dos dez eixos', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas());
    vi.mocked(apiClient.listarPerfis).mockResolvedValue(perfis);
    renderizarCaso(CAMINHO);

    expect(await screen.findAllByRole('article')).toHaveLength(10);
    expect(apiClient.obterMetricas).toHaveBeenCalledWith(ID_DO_CASO);
    expect(screen.getByRole('img', { name: 'Radar (teste)' })).toHaveAttribute('data-eixos', '10');
  });

  // CT08 / RN08: eixo sem dados (ex.: UX sem Frontstage) aparece como "não aplicável", com o motivo, nunca como 0.
  it('eixo não aplicável mostra o motivo no lugar do número', async () => {
    const metricas = fabricaMetricas();
    metricas.eixos = metricas.eixos.map((e) =>
      e.eixo === 'UX' ? { eixo: 'UX', aplicavel: false, valorBruto: null, valorNormalizado: null, motivo: 'Nenhum nó na banda Frontstage' } : e,
    );
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(metricas);
    vi.mocked(apiClient.listarPerfis).mockResolvedValue(perfis);
    renderizarCaso(CAMINHO);

    expect(await screen.findByText('Não aplicável')).toBeInTheDocument();
    expect(screen.getByText('Nenhum nó na banda Frontstage')).toBeInTheDocument();
    expect(screen.getAllByRole('article')).toHaveLength(10);
  });

  it('os perfis aparecem na legenda e desmarcar um tira ele do gráfico', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas());
    vi.mocked(apiClient.listarPerfis).mockResolvedValue(perfis);
    renderizarCaso(CAMINHO);

    const radar = await screen.findByRole('img', { name: 'Radar (teste)' });
    await screen.findByRole('checkbox', { name: 'Monólito Modular' });
    expect(radar).toHaveAttribute('data-perfis', 'Monólito Modular|Microsserviços');

    await user.click(screen.getByRole('checkbox', { name: 'Monólito Modular' }));
    expect(screen.getByRole('img', { name: 'Radar (teste)' })).toHaveAttribute('data-perfis', 'Microsserviços');
    await user.click(screen.getByRole('checkbox', { name: 'Monólito Modular' }));
    expect(screen.getByRole('img', { name: 'Radar (teste)' })).toHaveAttribute('data-perfis', 'Monólito Modular|Microsserviços');
  });

  it('sem perfis cadastrados, avisa e aponta para a tela de perfis', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas());
    vi.mocked(apiClient.listarPerfis).mockResolvedValue([]);
    renderizarCaso(CAMINHO);
    expect(await screen.findByText(/Nenhum Perfil Arquitetural cadastrado/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'cadastrar' })).toHaveAttribute('href', '/perfis');
  });
});

describe('MetricasRadar (T6) — estrutura do Mapa por trás dos eixos', () => {
  const nosDoCaso = [
    fabricaNo('a', 'Paciente', 'Ator'),
    fabricaNo('tp1', 'Portal', 'Touchpoint'),
    fabricaNo('p2', 'Registro', 'Processo'),
    fabricaNo('ds', 'Repositório', 'Dados'),
  ];
  const caso = () => fabricaCaso({ nos: nosDoCaso });
  const abrir = async (metricas = fabricaMetricas()) => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(caso());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(metricas);
    vi.mocked(apiClient.listarPerfis).mockResolvedValue(perfis);
    renderizarCaso(CAMINHO);
    await screen.findAllByRole('article');
  };

  // RF06: comunidades e modularidade Q informadas ao usuário.
  it('lista as comunidades C1…Cn com os nomes dos Nós e as saídas externas, e o valor de Q', async () => {
    await abrir(
      fabricaMetricas({
        modularidadeQ: 0.4117,
        comunidades: [
          { indice: 1, nosIds: ['a', 'tp1'], saidasExternas: 2 },
          { indice: 2, nosIds: ['p2', 'ds'], saidasExternas: 1 },
        ],
      }),
    );
    const painel = screen.getByRole('region', { name: 'Comunidades estruturais' });
    expect(within(painel).getByText(/Modularidade Q =/)).toHaveTextContent('0,4117');
    const itens = within(painel).getAllByRole('listitem');
    expect(itens[0]).toHaveTextContent('C1Paciente, Portal2 saídas externas');
    expect(itens[0]).not.toHaveTextContent('autônoma');
    expect(itens[1]).toHaveTextContent('C2Registro, Repositório1 saída externa · autônoma');
  });

  // TCC 4.4.1: Q ≥ ~0,3 indica estrutura de comunidades significativa (o limite em si entra como significativa).
  it.each([
    [0.3, 'estrutura significativa'],
    [0.1746, 'estrutura pouco significativa'],
  ])('Q = %s ganha o selo "%s"', async (q, selo) => {
    await abrir(fabricaMetricas({ modularidadeQ: q, comunidades: [{ indice: 1, nosIds: ['a'], saidasExternas: 0 }] }));
    expect(screen.getByText(selo)).toBeInTheDocument();
  });

  it('Q nulo (Mapa sem arestas) aparece como indefinida, sem selo de significância', async () => {
    await abrir(fabricaMetricas({ modularidadeQ: null, comunidades: [{ indice: 1, nosIds: ['a'], saidasExternas: 0 }] }));
    expect(screen.getByText(/Modularidade Q =/)).toHaveTextContent('indefinida');
    expect(screen.queryByText(/estrutura (pouco )?significativa/)).not.toBeInTheDocument();
  });

  it('exibe os avisos de casos-limite vindos da API em destaque, acima do gráfico', async () => {
    await abrir(fabricaMetricas({ avisos: ['Comunidade única: FLEX e AUT ficam degenerados (= 1).', 'Q < 0,3: estrutura de comunidades não significativa.'] }));
    const aviso = screen.getByRole('status', { name: 'Avisos sobre o cálculo' });
    expect(within(aviso).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      'Comunidade única: FLEX e AUT ficam degenerados (= 1).',
      'Q < 0,3: estrutura de comunidades não significativa.',
    ]);
  });

  // TCC 4.6: o próximo passo do fluxo é o perfil de referência mais próximo, não uma "recomendação".
  it('o atalho para a próxima etapa se chama "Ver perfil mais próximo" e leva à aba de perfil', async () => {
    await abrir();

    const atalho = screen.getByRole('link', { name: /Ver perfil mais próximo/ });
    expect(atalho).toHaveAttribute('href', expect.stringContaining('recomendacao'));
    expect(screen.queryByRole('link', { name: /Ver recomendação/ })).not.toBeInTheDocument();
  });

  // US04: os valores de μ e σ de cada Banda são exibidos junto dos nós potencialmente críticos.
  it('mostra μ, σ e limiar de cada Banda e os Nós potencialmente críticos dela (US04)', async () => {
    await abrir(
      fabricaMetricas({
        nosGargalo: ['ds'],
        bandas: [
          { banda: 'Ator', quantidadeDeNos: 1, media: 6, desvioPadrao: 0, limiar: 6 },
          { banda: 'Suporte', quantidadeDeNos: 3, media: 7, desvioPadrao: 2.94, limiar: 9.94 },
        ],
      }),
    );
    const tabela = screen.getByRole('table');
    const linhas = within(tabela).getAllByRole('row');
    expect(linhas).toHaveLength(3); // cabeçalho + 2 Bandas
    const suporte = linhas[2];
    expect(within(suporte).getAllByRole('cell').map((c) => c.textContent)).toEqual(['Suporte', '3', '7,00', '2,94', '9,94', 'Repositório']);
    expect(within(linhas[1]).getAllByRole('cell')[5]).toHaveTextContent('nenhum');
  });

  it('resume os fluxos do DEP: válidos entram, inválidos ficam de fora e apontam para a aba Fluxos', async () => {
    await abrir(
      fabricaMetricas({
        fluxos: [
          { id: 'f1', nome: 'Agendamento', valido: true, bandasPercorridas: 4 },
          { id: 'f2', nome: 'Quebrado', valido: false, bandasPercorridas: 2 },
        ],
      }),
    );
    const painel = screen.getByRole('region', { name: 'Fluxos principais (base do DEP)' });
    expect(within(painel).getByText(/1 fluxo válido entra no cálculo\./)).toBeInTheDocument();
    expect(within(painel).getByText(/1 inválido ficou de fora/)).toBeInTheDocument();
    expect(within(painel).getByRole('link', { name: 'corrigir' })).toHaveAttribute('href', expect.stringMatching(/\/fluxos$/));
    expect(within(painel).getByText('4 bandas')).toBeInTheDocument();
  });

  it('sem fluxos cadastrados, avisa que o DEP fica não aplicável e leva à aba Fluxos', async () => {
    await abrir(fabricaMetricas({ fluxos: [] }));
    const painel = screen.getByRole('region', { name: 'Fluxos principais (base do DEP)' });
    expect(within(painel).getByText(/o eixo DEP fica “não aplicável”/)).toBeInTheDocument();
    expect(within(painel).getByRole('link', { name: 'Cadastrar fluxos' })).toBeInTheDocument();
  });
});
