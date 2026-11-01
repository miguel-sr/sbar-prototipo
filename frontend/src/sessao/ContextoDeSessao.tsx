import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { apiClient, aoPerderASessao } from '../servicos/apiClient';
import type { CadastrarRequest, EntrarRequest, Usuario } from '../tipos/Conta';

// A sessão é um cookie HttpOnly da API: o front não guarda token, só sabe QUEM está logado (GET /api/account/me).
export type EstadoDaSessao = { situacao: 'carregando' } | { situacao: 'anonimo' } | { situacao: 'autenticado'; usuario: Usuario };

interface ContextoDaSessao {
  estado: EstadoDaSessao;
  entrar: (dados: EntrarRequest) => Promise<void>;
  cadastrar: (dados: CadastrarRequest) => Promise<void>;
  sair: () => Promise<void>;
}

const Contexto = createContext<ContextoDaSessao | undefined>(undefined);

export function SessaoProvider({ children }: { children: ReactNode }) {
  const [estado, setEstado] = useState<EstadoDaSessao>({ situacao: 'carregando' });

  // Ao abrir o app: há sessão? Qualquer falha (401, rede) deixa o usuário anônimo; a tela de login mostra o resto.
  useEffect(() => {
    let ativo = true;
    apiClient
      .eu()
      .then((usuario) => ativo && setEstado({ situacao: 'autenticado', usuario }))
      .catch(() => ativo && setEstado({ situacao: 'anonimo' }));
    return () => {
      ativo = false;
    };
  }, []);

  // Um 401 em qualquer chamada (sessão expirada) derruba a sessão local; a rota protegida leva ao login.
  useEffect(() => aoPerderASessao(() => setEstado({ situacao: 'anonimo' })), []);

  const entrar = useCallback(async (dados: EntrarRequest) => {
    setEstado({ situacao: 'autenticado', usuario: await apiClient.entrar(dados) });
  }, []);
  const cadastrar = useCallback(async (dados: CadastrarRequest) => {
    setEstado({ situacao: 'autenticado', usuario: await apiClient.cadastrar(dados) });
  }, []);
  const sair = useCallback(async () => {
    try {
      await apiClient.sair();
    } finally {
      setEstado({ situacao: 'anonimo' });
    }
  }, []);

  const valor = useMemo(() => ({ estado, entrar, cadastrar, sair }), [estado, entrar, cadastrar, sair]);
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function useSessao(): ContextoDaSessao {
  const contexto = useContext(Contexto);
  if (!contexto) throw new Error('useSessao precisa estar dentro de <SessaoProvider>.');
  return contexto;
}

/** Só aceita destino interno ("/casos/…"): evita redirecionar para outro site por um ?de= forjado. */
export function destinoSeguro(de: string | null): string {
  return de && de.startsWith('/') && !de.startsWith('//') && !de.startsWith('/\\') ? de : '/';
}
