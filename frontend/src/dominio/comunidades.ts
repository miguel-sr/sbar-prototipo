// Limite de leitura da modularidade Q citado no TCC (seção 4.4.1, Clauset–Newman–Moore): valores
// acima de ~0,3 indicam estrutura de comunidades significativa. É só um limiar de EXIBIÇÃO (selo);
// os avisos de caso-limite (sem arestas, comunidade única, Q baixo) vêm prontos da API em `avisos`.
export const MODULARIDADE_SIGNIFICATIVA = 0.3;

export function estruturaDeComunidadesEhSignificativa(q: number | null): boolean {
  return q !== null && q >= MODULARIDADE_SIGNIFICATIVA;
}
