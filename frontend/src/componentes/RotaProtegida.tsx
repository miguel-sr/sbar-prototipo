import { Navigate, Outlet, useLocation } from 'react-router';
import { useSessao } from '../sessao/ContextoDeSessao';
import { Carregando } from './Estados';

// Tudo, menos /entrar e /cadastrar, exige sessão. Sem ela, vai para o login guardando a rota de origem (?de=).
export function RotaProtegida() {
  const { estado } = useSessao();
  const local = useLocation();

  if (estado.situacao === 'carregando') return <Carregando texto="Verificando sua sessão…" />;
  if (estado.situacao === 'anonimo') {
    const de = `${local.pathname}${local.search}`;
    return <Navigate to={de === '/' ? '/entrar' : `/entrar?de=${encodeURIComponent(de)}`} replace />;
  }
  return <Outlet />;
}
