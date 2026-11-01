import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { MensagemDeErro } from '../componentes/Estados';
import { PaginaDeConta } from '../componentes/PaginaDeConta';
import { destinoSeguro, useSessao } from '../sessao/ContextoDeSessao';

// Entrada na conta. POST /api/account/login; o cookie de sessão vem na resposta.
export function Entrar() {
  const { estado, entrar } = useSessao();
  const navegar = useNavigate();
  const [params] = useSearchParams();
  const destino = destinoSeguro(params.get('de'));
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();

  if (estado.situacao === 'autenticado') return <Navigate to={destino} replace />;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setErro(undefined);
    try {
      await entrar({ email: email.trim(), senha });
      navegar(destino, { replace: true });
    } catch (err) {
      setErro(err);
      setEnviando(false);
    }
  }

  return (
    <PaginaDeConta titulo="Entrar no SBAR" descricao="Cada conta tem os seus próprios Estudos de Caso, parâmetros e perfis.">
      <form className="formulario" onSubmit={enviar}>
        <label className="rotulo" htmlFor="entrar-email">
          E-mail
        </label>
        <input id="entrar-email" className="campo" type="email" autoComplete="username" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} />
        <label className="rotulo" htmlFor="entrar-senha">
          Senha
        </label>
        <input id="entrar-senha" className="campo" type="password" autoComplete="current-password" required value={senha} onChange={(e) => setSenha(e.target.value)} />
        {erro !== undefined && <MensagemDeErro erro={erro} />}
        <div className="formulario__acoes">
          <Link to={`/cadastrar${destino === '/' ? '' : `?de=${encodeURIComponent(destino)}`}`} className="botao botao--secundario">
            Criar conta
          </Link>
          <button type="submit" className="botao botao--primario" disabled={enviando || !email.trim() || !senha}>
            {enviando ? 'Entrando…' : 'Entrar'}
          </button>
        </div>
      </form>
    </PaginaDeConta>
  );
}
