import { vi } from 'vitest';

// Substituto do `apiClient` para o vi.mock. Fica num arquivo próprio que NÃO importa nada do app:
// o factory do vi.mock roda enquanto o módulo real do apiClient ainda está sendo substituído, e
// importar o app daqui criaria um ciclo (helper → página → apiClient mockado).
//
// Uso, no topo de cada arquivo de teste de página/componente que chama a API:
//   vi.mock('../servicos/apiClient', async (importOriginal) => ({
//     ...(await importOriginal<typeof import('../servicos/apiClient')>()),
//     apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
//   }));
export function apiClientFalso() {
  return {
    eu: vi.fn(),
    entrar: vi.fn(),
    cadastrar: vi.fn(),
    sair: vi.fn(),
    listarCasos: vi.fn(),
    obterCaso: vi.fn(),
    criarCaso: vi.fn(),
    atualizarCaso: vi.fn(),
    excluirCaso: vi.fn(),
    criarNo: vi.fn(),
    atualizarNo: vi.fn(),
    excluirNo: vi.fn(),
    criarAresta: vi.fn(),
    atualizarAresta: vi.fn(),
    excluirAresta: vi.fn(),
    listarFluxos: vi.fn(),
    criarFluxo: vi.fn(),
    atualizarFluxo: vi.fn(),
    excluirFluxo: vi.fn(),
    obterMetricas: vi.fn(),
    obterMatriz: vi.fn(),
    exportarCaso: vi.fn(),
    listarVersoesDeParametros: vi.fn(),
    obterParametrosAtuais: vi.fn(),
    obterParametros: vi.fn(),
    atualizarLimitesEPesos: vi.fn(),
    listarPerfis: vi.fn(),
    criarPerfil: vi.fn(),
    atualizarPerfil: vi.fn(),
    removerPerfil: vi.fn(),
  };
}
