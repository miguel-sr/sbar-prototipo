import { act, renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { useCarregar } from './useCarregar';

function adiado<T>() {
  let resolver!: (valor: T) => void;
  let rejeitar!: (erro: unknown) => void;
  const promessa = new Promise<T>((res, rej) => {
    resolver = res;
    rejeitar = rej;
  });
  return { promessa, resolver, rejeitar };
}

describe('useCarregar', () => {
  it('começa carregando e entrega os dados quando a busca termina', async () => {
    const { result } = renderHook(() => useCarregar(() => Promise.resolve('ok')));
    expect(result.current.carregando).toBe(true);
    await waitFor(() => expect(result.current.carregando).toBe(false));
    expect(result.current.dados).toBe('ok');
    expect(result.current.erro).toBeUndefined();
  });

  it('recarregar busca de novo e limpa o erro anterior', async () => {
    const buscar = vi.fn().mockRejectedValueOnce(new Error('falhou')).mockResolvedValueOnce('agora foi');
    const { result } = renderHook(() => useCarregar<string>(buscar));
    await waitFor(() => expect(result.current.erro).toBeDefined());

    await act(async () => {
      await result.current.recarregar();
    });

    expect(buscar).toHaveBeenCalledTimes(2);
    expect(result.current.erro).toBeUndefined();
    expect(result.current.dados).toBe('agora foi');
  });

  it('uma resposta antiga (mais lenta) não sobrescreve a mais nova', async () => {
    const antiga = adiado<string>();
    const nova = adiado<string>();
    const buscar = vi.fn((id: string) => (id === 'a' ? antiga.promessa : nova.promessa));
    const { result, rerender } = renderHook(({ id }) => useCarregar(() => buscar(id), [id]), { initialProps: { id: 'a' } });

    rerender({ id: 'b' });
    await act(async () => nova.resolver('resposta nova'));
    await waitFor(() => expect(result.current.dados).toBe('resposta nova'));

    await act(async () => antiga.resolver('resposta antiga'));
    expect(result.current.dados).toBe('resposta nova');
    expect(result.current.carregando).toBe(false);
  });

  it('um erro antigo também não derruba os dados mais novos', async () => {
    const antiga = adiado<string>();
    const nova = adiado<string>();
    const buscar = vi.fn((id: string) => (id === 'a' ? antiga.promessa : nova.promessa));
    const { result, rerender } = renderHook(({ id }) => useCarregar(() => buscar(id), [id]), { initialProps: { id: 'a' } });

    rerender({ id: 'b' });
    await act(async () => nova.resolver('boa'));
    await act(async () => antiga.rejeitar(new Error('tardio')));

    expect(result.current.dados).toBe('boa');
    expect(result.current.erro).toBeUndefined();
  });
});
