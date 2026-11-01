// Limites de texto aceitos pela API (422 acima disso) — mantidos aqui para o formulário avisar
// antes de enviar. Espelham as regras do backend (Estudo de Caso 120, Nó 100, descrição 1000,
// Fluxo 100, justificativa da Aresta 500).
export const LIMITES = {
  nomeDoCaso: 120,
  descricaoDoCaso: 1000,
  nomeDoNo: 100,
  nomeDoFluxo: 100,
  justificativaDaAresta: 500,
} as const;

/**
 * Texto que o backend grava nas Arestas cadastradas antes da exigência de justificativa (RN03).
 * A interface as marca como "pendente de revisão" e não pré-preenche o campo com ele.
 */
export const JUSTIFICATIVA_PENDENTE = 'Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)';

export function justificativaEstaPendente(justificativa: string): boolean {
  return justificativa.trim() === JUSTIFICATIVA_PENDENTE;
}

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Id de rota válido? Evita bater na API com algo que nunca será um Estudo de Caso. */
export function ehGuid(valor: string): boolean {
  return GUID.test(valor);
}
