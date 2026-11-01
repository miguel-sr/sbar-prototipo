import type { APIRequestContext } from '@playwright/test';
import type { Aresta, CriarArestaRequest } from '../../src/tipos/Aresta';
import type { EstudoDeCaso } from '../../src/tipos/EstudoDeCaso';
import type { CriarFluxoRequest, Fluxo } from '../../src/tipos/Fluxo';
import type { CriarNoRequest, No } from '../../src/tipos/No';

// API do ambiente E2E (playwright.config.ts: porta 5195, banco `sbar_e2e`). Os testes usam a API
// só para MONTAR dados (setup) e limpar; o que está sob teste é sempre a interface.
export const URL_API = 'http://localhost:5195';

async function exigirOk<T>(resposta: Awaited<ReturnType<APIRequestContext['get']>>, o_que: string): Promise<T> {
  if (!resposta.ok()) throw new Error(`${o_que} falhou: HTTP ${resposta.status()} ${await resposta.text()}`);
  return resposta.status() === 204 ? (undefined as T) : ((await resposta.json()) as T);
}

export async function criarCaso(request: APIRequestContext, nome: string, dominioDescricao: string | null = null) {
  return exigirOk<EstudoDeCaso>(await request.post(`${URL_API}/api/casos`, { data: { nome, dominioDescricao } }), 'criar caso');
}

export async function criarNo(request: APIRequestContext, casoId: string, dados: CriarNoRequest) {
  return exigirOk<No>(await request.post(`${URL_API}/api/casos/${casoId}/nos`, { data: dados }), `criar nó ${dados.nome}`);
}

export async function criarAresta(request: APIRequestContext, casoId: string, dados: CriarArestaRequest) {
  return exigirOk<Aresta>(await request.post(`${URL_API}/api/casos/${casoId}/arestas`, { data: dados }), 'criar aresta');
}

export async function criarFluxo(request: APIRequestContext, casoId: string, dados: CriarFluxoRequest) {
  return exigirOk<Fluxo>(await request.post(`${URL_API}/api/casos/${casoId}/fluxos`, { data: dados }), `criar fluxo ${dados.nome}`);
}

export async function obterCaso(request: APIRequestContext, casoId: string) {
  return exigirOk<EstudoDeCaso>(await request.get(`${URL_API}/api/casos/${casoId}`), 'obter caso');
}

/** Apaga todos os Estudos de Caso cujo nome começa com o prefixo do teste (inclusive os renomeados). */
export async function apagarCasosComPrefixo(request: APIRequestContext, prefixo: string) {
  const casos = await exigirOk<EstudoDeCaso[]>(await request.get(`${URL_API}/api/casos`), 'listar casos');
  for (const caso of casos.filter((c) => c.nome.startsWith(prefixo))) {
    await request.delete(`${URL_API}/api/casos/${caso.id}`);
  }
}
