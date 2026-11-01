import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { fabricaConjunto, fabricaVersao, renderizarComRota } from '../testes/auxiliares';
import { Parametros } from './Parametros';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const renderizar = () => renderizarComRota(<Parametros />, '/parametros');
const campo = (rotulo: string) => screen.getByLabelText(rotulo) as HTMLInputElement;

const assinatura = { id: 'p1', nome: 'Monólito Modular (provisório)', valores: { CO: 2, ESC: 1.5, INT: 1, UX: 2.5, DISP: 3, FLEX: 1.5, TR: 1.5, AUT: 1, VOL: 3, DEP: 3 } };

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(apiClient.obterParametrosAtuais).mockResolvedValue(fabricaConjunto(2, 'Mais peso em DISP', { assinaturas: [assinatura] }));
  vi.mocked(apiClient.listarVersoesDeParametros).mockResolvedValue([fabricaVersao(1, 'Parâmetros iniciais'), fabricaVersao(2, 'Mais peso em DISP')]);
});

describe('Parâmetros (versionados — RF11, RN07, CT10)', () => {
  it('mostra a versão vigente e uma linha por eixo com mínimo, máximo e peso', async () => {
    renderizar();
    expect(await screen.findByTestId('versao-exibida')).toHaveTextContent('Versão vigente: 2 — Mais peso em DISP');
    expect(campo('Mínimo de CO').value).toBe('6');
    expect(campo('Máximo de CO').value).toBe('40');
    expect(campo('Peso de DEP').value).toBe('0.1');
    expect(screen.getAllByLabelText(/^Peso de /)).toHaveLength(10);
    expect(screen.getByTestId('soma-dos-pesos')).toHaveTextContent('1,0000');
  });

  it('com a soma dos pesos diferente de 1, mostra o erro e BLOQUEIA o salvamento (CT10)', async () => {
    const user = userEvent.setup();
    renderizar();
    await screen.findByTestId('soma-dos-pesos');

    await user.type(screen.getByLabelText(/^O que mudou/), 'Teste');
    await user.clear(campo('Peso de DISP'));
    await user.type(campo('Peso de DISP'), '0,3');

    expect(screen.getByTestId('soma-dos-pesos')).toHaveTextContent('1,2000');
    expect(screen.getByRole('alert')).toHaveTextContent('Soma dos pesos igual a 1,2000; deve ser 1');
    expect(screen.getByRole('button', { name: 'Salvar como nova versão' })).toBeDisabled();
  });

  it('a soma em tempo real aceita pesos redistribuídos que continuam somando 1 (com vírgula decimal)', async () => {
    const user = userEvent.setup();
    renderizar();
    await screen.findByTestId('soma-dos-pesos');

    await user.type(screen.getByLabelText(/^O que mudou/), 'Mais peso em DISP');
    await user.clear(campo('Peso de DISP'));
    await user.type(campo('Peso de DISP'), '0,2');
    await user.clear(campo('Peso de CO'));
    await user.type(campo('Peso de CO'), '0,0');

    expect(screen.getByTestId('soma-dos-pesos')).toHaveTextContent('1,0000');
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar como nova versão' })).toBeEnabled();
  });

  it('sem alteração ou sem descrição, o botão de salvar fica desabilitado', async () => {
    const user = userEvent.setup();
    renderizar();
    await screen.findByTestId('soma-dos-pesos');
    const salvar = screen.getByRole('button', { name: 'Salvar como nova versão' });
    expect(salvar).toBeDisabled();

    await user.clear(campo('Máximo de UX'));
    await user.type(campo('Máximo de UX'), '4');
    expect(salvar).toBeDisabled(); // falta a descrição

    await user.type(screen.getByLabelText(/^O que mudou/), 'UX recalibrado');
    expect(salvar).toBeEnabled();
  });

  it('mínimo maior ou igual ao máximo é rejeitado com mensagem clara', async () => {
    const user = userEvent.setup();
    renderizar();
    await screen.findByTestId('soma-dos-pesos');
    await user.type(screen.getByLabelText(/^O que mudou/), 'x');
    await user.clear(campo('Mínimo de VOL'));
    await user.type(campo('Mínimo de VOL'), '3');
    expect(screen.getByRole('alert')).toHaveTextContent('Em VOL, o mínimo (3) precisa ser menor que o máximo (3)');
    expect(screen.getByRole('button', { name: 'Salvar como nova versão' })).toBeDisabled();
  });

  it('salvar envia descrição e os dez eixos como números, cria nova versão e avisa', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.atualizarLimitesEPesos).mockResolvedValue(fabricaConjunto(3, 'UX recalibrado'));
    renderizar();
    await screen.findByTestId('soma-dos-pesos');

    await user.type(screen.getByLabelText(/^O que mudou/), '  UX recalibrado  ');
    await user.clear(campo('Máximo de UX'));
    await user.type(campo('Máximo de UX'), '4,5');
    expect(screen.getByText(/Salvar cria a versão 3; a versão 2 continua intacta/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Salvar como nova versão' }));

    const [dados] = vi.mocked(apiClient.atualizarLimitesEPesos).mock.calls[0];
    expect(dados.descricao).toBe('UX recalibrado');
    expect(dados.eixos).toHaveLength(10);
    expect(dados.eixos.find((e) => e.eixo === 'UX')).toEqual({ eixo: 'UX', minimo: 0, maximo: 4.5, peso: 0.1 });
    expect(await screen.findByRole('status')).toHaveTextContent('Versão 3 criada');
  });

  it('erro 422 do servidor aparece no formulário e os valores digitados são mantidos', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.atualizarLimitesEPesos).mockRejectedValue(new ErroDaApi(422, 'Soma dos pesos igual a 1,0001; deve ser 1.'));
    renderizar();
    await screen.findByTestId('soma-dos-pesos');

    await user.type(screen.getByLabelText(/^O que mudou/), 'x');
    await user.clear(campo('Máximo de UX'));
    await user.type(campo('Máximo de UX'), '4');
    await user.click(screen.getByRole('button', { name: 'Salvar como nova versão' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Soma dos pesos igual a 1,0001; deve ser 1.');
    expect(campo('Máximo de UX').value).toBe('4');
  });

  it('histórico: lista as versões (a vigente marcada) e abre uma antiga em modo somente leitura', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.obterParametros).mockResolvedValue(fabricaConjunto(1, 'Parâmetros iniciais'));
    renderizar();
    await screen.findByTestId('versao-exibida');

    const itens = screen.getAllByRole('listitem');
    expect(itens[0]).toHaveTextContent('Versão 2');
    expect(itens[0]).toHaveTextContent('vigente');
    expect(itens[1]).toHaveTextContent('Versão 1');

    await user.click(screen.getByRole('button', { name: 'Ver a versão 1' }));

    expect(apiClient.obterParametros).toHaveBeenCalledWith(1);
    expect(await screen.findByText(/Visualizando a versão 1/)).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'Limites e pesos da versão 1' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Peso de CO')).not.toBeInTheDocument(); // sem campos editáveis
    expect(screen.queryByRole('button', { name: 'Salvar como nova versão' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Voltar à versão vigente' }));
    expect(await screen.findByTestId('versao-exibida')).toHaveTextContent('Versão vigente: 2');
    expect(screen.getByLabelText('Peso de CO')).toBeInTheDocument();
  });
});
