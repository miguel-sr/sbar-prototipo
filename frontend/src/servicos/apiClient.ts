import type { Aresta, AtualizarArestaRequest, CriarArestaRequest } from '../tipos/Aresta';
import type { CadastrarRequest, EntrarRequest, Usuario } from '../tipos/Conta';
import type { AtualizarFluxoRequest, CriarFluxoRequest, Fluxo } from '../tipos/Fluxo';
import type { AtualizarEstudoDeCasoRequest, CriarEstudoDeCasoRequest, EstudoDeCaso } from '../tipos/EstudoDeCaso';
import type { ResultadoDeMetricas } from '../tipos/Metricas';
import type { AtualizarLimitesEPesosRequest, ConjuntoDeParametros, VersaoDeParametros } from '../tipos/Parametros';
import type { AtualizarNoRequest, CriarNoRequest, No } from '../tipos/No';
import type { DadosDoPerfilArquitetural, PerfilArquitetural } from '../tipos/PerfilArquitetural';
import type { LinhaDeRastreabilidade } from '../tipos/Rastreabilidade';

// Único ponto do front que conhece URLs da API (docs/projeto/API.md). A sessão é um cookie HttpOnly enviado
// pelo navegador na mesma origem (Vercel repassa /api): nenhum token passa por aqui.
// Em desenvolvimento VITE_API_URL fica vazio e o Vite repassa /api para a API local.
const URL_BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

/** Erro devolvido pela API no formato ProblemDetails (RFC 7807) — API.md seção 1.1. */
export class ErroDaApi extends Error {
  readonly status: number;

  constructor(status: number, mensagem: string) {
    super(mensagem);
    this.name = 'ErroDaApi';
    this.status = status;
  }
}

/** O recurso pedido não existe (404): não adianta tentar de novo. */
export function ehNaoEncontrado(erro: unknown): boolean {
  return erro instanceof ErroDaApi && erro.status === 404;
}

// Mensagens genéricas do ASP.NET (statusText em inglês) que chegam quando a rota nem casa com
// um controller — traduzidas para o usuário não ver "Not Found." solto.
const MENSAGENS_GENERICAS: Record<string, string> = {
  'not found': 'Recurso não encontrado.',
  'bad request': 'Requisição inválida.',
  'internal server error': 'Erro interno da API.',
  'unprocessable entity': 'Os dados enviados não são válidos.',
  unauthorized: 'Sua sessão expirou. Entre novamente.',
  'too many requests': 'Muitas tentativas. Aguarde um minuto e tente de novo.',
};

const GUID_ENTRE_ASPAS = /\s*'[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}'/gi;

/** Tira ids técnicos (GUID) e nomes de classe da mensagem da API antes de mostrar ao usuário. */
function limparMensagem(mensagem: string): string {
  const generica = MENSAGENS_GENERICAS[mensagem.trim().replace(/\.$/, '').toLowerCase()];
  if (generica) return generica;
  return mensagem
    .replace(GUID_ENTRE_ASPAS, '')
    .replace(/\bEstudoDeCaso\b/g, 'Estudo de Caso')
    .replace(/\bPerfilArquitetural\b/g, 'Perfil Arquitetural')
    .replace(/\s{2,}/g, ' ')
    .trim();
}

/** Texto amigável para mostrar ao usuário a partir de um erro qualquer. */
export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof ErroDaApi) {
    const mensagem = limparMensagem(erro.message);
    if (erro.status >= 500) return `Erro inesperado na API (${erro.status}). ${mensagem}`;
    return mensagem;
  }
  if (erro instanceof TypeError) {
    return 'Não foi possível falar com a API. Confira se ela está rodando e acessível.';
  }
  return erro instanceof Error ? erro.message : String(erro);
}

export async function extrairErro(resposta: Response): Promise<ErroDaApi> {
  let titulo = resposta.statusText || `HTTP ${resposta.status}`;
  try {
    const corpo = (await resposta.json()) as { title?: string };
    if (corpo?.title) titulo = corpo.title;
  } catch {
    // corpo vazio ou não-JSON: fica com o statusText
  }
  return new ErroDaApi(resposta.status, titulo);
}

let avisarPerdaDaSessao: (() => void) | undefined;

/** O SessaoProvider registra aqui o que fazer quando uma chamada volta 401 (sessão expirada). Devolve o cancelamento. */
export function aoPerderASessao(aoPerder: () => void): () => void {
  avisarPerdaDaSessao = aoPerder;
  return () => {
    if (avisarPerdaDaSessao === aoPerder) avisarPerdaDaSessao = undefined;
  };
}

// 401 fora de /api/account/* (login errado também é 401, mas não é sessão perdida) derruba a sessão local.
function tratarSessaoPerdida(caminho: string, resposta: Response) {
  if (resposta.status === 401 && !caminho.startsWith('/api/account/')) avisarPerdaDaSessao?.();
}

async function requisitar<T>(caminho: string, init?: RequestInit): Promise<T> {
  const resposta = await fetch(`${URL_BASE}${caminho}`, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json', ...init.headers } : init?.headers,
  });
  tratarSessaoPerdida(caminho, resposta);
  if (!resposta.ok) throw await extrairErro(resposta);
  if (resposta.status === 204) return undefined as T;
  return (await resposta.json()) as T;
}

export type FormatoDeExportacao = 'json' | 'csv';

/** Arquivo devolvido por um endpoint de download (exportação). */
export interface ArquivoBaixado {
  blob: Blob;
  nome: string;
}

/** Nome do arquivo no cabeçalho Content-Disposition (filename="…" ou filename=…), se houver. */
export function nomeDoArquivo(contentDisposition: string | null, padrao: string): string {
  const achado = contentDisposition?.match(/filename=(?:UTF-8'')?"?([^";]+)"?/i);
  return achado ? decodeURIComponent(achado[1].trim()) : padrao;
}

async function baixar(caminho: string, nomePadrao: string): Promise<ArquivoBaixado> {
  const resposta = await fetch(`${URL_BASE}${caminho}`);
  tratarSessaoPerdida(caminho, resposta);
  if (!resposta.ok) throw await extrairErro(resposta);
  return { blob: await resposta.blob(), nome: nomeDoArquivo(resposta.headers.get('Content-Disposition'), nomePadrao) };
}

const post = <T>(caminho: string, corpo: unknown) =>
  requisitar<T>(caminho, { method: 'POST', body: JSON.stringify(corpo) });
const put = <T>(caminho: string, corpo: unknown) =>
  requisitar<T>(caminho, { method: 'PUT', body: JSON.stringify(corpo) });

export const apiClient = {
  // Conta e sessão
  eu: () => requisitar<Usuario>('/api/account/me'),
  entrar: (dados: EntrarRequest) => post<Usuario>('/api/account/login', dados),
  cadastrar: (dados: CadastrarRequest) => post<Usuario>('/api/account/register', dados),
  sair: () => requisitar<void>('/api/account/logout', { method: 'POST' }),

  // T1/T2 — Estudos de Caso
  listarCasos: () => requisitar<EstudoDeCaso[]>('/api/casos'),
  obterCaso: (id: string) => requisitar<EstudoDeCaso>(`/api/casos/${id}`),
  criarCaso: (dados: CriarEstudoDeCasoRequest) => post<EstudoDeCaso>('/api/casos', dados),
  atualizarCaso: (id: string, dados: AtualizarEstudoDeCasoRequest) => put<EstudoDeCaso>(`/api/casos/${id}`, dados),
  excluirCaso: (id: string) => requisitar<void>(`/api/casos/${id}`, { method: 'DELETE' }),

  // T3 — Nós
  criarNo: (casoId: string, dados: CriarNoRequest) => post<No>(`/api/casos/${casoId}/nos`, dados),
  atualizarNo: (casoId: string, noId: string, dados: AtualizarNoRequest) =>
    put<No>(`/api/casos/${casoId}/nos/${noId}`, dados),
  // Excluir um Nó remove também as Arestas ligadas a ele (regra do backend).
  excluirNo: (casoId: string, noId: string) =>
    requisitar<void>(`/api/casos/${casoId}/nos/${noId}`, { method: 'DELETE' }),

  // T4 — Arestas
  criarAresta: (casoId: string, dados: CriarArestaRequest) =>
    post<Aresta>(`/api/casos/${casoId}/arestas`, dados),
  atualizarAresta: (casoId: string, arestaId: string, dados: AtualizarArestaRequest) =>
    put<Aresta>(`/api/casos/${casoId}/arestas/${arestaId}`, dados),
  excluirAresta: (casoId: string, arestaId: string) =>
    requisitar<void>(`/api/casos/${casoId}/arestas/${arestaId}`, { method: 'DELETE' }),

  // T4b — Fluxos principais (entrada do eixo DEP; TCC 4.2.7, RF05)
  listarFluxos: (casoId: string) => requisitar<Fluxo[]>(`/api/casos/${casoId}/fluxos`),
  criarFluxo: (casoId: string, dados: CriarFluxoRequest) => post<Fluxo>(`/api/casos/${casoId}/fluxos`, dados),
  atualizarFluxo: (casoId: string, fluxoId: string, dados: AtualizarFluxoRequest) =>
    put<Fluxo>(`/api/casos/${casoId}/fluxos/${fluxoId}`, dados),
  excluirFluxo: (casoId: string, fluxoId: string) =>
    requisitar<void>(`/api/casos/${casoId}/fluxos/${fluxoId}`, { method: 'DELETE' }),

  // T6/T7 — Radar e perfil de referência mais próximo (sempre recalculados pela API, nunca cacheados aqui).
  // `versao` reaplica uma versão antiga dos parâmetros (TCC 5.4); sem ela vale a versão vigente.
  obterMetricas: (casoId: string, versao?: number) =>
    requisitar<ResultadoDeMetricas>(`/api/casos/${casoId}/metricas${versao === undefined ? '' : `?versao=${versao}`}`),

  // T8 — Matriz de Rastreabilidade
  obterMatriz: (casoId: string) =>
    requisitar<LinhaDeRastreabilidade[]>(`/api/casos/${casoId}/matriz-rastreabilidade`),

  // Exportação dos resultados (RF13): JSON completo ou CSV, na versão pedida (ou vigente).
  exportarCaso: (casoId: string, formato: FormatoDeExportacao, versao?: number) =>
    baixar(
      `/api/casos/${casoId}/exportacao?formato=${formato}${versao === undefined ? '' : `&versao=${versao}`}`,
      `sbar-exportacao.${formato}`,
    ),

  // Parâmetros versionados (RF11): limites, pesos e assinaturas
  listarVersoesDeParametros: () => requisitar<VersaoDeParametros[]>('/api/parametros'),
  obterParametrosAtuais: () => requisitar<ConjuntoDeParametros>('/api/parametros/atual'),
  obterParametros: (numero: number) => requisitar<ConjuntoDeParametros>(`/api/parametros/${numero}`),
  atualizarLimitesEPesos: (dados: AtualizarLimitesEPesosRequest) =>
    put<ConjuntoDeParametros>('/api/parametros/limites-e-pesos', dados),

  // T9 — Perfis Arquiteturais
  listarPerfis: () => requisitar<PerfilArquitetural[]>('/api/perfis-arquiteturais'),
  criarPerfil: (dados: DadosDoPerfilArquitetural) =>
    post<PerfilArquitetural>('/api/perfis-arquiteturais', dados),
  atualizarPerfil: (id: string, dados: DadosDoPerfilArquitetural) =>
    put<PerfilArquitetural>(`/api/perfis-arquiteturais/${id}`, dados),
  removerPerfil: (id: string) =>
    requisitar<void>(`/api/perfis-arquiteturais/${id}`, { method: 'DELETE' }),
};
