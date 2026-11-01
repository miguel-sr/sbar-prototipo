import { useState, type FormEvent } from 'react';
import { LIMITES } from '../dominio/validacao';
import type { CriarFluxoRequest } from '../tipos/Fluxo';
import type { No } from '../tipos/No';
import { BotaoDeIcone } from './BotaoDeIcone';
import { CampoComContador } from './CampoComContador';
import { ChipDeBanda } from './ChipDeBanda';
import { MensagemDeErro } from './Estados';
import { SeletorDeNo } from './SeletorDeNo';

interface Props {
  nos: No[];
  aoSalvar: (dados: CriarFluxoRequest) => Promise<void>;
  /** Quando presente o formulário edita um Fluxo existente (PUT) em vez de criar um novo. */
  inicial?: CriarFluxoRequest;
  aoCancelar?: () => void;
}

// Formulário de Fluxo principal (TCC 4.2.7, RF05; POST/PUT /api/casos/{id}/fluxos): nome + sequência
// ORDENADA de Nós. Um Nó pode aparecer mais de uma vez (ex.: o retorno P5 do Estudo de Caso 1).
// A regra "cada passo precisa de uma Aresta até o próximo" é validada pela API (422, indica a etapa).
export function FluxoForm({ nos, aoSalvar, inicial, aoCancelar }: Props) {
  const editando = inicial !== undefined;
  const [nome, setNome] = useState(inicial?.nome ?? '');
  const [passos, setPassos] = useState<string[]>(inicial?.nosIds ?? []);
  const [proximo, setProximo] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();

  const nomeDo = (id: string) => nos.find((n) => n.id === id)?.nome ?? 'Nó removido';
  const podeEnviar = nome.trim() !== '' && passos.length >= 2 && !enviando;

  function adicionarPasso() {
    if (!proximo) return;
    setPassos((atual) => [...atual, proximo]);
    setProximo('');
  }

  function mover(indice: number, delta: -1 | 1) {
    setPassos((atual) => {
      const destino = indice + delta;
      if (destino < 0 || destino >= atual.length) return atual;
      const copia = [...atual];
      [copia[indice], copia[destino]] = [copia[destino], copia[indice]];
      return copia;
    });
  }

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!podeEnviar) return;
    setEnviando(true);
    setErro(undefined);
    try {
      await aoSalvar({ nome: nome.trim(), nosIds: passos });
      if (!editando) {
        setNome('');
        setPassos([]);
      }
    } catch (err) {
      setErro(err);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form className="cartao formulario" onSubmit={enviar}>
      <h2 className="cartao__titulo">{editando ? 'Editar Fluxo' : 'Adicionar Fluxo'}</h2>

      <label className="rotulo" htmlFor="fluxo-nome">
        Nome do fluxo
      </label>
      <CampoComContador id="fluxo-nome" valor={nome} aoMudar={setNome} max={LIMITES.nomeDoFluxo} placeholder="Ex.: Pedido e preparo" obrigatorio />

      <fieldset className="grupo-opcoes">
        <legend className="rotulo">Sequência de Nós</legend>
        <p className="nota">
          Na ordem em que as coisas acontecem. Cada passo precisa estar ligado ao próximo por uma Aresta no mesmo sentido; o mesmo Nó pode
          aparecer mais de uma vez.
        </p>
        {passos.length === 0 ? (
          <p className="texto-suave">Nenhum passo ainda. Escolha o primeiro Nó abaixo.</p>
        ) : (
          <ol className="passos" aria-label="Passos do fluxo">
            {passos.map((id, i) => {
              const no = nos.find((n) => n.id === id);
              return (
                <li key={`${i}-${id}`} className="passos__item">
                  <span className="passos__numero">{i + 1}</span>
                  <span className="passos__nome">{nomeDo(id)}</span>
                  {no && <ChipDeBanda banda={no.banda} />}
                  <span className="passos__acoes">
                    <BotaoDeIcone icone="cima" rotulo={`Subir o passo ${i + 1} (${nomeDo(id)})`} onClick={() => mover(i, -1)} />
                    <BotaoDeIcone icone="baixo" rotulo={`Descer o passo ${i + 1} (${nomeDo(id)})`} onClick={() => mover(i, 1)} />
                    <BotaoDeIcone
                      icone="lixeira"
                      perigo
                      rotulo={`Remover o passo ${i + 1} (${nomeDo(id)})`}
                      onClick={() => setPassos((atual) => atual.filter((_, j) => j !== i))}
                    />
                  </span>
                </li>
              );
            })}
          </ol>
        )}
        <SeletorDeNo id="fluxo-proximo-passo" rotulo="Próximo passo" nos={nos} valor={proximo} aoMudar={setProximo} obrigatorio={false} />
        <button type="button" className="botao botao--secundario botao--largo" onClick={adicionarPasso} disabled={!proximo}>
          Adicionar passo
        </button>
      </fieldset>

      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <button type="submit" className="botao botao--primario botao--largo" disabled={!podeEnviar}>
        {enviando ? 'Salvando…' : editando ? 'Salvar alterações' : 'Adicionar Fluxo'}
      </button>
      {passos.length < 2 && <p className="nota">Um fluxo precisa de pelo menos dois passos.</p>}
      {editando && aoCancelar && (
        <button type="button" className="botao botao--secundario botao--largo" onClick={aoCancelar} disabled={enviando}>
          Cancelar edição
        </button>
      )}
    </form>
  );
}
