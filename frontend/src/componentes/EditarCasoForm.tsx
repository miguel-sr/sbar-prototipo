import { useState, type FormEvent } from 'react';
import { LIMITES } from '../dominio/validacao';
import type { EstudoDeCaso } from '../tipos/EstudoDeCaso';
import { CampoComContador } from './CampoComContador';
import { MensagemDeErro } from './Estados';

interface Props {
  caso: Pick<EstudoDeCaso, 'nome' | 'dominioDescricao'>;
  aoSalvar: (dados: { nome: string; dominioDescricao: string | null }) => Promise<void>;
  aoCancelar: () => void;
}

// Edição de nome e descrição do Estudo de Caso (PUT /api/casos/{id}) no cabeçalho da página.
export function EditarCasoForm({ caso, aoSalvar, aoCancelar }: Props) {
  const [nome, setNome] = useState(caso.nome);
  const [descricao, setDescricao] = useState(caso.dominioDescricao ?? '');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();

  async function enviar(e: FormEvent) {
    e.preventDefault();
    const nomeLimpo = nome.trim();
    if (!nomeLimpo) return;
    setEnviando(true);
    setErro(undefined);
    try {
      await aoSalvar({ nome: nomeLimpo, dominioDescricao: descricao.trim() || null });
    } catch (err) {
      setErro(err);
      setEnviando(false);
    }
  }

  return (
    <form className="cartao formulario caso__edicao" onSubmit={enviar}>
      <label className="rotulo" htmlFor="editar-caso-nome">
        Nome do sistema
      </label>
      <CampoComContador id="editar-caso-nome" valor={nome} aoMudar={setNome} max={LIMITES.nomeDoCaso} obrigatorio autoFocus />
      <label className="rotulo" htmlFor="editar-caso-descricao">
        Domínio de negócio <span className="opcional">(opcional)</span>
      </label>
      <CampoComContador id="editar-caso-descricao" valor={descricao} aoMudar={setDescricao} max={LIMITES.descricaoDoCaso} multilinha />
      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <div className="formulario__acoes">
        <button type="button" className="botao botao--secundario" onClick={aoCancelar} disabled={enviando}>
          Cancelar
        </button>
        <button type="submit" className="botao botao--primario" disabled={enviando || !nome.trim()}>
          {enviando ? 'Salvando…' : 'Salvar alterações'}
        </button>
      </div>
    </form>
  );
}
