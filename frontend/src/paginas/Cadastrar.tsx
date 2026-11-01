import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { MensagemDeErro } from '../componentes/Estados';
import { PaginaDeConta } from '../componentes/PaginaDeConta';
import { destinoSeguro, useSessao } from '../sessao/ContextoDeSessao';

// Criação de conta. POST /api/account/register — exige o código de convite de quem administra a instância.
export function Cadastrar() {
  const { estado, cadastrar } = useSessao();
  const navegar = useNavigate();
  const [params] = useSearchParams();
  const destino = destinoSeguro(params.get('de'));
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [convite, setConvite] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();

  if (estado.situacao === 'autenticado') return <Navigate to={destino} replace />;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setEnviando(true);
    setErro(undefined);
    try {
      await cadastrar({ email: email.trim(), senha, codigoDeConvite: convite.trim() });
      navegar(destino, { replace: true });
    } catch (err) {
      setErro(err);
      setEnviando(false);
    }
  }

  return (
    <PaginaDeConta
      titulo="Criar conta"
      descricao="O cadastro exige um código de convite. Seu e-mail não é verificado e não há recuperação de senha nesta versão."
    >
      <form className="formulario" onSubmit={enviar}>
        <label className="rotulo" htmlFor="cadastrar-email">
          E-mail
        </label>
        <input id="cadastrar-email" className="campo" type="email" autoComplete="username" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} />
        <label className="rotulo" htmlFor="cadastrar-senha">
          Senha <span className="opcional">(mínimo de 8 caracteres, com letra e número)</span>
        </label>
        <input id="cadastrar-senha" className="campo" type="password" autoComplete="new-password" required minLength={8} value={senha} onChange={(e) => setSenha(e.target.value)} />
        <label className="rotulo" htmlFor="cadastrar-convite">
          Código de convite
        </label>
        <input id="cadastrar-convite" className="campo" type="password" autoComplete="off" required value={convite} onChange={(e) => setConvite(e.target.value)} />
        {erro !== undefined && <MensagemDeErro erro={erro} />}
        <div className="formulario__acoes">
          <Link to={`/entrar${destino === '/' ? '' : `?de=${encodeURIComponent(destino)}`}`} className="botao botao--secundario">
            Já tenho conta
          </Link>
          <button type="submit" className="botao botao--primario" disabled={enviando || !email.trim() || senha.length < 8 || !convite.trim()}>
            {enviando ? 'Criando…' : 'Criar conta'}
          </button>
        </div>
      </form>
    </PaginaDeConta>
  );
}
