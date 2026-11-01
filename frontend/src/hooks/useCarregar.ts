import { useCallback, useEffect, useRef, useState } from 'react';

export interface Carregamento<T> {
  dados: T | undefined;
  erro: unknown;
  carregando: boolean;
  recarregar: () => Promise<void>;
}

/**
 * Busca dados da API ao montar e sempre que `chave` mudar. Não guarda cache entre telas:
 * a API recalcula tudo a cada leitura (RF-11), então o front sempre pede o estado atual.
 */
export function useCarregar<T>(buscar: () => Promise<T>, chave: unknown[] = []): Carregamento<T> {
  const [dados, setDados] = useState<T>();
  const [erro, setErro] = useState<unknown>();
  const [carregando, setCarregando] = useState(true);
  const buscarRef = useRef(buscar);
  buscarRef.current = buscar;
  const geracao = useRef(0);

  const recarregar = useCallback(async () => {
    const minha = ++geracao.current;
    setCarregando(true);
    setErro(undefined);
    try {
      const resultado = await buscarRef.current();
      if (minha === geracao.current) setDados(resultado);
    } catch (e) {
      if (minha === geracao.current) setErro(e);
    } finally {
      if (minha === geracao.current) setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void recarregar();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, chave);

  return { dados, erro, carregando, recarregar };
}
