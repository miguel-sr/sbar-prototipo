import type { Aresta } from './Aresta';
import type { No } from './No';

// Contrato de docs/projeto/API.md seção 3.
export interface EstudoDeCaso {
  id: string;
  nome: string;
  dominioDescricao: string | null;
  dataCriacao: string;
  nos: No[];
  arestas: Aresta[];
}

export interface CriarEstudoDeCasoRequest {
  nome: string;
  dominioDescricao: string | null;
}

/** PUT /api/casos/{id}: substitui nome e descrição (Nós e Arestas não mudam). */
export type AtualizarEstudoDeCasoRequest = CriarEstudoDeCasoRequest;
