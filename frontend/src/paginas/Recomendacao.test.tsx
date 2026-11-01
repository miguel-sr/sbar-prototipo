import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import {
  fabricaCaso,
  fabricaDistancia,
  fabricaMetricas,
  fabricaNo,
  fabricaRecomendacao,
  fabricaVersao,
  ID_DO_CASO,
  renderizarCaso,
} from '../testes/auxiliares';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/recomendacao`;
const casoComNos = () => fabricaCaso({ nos: [fabricaNo('a', 'Cliente', 'Ator')] });

// CE1 do TCC (Quadros 26/27): Monólito 1,051 (soma 1,1050) × Microsserviços 1,605 (soma 2,5747).
const monolito = () => fabricaDistancia('p1', 'Monólito Modular (provisório)', 1.0512, { CO: 0.0631, ESC: 0.3063, INT: 0.0002, AUT: 0.225 });
const micro = () => fabricaDistancia('p2', 'Microsserviços (provisório)', 1.6046, { CO: 0.1454, ESC: 0.0563, INT: 0.649, AUT: 0.4 });

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(apiClient.listarVersoesDeParametros).mockResolvedValue([fabricaVersao(1, 'Parâmetros iniciais')]);
});

describe('Perfil de referência mais próximo (T7)', () => {
  it('sem Nós, mostra o estado vazio', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(fabricaCaso());
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Sem Mapa, sem perfil mais próximo')).toBeInTheDocument();
  });

  // RN09: o resultado é apresentado como perfil de referência mais próximo, com aviso de que não substitui o arquiteto.
  it('mostra o perfil mais próximo como SUBSÍDIO (RN09), não como decisão', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      // fora de ordem de propósito: a tela ordena pela menor distância
      fabricaMetricas({ recomendacao: fabricaRecomendacao('Monólito Modular (provisório)', [micro(), monolito()]) }),
    );
    renderizarCaso(CAMINHO);

    expect(await screen.findByRole('heading', { name: 'Monólito Modular (provisório)', level: 2 })).toBeInTheDocument();
    expect(screen.getByText('Perfil de referência mais próximo')).toBeInTheDocument();
    expect(screen.queryByText(/Recomendação preliminar/)).not.toBeInTheDocument();
    expect(screen.getByText(/não substitui a análise do arquiteto/)).toBeInTheDocument();
    expect(screen.getByText(/assinaturas de referência são provisórias/)).toBeInTheDocument();
  });

  // RNF03: distâncias com 3 casas decimais.
  it('lista as distâncias com 3 casas e o percentual do máximo (5), da menor para a maior', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({ recomendacao: fabricaRecomendacao('Monólito Modular (provisório)', [micro(), monolito()]) }),
    );
    renderizarCaso(CAMINHO);

    const itens = within(await screen.findByRole('list')).getAllByRole('listitem');
    expect(itens).toHaveLength(2);
    expect(itens[0]).toHaveTextContent('Monólito Modular (provisório)');
    expect(itens[0]).toHaveTextContent('1,051');
    expect(itens[0]).toHaveTextContent('21,0% do máximo');
    expect(itens[1]).toHaveTextContent('Microsserviços (provisório)');
    expect(itens[1]).toHaveTextContent('1,605');
    expect(itens[1]).toHaveTextContent('32,1% do máximo');
  });

  it('mostra a margem entre as duas menores distâncias, em valor e em % da escala', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({ recomendacao: fabricaRecomendacao('Monólito Modular (provisório)', [monolito(), micro()]) }),
    );
    renderizarCaso(CAMINHO);

    const margem = await screen.findByTestId('margem');
    expect(margem).toHaveTextContent('Margem entre as duas menores distâncias: 0,553');
    expect(margem).toHaveTextContent('11,1% da escala');
    expect(margem).toHaveTextContent('mais intermediário');
  });

  it('com um único perfil, não inventa margem nem "segundo colocado"', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({ recomendacao: fabricaRecomendacao('Monólito Modular', [fabricaDistancia('p1', 'Monólito Modular', 1)]) }),
    );
    renderizarCaso(CAMINHO);
    await screen.findByRole('heading', { name: 'Monólito Modular', level: 2 });
    expect(screen.queryByTestId('margem')).not.toBeInTheDocument();
    expect(screen.queryByText(/contra/)).not.toBeInTheDocument();
  });

  it('tabela de contribuição por eixo: valor do serviço, contribuição de cada perfil, soma e d = √soma', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({
        recomendacao: fabricaRecomendacao('Monólito Modular (provisório)', [micro(), monolito()]),
        eixos: [
          { eixo: 'CO', aplicavel: true, valorBruto: 25, valorNormalizado: 2.7941, motivo: null },
          { eixo: 'ESC', aplicavel: true, valorBruto: 0.65, valorNormalizado: 3.25, motivo: null },
          { eixo: 'INT', aplicavel: true, valorBruto: 0.19, valorNormalizado: 0.9524, motivo: null },
          { eixo: 'AUT', aplicavel: true, valorBruto: 0.5, valorNormalizado: 2.5, motivo: null },
        ],
      }),
    );
    renderizarCaso(CAMINHO);

    const tabela = await screen.findByRole('table', { name: 'Contribuição de cada eixo para a distância' });
    const cabecalho = within(tabela).getAllByRole('columnheader').map((c) => c.textContent);
    // ordem das colunas de perfil = ordem das distâncias (menor primeiro)
    expect(cabecalho).toEqual(['Eixo', 'Serviço', 'Monólito Modular (provisório)', 'Microsserviços (provisório)']);

    const linhaEsc = within(tabela).getByText('Escalabilidade').closest('tr') as HTMLElement;
    expect(within(linhaEsc).getAllByRole('cell').map((c) => c.textContent)).toEqual([expect.stringContaining('ESC'), '3,25', '0,3063', '0,0563']);

    const rodape = tabela.querySelector('tfoot') as HTMLElement;
    const [soma, raiz] = within(rodape).getAllByRole('row');
    // soma das contribuições informadas (fixture parcial): 0,0631+0,3063+0,0002+0,225 e 0,1454+0,0563+0,649+0,4
    expect(soma).toHaveTextContent('0,5946');
    expect(soma).toHaveTextContent('1,2507');
    expect(raiz).toHaveTextContent('1,051');
    expect(raiz).toHaveTextContent('1,605');
  });

  it('escolher uma versão antiga reaplica os parâmetros (?versao=) e avisa que é reaplicação; voltar à vigente a limpa', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.listarVersoesDeParametros).mockResolvedValue([fabricaVersao(2, 'Atual'), fabricaVersao(1, 'Inicial')]);
    vi.mocked(apiClient.obterMetricas).mockImplementation(async (_id, versao) =>
      fabricaMetricas({
        versaoDosParametros: versao === 1 ? fabricaVersao(1, 'Inicial') : fabricaVersao(2, 'Atual'),
        recomendacao: fabricaRecomendacao(versao === 1 ? 'Microsserviços' : 'Monólito', [fabricaDistancia('p', 'X', 1)]),
      }),
    );
    renderizarCaso(CAMINHO);

    await screen.findByRole('heading', { name: 'Monólito', level: 2 });
    await user.selectOptions(screen.getByLabelText('Versão dos parâmetros'), '1');

    expect(await screen.findByRole('heading', { name: 'Microsserviços', level: 2 })).toBeInTheDocument();
    expect(apiClient.obterMetricas).toHaveBeenLastCalledWith(ID_DO_CASO, 1);
    expect(screen.getByText(/Reaplicando a versão 1/)).toHaveTextContent('a versão vigente é a 2');

    await user.selectOptions(screen.getByLabelText('Versão dos parâmetros'), '2');
    expect(await screen.findByRole('heading', { name: 'Monólito', level: 2 })).toBeInTheDocument();
    expect(apiClient.obterMetricas).toHaveBeenLastCalledWith(ID_DO_CASO, undefined);
    expect(screen.queryByText(/Reaplicando/)).not.toBeInTheDocument();
  });

  it('se a lista de versões falhar, a tela continua funcionando sem o seletor', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.listarVersoesDeParametros).mockRejectedValue(new ErroDaApi(500, 'x'));
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(
      fabricaMetricas({ recomendacao: fabricaRecomendacao('X', [fabricaDistancia('p', 'X', 1)]) }),
    );
    renderizarCaso(CAMINHO);
    await screen.findByRole('heading', { name: 'X', level: 2 });
    expect(screen.queryByLabelText('Versão dos parâmetros')).not.toBeInTheDocument();
  });

  it('recomendação null (sem perfis cadastrados) explica e aponta para a tela de perfis', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockResolvedValue(fabricaMetricas({ recomendacao: null }));
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Nenhum Perfil Arquitetural para comparar')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Cadastre pelo menos um perfil' })).toHaveAttribute('href', '/perfis');
  });

  it('falha ao calcular (ex.: soma dos pesos ≠ 1, 422): mostra o erro da API', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMetricas).mockRejectedValue(new ErroDaApi(422, 'Soma dos pesos igual a 0,9000; deve ser 1.'));
    renderizarCaso(CAMINHO);
    expect(await screen.findByRole('alert')).toHaveTextContent('Soma dos pesos igual a 0,9000; deve ser 1.');
  });
});
