import { useRef, useState, type FormEvent } from 'react';
import { DICA_TIPO_DE_NO, ROTULO_TIPO_DE_NO } from '../dominio/rotulos';
import { LIMITES } from '../dominio/validacao';
import { TIPOS_DE_NO, type CriarNoRequest, type TipoDeNo } from '../tipos/No';
import { CampoComContador } from './CampoComContador';
import { MensagemDeErro } from './Estados';

interface Props {
  aoSalvar: (dados: CriarNoRequest) => Promise<void>;
  /** Quando presente o formulário edita um Nó existente (PUT) em vez de criar um novo. */
  inicial?: CriarNoRequest;
  aoCancelar?: () => void;
}

// Formulário de criação/edição de Nó (POP-09 seção 4). Não há campo de Banda: ela é derivada do
// Tipo pelo backend (RegraDeBanda) — é o que evita o erro humano visto na escrita manual dos casos.
export function NoForm({ aoSalvar, inicial, aoCancelar }: Props) {
  const editando = inicial !== undefined;
  const [nome, setNome] = useState(inicial?.nome ?? '');
  const [tipo, setTipo] = useState<TipoDeNo>(inicial?.tipo ?? 'Touchpoint');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();
  const nomeRef = useRef<HTMLInputElement>(null);

  async function enviar(e: FormEvent) {
    e.preventDefault();
    const nomeLimpo = nome.trim();
    if (!nomeLimpo) return;
    setEnviando(true);
    setErro(undefined);
    try {
      await aoSalvar({ nome: nomeLimpo, tipo });
      if (!editando) {
        setNome(''); // mantém o Tipo: normalmente se cadastram vários do mesmo tipo em sequência
        nomeRef.current?.focus();
      }
    } catch (err) {
      setErro(err);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form className="cartao formulario" onSubmit={enviar}>
      <h2 className="cartao__titulo">{editando ? 'Editar Nó' : 'Adicionar Nó'}</h2>
      <label className="rotulo" htmlFor="no-nome">
        Nome
      </label>
      <CampoComContador
        id="no-nome"
        inputRef={nomeRef}
        valor={nome}
        aoMudar={setNome}
        max={LIMITES.nomeDoNo}
        placeholder="Ex.: Carrinho/Checkout"
        obrigatorio
      />
      <fieldset className="grupo-opcoes">
        <legend className="rotulo">Tipo</legend>
        {TIPOS_DE_NO.map((t) => (
          <label key={t} className={`opcao${tipo === t ? ' opcao--marcada' : ''}`}>
            <input type="radio" name="no-tipo" value={t} checked={tipo === t} onChange={() => setTipo(t)} />
            <span className="opcao__titulo">{ROTULO_TIPO_DE_NO[t]}</span>
            <span className="opcao__dica">{DICA_TIPO_DE_NO[t]}</span>
          </label>
        ))}
      </fieldset>
      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <button type="submit" className="botao botao--primario botao--largo" disabled={enviando || !nome.trim()}>
        {enviando ? 'Salvando…' : editando ? 'Salvar alterações' : 'Adicionar Nó'}
      </button>
      {editando && aoCancelar && (
        <button type="button" className="botao botao--secundario botao--largo" onClick={aoCancelar} disabled={enviando}>
          Cancelar edição
        </button>
      )}
      <p className="nota">A Banda é definida automaticamente a partir do Tipo.</p>
    </form>
  );
}
