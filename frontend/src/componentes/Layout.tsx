import { Link, NavLink, Outlet, useNavigate } from 'react-router';
import { useSessao } from '../sessao/ContextoDeSessao';
import { AjudaFlutuante } from './AjudaFlutuante';
import { LogoSbar } from './LogoSbar';

export function Layout() {
  const { estado, sair } = useSessao();
  const navegar = useNavigate();

  async function aoSair() {
    await sair();
    navegar('/entrar', { replace: true });
  }

  return (
    <div className="app">
      <header className="topo">
        <div className="topo__conteudo">
          <Link to="/" className="marca" aria-label="SBAR — início">
            <LogoSbar />
            <span className="marca__nome">SBAR</span>
            <span className="marca__sub">Service Blueprint Architectural Recommendation</span>
          </Link>
          <nav className="topo__nav" aria-label="Principal">
            <NavLink to="/" end className="topo__link">
              Estudos de Caso
            </NavLink>
            <NavLink to="/perfis" className="topo__link">
              Perfis Arquiteturais
            </NavLink>
            <NavLink to="/parametros" className="topo__link">
              Parâmetros
            </NavLink>
          </nav>
          {estado.situacao === 'autenticado' && (
            <div className="topo__conta">
              <span className="topo__email" title={estado.usuario.email}>
                {estado.usuario.email}
              </span>
              <button type="button" className="botao botao--secundario botao--pequeno" onClick={aoSair}>
                Sair
              </button>
            </div>
          )}
        </div>
      </header>
      <main className="conteudo">
        <Outlet />
      </main>
      <AjudaFlutuante />
    </div>
  );
}
