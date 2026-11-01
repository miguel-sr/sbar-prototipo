import { useMemo, useState, type FormEvent } from 'react';
import { glossarioBruto, lerSubsecao, paraHtml } from '../ajuda/glossario';
import { CODIGO_TIPO_DE_ARESTA, ROTULO_PESO, ROTULO_TIPO_DE_ARESTA } from '../dominio/rotulos';
import { justificativaEstaPendente, LIMITES } from '../dominio/validacao';
import { PESOS, TIPOS_DE_ARESTA, type CriarArestaRequest, type Peso, type TipoDeAresta } from '../tipos/Aresta';
import type { No } from '../tipos/No';
import { CampoComContador } from './CampoComContador';
import { MensagemDeErro } from './Estados';
import { SeletorDeNo } from './SeletorDeNo';

interface Props {
  nos: No[];
  aoSalvar: (dados: CriarArestaRequest) => Promise<void>;
  /** Quando presente o formulário edita uma Aresta existente (PUT) em vez de criar uma nova. */
  inicial?: CriarArestaRequest;
  aoCancelar?: () => void;
}

// Texto de apoio vindo do glossário (fonte única): critérios de tipo e de peso (POP-09, T4).
const CRITERIO_TIPO = paraHtml(lerSubsecao(glossarioBruto, 'Tipos de aresta'));
const CRITERIO_PESO = paraHtml(lerSubsecao(glossarioBruto, 'Peso da ligação'));

export function ArestaForm({ nos, aoSalvar, inicial, aoCancelar }: Props) {
  const editando = inicial !== undefined;
  const [origem, setOrigem] = useState(inicial?.noOrigemId ?? '');
  const [destino, setDestino] = useState(inicial?.noDestinoId ?? '');
  const [tipo, setTipo] = useState<TipoDeAresta>(inicial?.tipo ?? 'Sincrona');
  const [peso, setPeso] = useState<Peso>(inicial?.peso ?? 3);
  // Aresta antiga com a justificativa-padrão: o campo começa vazio (o texto-padrão não vale como justificativa).
  const pendente = inicial !== undefined && justificativaEstaPendente(inicial.justificativa);
  const [justificativa, setJustificativa] = useState(pendente ? '' : (inicial?.justificativa ?? ''));
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();
  const [criterios, setCriterios] = useState<'tipo' | 'peso' | null>(null);

  const mesmoNo = origem !== '' && origem === destino;
  const podeEnviar = origem && destino && !mesmoNo && justificativa.trim() !== '' && !enviando;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!podeEnviar) return;
    setEnviando(true);
    setErro(undefined);
    try {
      await aoSalvar({ noOrigemId: origem, noDestinoId: destino, tipo, peso, justificativa: justificativa.trim() });
      if (!editando) {
        // Encadeia: o destino desta ligação costuma ser a origem da próxima.
        setOrigem(destino);
        setDestino('');
        setJustificativa('');
      }
    } catch (err) {
      setErro(err);
    } finally {
      setEnviando(false);
    }
  }

  const criterioHtml = useMemo(() => (criterios === 'tipo' ? CRITERIO_TIPO : criterios === 'peso' ? CRITERIO_PESO : ''), [criterios]);

  return (
    <form className="cartao formulario" onSubmit={enviar}>
      <h2 className="cartao__titulo">{editando ? 'Editar Aresta' : 'Adicionar Aresta'}</h2>
      <SeletorDeNo id="aresta-origem" rotulo="De (origem)" nos={nos} valor={origem} aoMudar={setOrigem} />
      <SeletorDeNo id="aresta-destino" rotulo="Para (destino)" nos={nos} valor={destino} aoMudar={setDestino} />
      {mesmoNo && <p className="aviso-campo">Origem e destino precisam ser Nós diferentes.</p>}

      <fieldset className="grupo-opcoes">
        <legend className="rotulo rotulo--com-acao">
          Tipo
          <button type="button" className="link-botao" onClick={() => setCriterios(criterios === 'tipo' ? null : 'tipo')}>
            {criterios === 'tipo' ? 'ocultar critério' : 'como escolher?'}
          </button>
        </legend>
        <div className="segmentado">
          {TIPOS_DE_ARESTA.map((t) => (
            <label key={t} className={`segmentado__opcao${tipo === t ? ' segmentado__opcao--marcada' : ''}`}>
              <input type="radio" name="aresta-tipo" value={t} checked={tipo === t} onChange={() => setTipo(t)} />
              {ROTULO_TIPO_DE_ARESTA[t]} <span className="codigo">{CODIGO_TIPO_DE_ARESTA[t]}</span>
            </label>
          ))}
        </div>
      </fieldset>

      <fieldset className="grupo-opcoes">
        <legend className="rotulo rotulo--com-acao">
          Peso
          <button type="button" className="link-botao" onClick={() => setCriterios(criterios === 'peso' ? null : 'peso')}>
            {criterios === 'peso' ? 'ocultar critério' : 'como escolher?'}
          </button>
        </legend>
        <div className="segmentado">
          {PESOS.map((p) => (
            <label key={p} className={`segmentado__opcao${peso === p ? ' segmentado__opcao--marcada' : ''}`}>
              <input type="radio" name="aresta-peso" value={p} checked={peso === p} onChange={() => setPeso(p)} />
              {p} · {ROTULO_PESO[p]}
            </label>
          ))}
        </div>
      </fieldset>

      {criterios && <div className="criterio conteudo-md" dangerouslySetInnerHTML={{ __html: criterioHtml }} />}

      <label className="rotulo" htmlFor="aresta-justificativa">
        Justificativa
      </label>
      <CampoComContador
        id="aresta-justificativa"
        valor={justificativa}
        aoMudar={setJustificativa}
        max={LIMITES.justificativaDaAresta}
        multilinha
        placeholder="Por que esta interação existe? Cite o passo do Blueprint (ex.: etapa 2, linha de interação)."
      />
      {pendente && (
        <p className="aviso-campo">Esta Aresta foi cadastrada antes da exigência de justificativa: escreva a justificativa para salvar.</p>
      )}

      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <button type="submit" className="botao botao--primario botao--largo" disabled={!podeEnviar}>
        {enviando ? 'Salvando…' : editando ? 'Salvar alterações' : 'Adicionar Aresta'}
      </button>
      {editando && aoCancelar && (
        <button type="button" className="botao botao--secundario botao--largo" onClick={aoCancelar} disabled={enviando}>
          Cancelar edição
        </button>
      )}
    </form>
  );
}
