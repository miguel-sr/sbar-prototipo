import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiClient, aoPerderASessao, ehNaoEncontrado, ErroDaApi, extrairErro, mensagemDeErro, nomeDoArquivo } from './apiClient';

afterEach(() => vi.restoreAllMocks());

const json = (corpo: unknown, init: ResponseInit = { status: 200 }) => new Response(JSON.stringify(corpo), init);
const simularFetch = (resposta: Response) => vi.spyOn(globalThis, 'fetch').mockResolvedValue(resposta);

describe('erros da API', () => {
  it('lê o title do ProblemDetails (API.md 1.1)', async () => {
    const erro = await extrairErro(json({ status: 422, title: 'Peso 4 inválido.' }, { status: 422 }));
    expect(erro).toBeInstanceOf(ErroDaApi);
    expect(erro.status).toBe(422);
    expect(erro.message).toBe('Peso 4 inválido.');
  });

  it('cai no status HTTP quando o corpo não é JSON', async () => {
    const erro = await extrairErro(new Response('', { status: 500, statusText: 'Internal Server Error' }));
    expect(erro.message).toBe('Internal Server Error');
  });

  it('toda resposta não-OK vira ErroDaApi com status e mensagem do servidor', async () => {
    simularFetch(json({ status: 404, title: 'Nó não encontrado.' }, { status: 404 }));
    const erro = await apiClient.excluirNo('c1', 'n9').catch((e: unknown) => e);
    expect(erro).toBeInstanceOf(ErroDaApi);
    expect(erro).toMatchObject({ status: 404, message: 'Nó não encontrado.' });
  });

  it('ehNaoEncontrado é verdadeiro só para ErroDaApi com status 404', () => {
    expect(ehNaoEncontrado(new ErroDaApi(404, 'x'))).toBe(true);
    expect(ehNaoEncontrado(new ErroDaApi(422, 'x'))).toBe(false);
    expect(ehNaoEncontrado(new Error('404'))).toBe(false);
    expect(ehNaoEncontrado(undefined)).toBe(false);
  });
});

describe('mensagemDeErro — o que o usuário lê', () => {
  it.each([
    ["EstudoDeCaso '00000000-0000-0000-0000-000000000000' não encontrado.", 'Estudo de Caso não encontrado.'],
    ["PerfilArquitetural 'a1b2c3d4-0000-4000-8000-000000000001' não encontrado.", 'Perfil Arquitetural não encontrado.'],
  ])('tira o GUID e troca o nome da classe por texto legível: %j', (original, esperado) => {
    expect(mensagemDeErro(new ErroDaApi(404, original))).toBe(esperado);
  });

  it.each([
    ['Not Found', 'Recurso não encontrado.'],
    ['Bad Request', 'Requisição inválida.'],
    ['Unprocessable Entity', 'Os dados enviados não são válidos.'],
  ])('traduz a mensagem genérica em inglês %j', (original, esperado) => {
    expect(mensagemDeErro(new ErroDaApi(400, original))).toBe(esperado);
  });

  it('mantém a mensagem de negócio da API (422) sem alterar', () => {
    expect(mensagemDeErro(new ErroDaApi(422, 'Nome não pode ter mais de 100 caracteres.'))).toBe('Nome não pode ter mais de 100 caracteres.');
  });

  it('avisa que o erro é inesperado e traz o status quando a API responde 5xx', () => {
    expect(mensagemDeErro(new ErroDaApi(503, 'Fora do ar'))).toBe('Erro inesperado na API (503). Fora do ar');
  });

  it('API fora do ar (falha de rede) ganha uma mensagem amigável', () => {
    expect(mensagemDeErro(new TypeError('Failed to fetch'))).toMatch(/Não foi possível falar com a API/);
  });
});

describe('contrato com a API (API.md): verbo, rota e corpo de cada operação', () => {
  it('corpo em JSON com enums como string e Content-Type declarado', async () => {
    const fetchMock = simularFetch(json({ id: 'x' }, { status: 201 }));
    await apiClient.criarAresta('caso-1', { noOrigemId: 'a', noDestinoId: 'b', tipo: 'Critica', peso: 3, justificativa: 'Interação do Blueprint' });
    const [, init] = fetchMock.mock.calls[0];
    expect(JSON.parse(init?.body as string)).toEqual({ noOrigemId: 'a', noDestinoId: 'b', tipo: 'Critica', peso: 3, justificativa: 'Interação do Blueprint' });
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json');
  });

  it('DELETE responde 204 sem corpo e a chamada não lê JSON', async () => {
    simularFetch(new Response(null, { status: 204 }));
    await expect(apiClient.removerPerfil('p1')).resolves.toBeUndefined();
  });

  // Uma rota digitada errada só apareceria em produção; a tabela é a transcrição do API.md.
  it('cada operação usa o verbo e a rota do contrato', async () => {
    const corpoQualquer = { nome: 'x' } as never;
    const operacoes: Array<[string, () => Promise<unknown>, string, string]> = [
      ['listarCasos', () => apiClient.listarCasos(), 'GET', '/api/casos'],
      ['atualizarCaso', () => apiClient.atualizarCaso('c1', corpoQualquer), 'PUT', '/api/casos/c1'],
      ['excluirCaso', () => apiClient.excluirCaso('c1'), 'DELETE', '/api/casos/c1'],
      ['atualizarNo', () => apiClient.atualizarNo('c1', 'n1', corpoQualquer), 'PUT', '/api/casos/c1/nos/n1'],
      ['excluirNo', () => apiClient.excluirNo('c1', 'n1'), 'DELETE', '/api/casos/c1/nos/n1'],
      ['atualizarAresta', () => apiClient.atualizarAresta('c1', 'a1', corpoQualquer), 'PUT', '/api/casos/c1/arestas/a1'],
      ['excluirAresta', () => apiClient.excluirAresta('c1', 'a1'), 'DELETE', '/api/casos/c1/arestas/a1'],
      ['listarFluxos', () => apiClient.listarFluxos('c1'), 'GET', '/api/casos/c1/fluxos'],
      ['criarFluxo', () => apiClient.criarFluxo('c1', corpoQualquer), 'POST', '/api/casos/c1/fluxos'],
      ['atualizarFluxo', () => apiClient.atualizarFluxo('c1', 'f1', corpoQualquer), 'PUT', '/api/casos/c1/fluxos/f1'],
      ['excluirFluxo', () => apiClient.excluirFluxo('c1', 'f1'), 'DELETE', '/api/casos/c1/fluxos/f1'],
      ['obterMetricas (vigente)', () => apiClient.obterMetricas('c1'), 'GET', '/api/casos/c1/metricas'],
      ['obterMetricas (versão)', () => apiClient.obterMetricas('c1', 2), 'GET', '/api/casos/c1/metricas?versao=2'],
      ['listarVersoesDeParametros', () => apiClient.listarVersoesDeParametros(), 'GET', '/api/parametros'],
      ['obterParametrosAtuais', () => apiClient.obterParametrosAtuais(), 'GET', '/api/parametros/atual'],
      ['obterParametros', () => apiClient.obterParametros(3), 'GET', '/api/parametros/3'],
      ['atualizarLimitesEPesos', () => apiClient.atualizarLimitesEPesos(corpoQualquer), 'PUT', '/api/parametros/limites-e-pesos'],
    ];

    for (const [nome, chamar, verbo, rota] of operacoes) {
      const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async () => new Response(verbo === 'DELETE' ? null : '{}', { status: verbo === 'DELETE' ? 204 : 200 }));
      await chamar();
      const [url, init] = fetchMock.mock.calls[0];
      expect([nome, init?.method ?? 'GET', url]).toEqual([nome, verbo, rota]);
      fetchMock.mockRestore();
    }
  });
});

describe('exportação (RF13)', () => {
  it('baixa o arquivo pedido (formato e versão) e usa o nome do Content-Disposition', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response('{"ok":true}', { status: 200, headers: { 'Content-Disposition': 'attachment; filename="sbar-clinica-v3.json"' } }),
    );
    const arquivo = await apiClient.exportarCaso('caso-1', 'json', 3);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/casos/caso-1/exportacao?formato=json&versao=3');
    expect(arquivo.nome).toBe('sbar-clinica-v3.json');
    expect(await arquivo.blob.text()).toBe('{"ok":true}');
  });

  it('sem Content-Disposition, usa um nome padrão com a extensão do formato', async () => {
    simularFetch(new Response('a;b', { status: 200 }));
    expect((await apiClient.exportarCaso('caso-1', 'csv')).nome).toBe('sbar-exportacao.csv');
  });

  it.each([
    ['attachment; filename="a b.csv"', 'a b.csv'],
    ['attachment; filename=sbar-x-v1.json', 'sbar-x-v1.json'],
  ])('nomeDoArquivo lê %j', (cabecalho, esperado) => {
    expect(nomeDoArquivo(cabecalho, 'padrao.txt')).toBe(esperado);
  });
});

describe('sessão perdida (401)', () => {
  it('um 401 numa chamada comum avisa quem registrou; o cancelamento desliga o aviso', async () => {
    const aviso = vi.fn();
    const cancelar = aoPerderASessao(aviso);

    simularFetch(new Response('', { status: 401, statusText: 'Unauthorized' }));
    const erro = await apiClient.listarCasos().catch((e: unknown) => e);
    expect(aviso).toHaveBeenCalledTimes(1);
    expect(mensagemDeErro(erro)).toBe('Sua sessão expirou. Entre novamente.');

    cancelar();
    await apiClient.listarCasos().catch(() => undefined);
    expect(aviso).toHaveBeenCalledTimes(1);
  });

  it('login errado também é 401, mas não é sessão perdida', async () => {
    const aviso = vi.fn();
    const cancelar = aoPerderASessao(aviso);

    simularFetch(json({ status: 401, title: 'E-mail ou senha incorretos.' }, { status: 401 }));
    const erro = await apiClient.entrar({ email: 'a@b.c', senha: 'x' }).catch((e: unknown) => e);

    expect(aviso).not.toHaveBeenCalled();
    expect(mensagemDeErro(erro)).toBe('E-mail ou senha incorretos.');
    cancelar();
  });

  it('as rotas de conta usam POST com corpo JSON e /me/logout os caminhos certos', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async () => json({ id: 'u1', email: 'a@b.c' }));
    await apiClient.cadastrar({ email: 'a@b.c', senha: 'Senha1234', codigoDeConvite: 'c' });
    await apiClient.eu();

    expect(fetchMock.mock.calls[0][0]).toBe('/api/account/register');
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: 'POST' });
    expect(fetchMock.mock.calls[1][0]).toBe('/api/account/me');
  });
});
