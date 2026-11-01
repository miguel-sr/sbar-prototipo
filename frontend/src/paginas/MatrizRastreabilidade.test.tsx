import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { salvarComoArquivo } from '../dominio/download';
import type { LinhaDeRastreabilidade } from '../tipos/Rastreabilidade';
import { fabricaCaso, fabricaLinhaDaMatriz, fabricaNo, fabricaVersao, ID_DO_CASO, renderizarCaso } from '../testes/auxiliares';

vi.mock('../dominio/download', () => ({ salvarComoArquivo: vi.fn() }));
vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const CAMINHO = `/casos/${ID_DO_CASO}/rastreabilidade`;
const casoComNos = () => fabricaCaso({ nos: [fabricaNo('a', 'Cliente', 'Ator')] });

const gargalo = fabricaLinhaDaMatriz('Registro do Agendamento (gargalo)', 'gargalo', ['DISP'], {
  elemento: 'Registro do Agendamento (Backstage)',
  metrica: 'carga(v), gargalo(v)',
  contribuicao: 'Carga 9 acima do limiar da Banda Backstage (μ+σ = 7,73); reduz a Disponibilidade calculada.',
});
const gargaloFrontstage = fabricaLinhaDaMatriz('Seleção de Horário (gargalo)', 'gargalo', ['UX'], {
  elemento: 'Seleção de Horário (Frontstage)',
  contribuicao: 'Carga 9 acima do limiar; não entra no DISP (Frontstage), mas concentra interações de entrada (UX).',
});
const gargaloDeAtor = fabricaLinhaDaMatriz('Paciente (gargalo)', 'gargalo', [], {
  elemento: 'Paciente (Ator)',
  contribuicao: 'Fora do DISP (Banda Ator).',
});
const critica = fabricaLinhaDaMatriz('Paciente → Check-in (crítica)', 'aresta-critica', ['TR'], {
  elemento: 'Paciente → Check-in',
  contribuicao: 'Aresta crítica de peso 3; contribui para o numerador de TR (Tempo Real).',
});
const externaAssincrona = fabricaLinhaDaMatriz('Notificações → Gateway (externa)', 'aresta-externa', ['INT'], {
  elemento: 'Notificações → Gateway',
  contribuicao: 'Aresta Assíncrona (peso 2) conectada à Banda Externo; contribui para o numerador de INT (Integração).',
});
const externaBatch: LinhaDeRastreabilidade = { ...externaAssincrona, contribuicao: externaAssincrona.contribuicao.replace('Assíncrona (peso 2)', 'Batch (peso 1)') };
const comunidade = fabricaLinhaDaMatriz('Comunidade C3 (TP4, P4, S2, E1)', 'comunidade', ['FLEX', 'AUT'], {
  elemento: 'Confirmação e lembrete',
  metrica: 'isolamento(v); saídas_externas(c)',
  contribuicao: 'Comunidade com 1 saída externa (autônoma); isolamento médio 0,625.',
});
const fluxo = fabricaLinhaDaMatriz('F1 – Agendamento', 'fluxo', ['DEP'], { elemento: 'Agendamento (fluxo principal)', contribuicao: 'Fluxo percorre 4 bandas.' });

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(apiClient.listarVersoesDeParametros).mockResolvedValue([fabricaVersao(1), fabricaVersao(3, 'Pesos recalibrados')]);
});

const abrirLista = async (user: ReturnType<typeof userEvent.setup>) => {
  await user.click(await screen.findByRole('button', { name: 'Lista' }));
  return screen.getAllByRole('row').slice(1);
};

describe('MatrizRastreabilidade (T8)', () => {
  // US07: cada eixo lista os elementos que contribuíram para o seu valor.
  it('padrão "Por eixo": cada eixo lista os elementos que contribuíram (US07)', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([gargalo, gargaloFrontstage, critica, comunidade, fluxo]);
    renderizarCaso(CAMINHO);

    const disp = await screen.findByRole('region', { name: 'Eixo DISP' });
    expect(within(disp).getByText('Registro do Agendamento')).toBeInTheDocument();
    expect(within(disp).queryByText('Seleção de Horário')).not.toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Eixo UX' })).getByText('Seleção de Horário')).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Eixo TR' })).getByText('Aresta crítica')).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Eixo DEP' })).getByText('F1 – Agendamento')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Por eixo' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('uma linha com vários eixos (comunidade → FLEX e AUT) aparece em cada um deles', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([comunidade]);
    renderizarCaso(CAMINHO);

    const flex = await screen.findByRole('region', { name: 'Eixo FLEX' });
    const aut = screen.getByRole('region', { name: 'Eixo AUT' });
    for (const secao of [flex, aut]) {
      expect(within(secao).getByText('Comunidade C3 (TP4, P4, S2, E1)')).toBeInTheDocument();
      expect(within(secao).getByText('Comunidade')).toBeInTheDocument();
    }
  });

  it('elementos sem eixo (gargalo de Ator/Externo) ficam numa seção "fora do cálculo dos eixos"', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([gargalo, gargaloDeAtor]);
    renderizarCaso(CAMINHO);

    const fora = await screen.findByRole('region', { name: 'Fora do cálculo dos eixos' });
    expect(within(fora).getByText('Paciente')).toBeInTheDocument();
    expect(within(fora).queryByText('Registro do Agendamento')).not.toBeInTheDocument();
  });

  // Regressão: arestas paralelas entre o mesmo par de nós precisam ser distinguíveis (tipo e peso).
  it('arestas paralelas ficam distinguíveis pelo tipo e peso na contribuição', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([externaAssincrona, externaBatch]);
    renderizarCaso(CAMINHO);

    const linhas = await abrirLista(user);
    expect(linhas).toHaveLength(2);
    expect(within(linhas[0]).getByText(/Aresta Assíncrona \(peso 2\)/)).toBeInTheDocument();
    expect(within(linhas[1]).getByText(/Aresta Batch \(peso 1\)/)).toBeInTheDocument();
  });

  it('o filtro por categoria vale nos dois modos e "Todas as categorias" volta ao conjunto completo', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([gargalo, critica, externaAssincrona, comunidade]);
    renderizarCaso(CAMINHO);

    const filtros = await screen.findByRole('group', { name: 'Filtrar por categoria' });
    expect(within(filtros).getByRole('button', { name: 'Todas as categorias (4)' })).toBeInTheDocument();

    await user.click(within(filtros).getByRole('button', { name: 'Comunidade' }));
    expect(screen.getByRole('region', { name: 'Eixo FLEX' })).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Eixo DISP' })).not.toBeInTheDocument();

    expect(await abrirLista(user)).toHaveLength(1);

    await user.click(within(filtros).getByRole('button', { name: 'Todas as categorias (4)' }));
    expect(screen.getAllByRole('row').slice(1)).toHaveLength(4);
  });

  it('mostra a versão vigente dos parâmetros', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([critica]);
    renderizarCaso(CAMINHO);
    expect(await screen.findByTestId('versao-dos-parametros')).toHaveTextContent('Parâmetros vigentes: versão 3');
  });

  describe('exportação (RF13)', () => {
    it.each([
      ['JSON', 'json', 'sbar-clinica-v3.json'],
      ['CSV', 'csv', 'sbar-clinica-v3.csv'],
    ])('Exportar %s pede esse formato à API e entrega o arquivo com o nome informado pelo servidor', async (rotulo, formato, nome) => {
      const user = userEvent.setup();
      const blob = new Blob(['conteudo']);
      vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
      vi.mocked(apiClient.obterMatriz).mockResolvedValue([critica]);
      vi.mocked(apiClient.exportarCaso).mockResolvedValue({ blob, nome });
      renderizarCaso(CAMINHO);

      await user.click(await screen.findByRole('button', { name: `Exportar ${rotulo}` }));

      expect(apiClient.exportarCaso).toHaveBeenCalledWith(ID_DO_CASO, formato);
      expect(salvarComoArquivo).toHaveBeenCalledWith(blob, nome);
    });

    it('falha na exportação: mostra o erro, não baixa nada e libera os botões de novo', async () => {
      const user = userEvent.setup();
      vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
      vi.mocked(apiClient.obterMatriz).mockResolvedValue([critica]);
      vi.mocked(apiClient.exportarCaso).mockRejectedValue(new ErroDaApi(500, 'x'));
      renderizarCaso(CAMINHO);

      await user.click(await screen.findByRole('button', { name: 'Exportar JSON' }));

      expect(await screen.findByRole('alert')).toHaveTextContent('Erro inesperado na API (500)');
      expect(salvarComoArquivo).not.toHaveBeenCalled();
      expect(screen.getByRole('button', { name: 'Exportar JSON' })).toBeEnabled();
    });
  });

  it('matriz vazia mostra o estado vazio com link para o Mapa', async () => {
    vi.mocked(apiClient.obterCaso).mockResolvedValue(casoComNos());
    vi.mocked(apiClient.obterMatriz).mockResolvedValue([]);
    renderizarCaso(CAMINHO);
    expect(await screen.findByText('Nenhum elemento relevante ainda')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ver o Mapa' })).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});
