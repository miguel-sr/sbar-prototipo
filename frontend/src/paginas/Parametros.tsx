import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { CampoComContador } from '../componentes/CampoComContador';
import { Carregando, MensagemDeErro } from '../componentes/Estados';
import { INFO_DO_EIXO } from '../dominio/eixos';
import { formatarData, formatarNumero } from '../dominio/rotulos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';
import { EIXOS, type Eixo } from '../tipos/Metricas';
import { TOLERANCIA_DA_SOMA_DOS_PESOS, type ConjuntoDeParametros } from '../tipos/Parametros';

const MAXIMO_DA_DESCRICAO = 200;

type Linha = { minimo: string; maximo: string; peso: string };
type Rascunho = Record<Eixo, Linha>;

function lerNumero(texto: string): number {
  return texto.trim() === '' ? Number.NaN : Number(texto.replace(',', '.'));
}

function paraRascunho(conjunto: ConjuntoDeParametros): Rascunho {
  const r = {} as Rascunho;
  for (const eixo of EIXOS) {
    const p = conjunto.eixos.find((x) => x.eixo === eixo);
    r[eixo] = { minimo: String(p?.minimo ?? ''), maximo: String(p?.maximo ?? ''), peso: String(p?.peso ?? '') };
  }
  return r;
}

/** Soma dos pesos digitados (NaN se algum peso for inválido). */
function somaDosPesos(r: Rascunho): number {
  return EIXOS.reduce((total, eixo) => total + lerNumero(r[eixo].peso), 0);
}

/** Primeiro problema do rascunho (mensagem pronta para a tela) ou null. */
function validar(r: Rascunho): string | null {
  for (const eixo of EIXOS) {
    const { minimo, maximo, peso } = r[eixo];
    const min = lerNumero(minimo);
    const max = lerNumero(maximo);
    const p = lerNumero(peso);
    if (Number.isNaN(min) || Number.isNaN(max)) return `Informe o limite mínimo e o máximo de ${eixo}.`;
    if (min >= max) return `Em ${eixo}, o mínimo (${minimo}) precisa ser menor que o máximo (${maximo}).`;
    if (Number.isNaN(p) || p < 0) return `O peso de ${eixo} precisa ser um número maior ou igual a 0.`;
  }
  const soma = somaDosPesos(r);
  if (Math.abs(soma - 1) > TOLERANCIA_DA_SOMA_DOS_PESOS) return `Soma dos pesos igual a ${formatarNumero(soma, 4)}; deve ser 1 (RN07).`;
  return null;
}

interface FormProps {
  conjunto: ConjuntoDeParametros;
  aoSalvar: (descricao: string, rascunho: Rascunho) => Promise<void>;
}

// Limites (Quadro 7) e pesos pᵢ (4.5.1) editáveis; a soma dos pesos precisa ser 1 (RN07, CT10) e é mostrada
// em tempo real. Salvar NÃO altera a versão atual: cria uma nova (RF11).
function FormularioDeLimitesEPesos({ conjunto, aoSalvar }: FormProps) {
  const [rascunho, setRascunho] = useState<Rascunho>(() => paraRascunho(conjunto));
  const [descricao, setDescricao] = useState('');
  const [erro, setErro] = useState<unknown>();
  const [salvando, setSalvando] = useState(false);

  const soma = somaDosPesos(rascunho);
  const problema = validar(rascunho);
  const somaOk = Math.abs(soma - 1) <= TOLERANCIA_DA_SOMA_DOS_PESOS;
  const alterado = JSON.stringify(rascunho) !== JSON.stringify(paraRascunho(conjunto));
  const podeSalvar = !salvando && alterado && problema === null && descricao.trim() !== '';

  const mudar = (eixo: Eixo, campo: keyof Linha, valor: string) =>
    setRascunho((r) => ({ ...r, [eixo]: { ...r[eixo], [campo]: valor } }));

  async function enviar(e: FormEvent) {
    e.preventDefault();
    if (!podeSalvar) return;
    setSalvando(true);
    setErro(undefined);
    try {
      await aoSalvar(descricao.trim(), rascunho);
    } catch (falha) {
      setErro(falha);
    } finally {
      setSalvando(false);
    }
  }

  return (
    <form className="cartao formulario" onSubmit={enviar} aria-label="Limites e pesos dos eixos">
      <h2 className="cartao__titulo">Limites de normalização e pesos dos eixos</h2>
      <p className="texto-suave">
        Cada métrica bruta é levada à escala 0–5 entre o seu mínimo e o seu máximo (TCC 4.5, Quadro 7); os pesos pᵢ
        combinam os eixos na distância até cada perfil (4.5.1) e precisam somar 1.
      </p>
      <div className="tabela-rolagem">
        <table className="tabela">
          <thead>
            <tr>
              <th>Eixo</th>
              <th className="coluna-numero">Mínimo</th>
              <th className="coluna-numero">Máximo</th>
              <th className="coluna-numero">Peso pᵢ</th>
            </tr>
          </thead>
          <tbody>
            {EIXOS.map((eixo) => (
              <tr key={eixo}>
                <td>
                  <span className="metrica__codigo">{eixo}</span> {INFO_DO_EIXO[eixo].nome}
                </td>
                {(['minimo', 'maximo', 'peso'] as const).map((campo) => (
                  <td key={campo} className="coluna-numero">
                    <input
                      className="campo campo--numero campo--tabela"
                      inputMode="decimal"
                      aria-label={`${campo === 'minimo' ? 'Mínimo' : campo === 'maximo' ? 'Máximo' : 'Peso'} de ${eixo}`}
                      value={rascunho[eixo][campo]}
                      onChange={(e) => mudar(eixo, campo, e.target.value)}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row" colSpan={3}>
                Soma dos pesos
              </th>
              <td className="coluna-numero">
                <span className={`numero ${somaOk ? 'texto-sucesso' : 'texto-erro'}`} data-testid="soma-dos-pesos">
                  {Number.isNaN(soma) ? '—' : formatarNumero(soma, 4)}
                </span>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      {!somaOk && (
        <p className="alerta alerta--erro" role="alert">
          {Number.isNaN(soma)
            ? 'Há um peso inválido.'
            : `Soma dos pesos igual a ${formatarNumero(soma, 4)}; deve ser 1. Não é possível salvar.`}
        </p>
      )}
      {somaOk && problema !== null && (
        <p className="alerta alerta--erro" role="alert">
          {problema}
        </p>
      )}

      <label htmlFor="descricao-da-versao" className="rotulo">
        O que mudou (descrição da nova versão)
      </label>
      <CampoComContador
        id="descricao-da-versao"
        valor={descricao}
        aoMudar={setDescricao}
        max={MAXIMO_DA_DESCRICAO}
        placeholder="Ex.: Limite máximo de UX recalibrado para 4,0"
      />
      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <div className="formulario__acoes">
        <span className="nota">Salvar cria a versão {conjunto.versao.numero + 1}; a versão {conjunto.versao.numero} continua intacta.</span>
        <span className="espacador" />
        <button type="submit" className="botao botao--primario" disabled={!podeSalvar}>
          {salvando ? 'Salvando…' : 'Salvar como nova versão'}
        </button>
      </div>
    </form>
  );
}

function TabelaDeAssinaturas({ conjunto }: { conjunto: ConjuntoDeParametros }) {
  return (
    <div className="cartao">
      <h2 className="cartao__titulo">Assinaturas de referência (Quadro 8)</h2>
      <p className="texto-suave">
        Valores de cada estilo nos dez eixos, nesta versão. Para alterá-los, use <Link to="/perfis">Perfis Arquiteturais</Link>:
        cada alteração gera uma nova versão. As assinaturas são provisórias, definidas pelos pesquisadores (TCC 4.6).
      </p>
      {conjunto.assinaturas.length === 0 ? (
        <p className="nota">Nenhuma assinatura cadastrada nesta versão.</p>
      ) : (
        <div className="tabela-rolagem">
          <table className="tabela" aria-label="Assinaturas de referência">
            <thead>
              <tr>
                <th>Perfil</th>
                {EIXOS.map((eixo) => (
                  <th key={eixo} className="coluna-numero">
                    {eixo}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {conjunto.assinaturas.map((a) => (
                <tr key={a.id}>
                  <td>{a.nome}</td>
                  {EIXOS.map((eixo) => (
                    <td key={eixo} className="coluna-numero numero">
                      {formatarNumero(a.valores[eixo], 1)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function TabelaSomenteLeitura({ conjunto }: { conjunto: ConjuntoDeParametros }) {
  const soma = conjunto.eixos.reduce((total, p) => total + p.peso, 0);
  return (
    <div className="cartao">
      <h2 className="cartao__titulo">Limites e pesos — versão {conjunto.versao.numero} (somente leitura)</h2>
      <div className="tabela-rolagem">
        <table className="tabela" aria-label={`Limites e pesos da versão ${conjunto.versao.numero}`}>
          <thead>
            <tr>
              <th>Eixo</th>
              <th className="coluna-numero">Mínimo</th>
              <th className="coluna-numero">Máximo</th>
              <th className="coluna-numero">Peso pᵢ</th>
            </tr>
          </thead>
          <tbody>
            {EIXOS.map((eixo) => {
              const p = conjunto.eixos.find((x) => x.eixo === eixo);
              return (
                <tr key={eixo}>
                  <td>
                    <span className="metrica__codigo">{eixo}</span> {INFO_DO_EIXO[eixo].nome}
                  </td>
                  <td className="coluna-numero numero">{p ? formatarNumero(p.minimo, 2) : '—'}</td>
                  <td className="coluna-numero numero">{p ? formatarNumero(p.maximo, 2) : '—'}</td>
                  <td className="coluna-numero numero">{p ? formatarNumero(p.peso, 4) : '—'}</td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row" colSpan={3}>
                Soma dos pesos
              </th>
              <td className="coluna-numero numero">{formatarNumero(soma, 4)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

// Parâmetros do cálculo (limites, pesos, assinaturas) com histórico de versões (RF11, US06, CT10).
// Fora do fluxo de um Estudo de Caso: vale para todos eles; cada análise informa a versão que usou.
export function Parametros() {
  const [visualizando, setVisualizando] = useState<number>();
  const [recarga, setRecarga] = useState(0);
  const [aviso, setAviso] = useState<string | null>(null);
  const versoes = useCarregar(() => apiClient.listarVersoesDeParametros(), [recarga]);
  const conjunto = useCarregar(
    () => (visualizando === undefined ? apiClient.obterParametrosAtuais() : apiClient.obterParametros(visualizando)),
    [visualizando, recarga],
  );

  async function salvar(descricao: string, rascunho: Rascunho) {
    const novo = await apiClient.atualizarLimitesEPesos({
      descricao,
      eixos: EIXOS.map((eixo) => ({
        eixo,
        minimo: lerNumero(rascunho[eixo].minimo),
        maximo: lerNumero(rascunho[eixo].maximo),
        peso: lerNumero(rascunho[eixo].peso),
      })),
    });
    setAviso(`Versão ${novo.versao.numero} criada. A análise de cada Estudo de Caso já usa os novos valores.`);
    setRecarga((n) => n + 1);
  }

  const vigente = versoes.dados?.reduce((maior, v) => Math.max(maior, v.numero), 0);
  const ordenadas = [...(versoes.dados ?? [])].sort((a, b) => b.numero - a.numero);

  return (
    <section className="pilha">
      <div className="cabecalho-pagina">
        <div>
          <h1 className="titulo-pagina">Parâmetros do cálculo</h1>
          <p className="texto-suave">
            Limites de normalização, pesos dos eixos e assinaturas de referência. Cada alteração cria uma{' '}
            <strong>nova versão</strong> imutável; cada análise informa a versão usada e pode ser reaplicada com versões
            antigas (TCC 4.5, 4.6.1 e 5.4).
          </p>
        </div>
      </div>

      {aviso && (
        <p className="alerta alerta--info" role="status">
          {aviso}
        </p>
      )}
      {conjunto.carregando && !conjunto.dados && <Carregando />}
      {conjunto.erro !== undefined && !conjunto.dados && <MensagemDeErro erro={conjunto.erro} aoTentarDeNovo={conjunto.recarregar} />}

      {conjunto.dados && (
        <>
          <p className="nota" data-testid="versao-exibida">
            {visualizando === undefined
              ? `Versão vigente: ${conjunto.dados.versao.numero} — ${conjunto.dados.versao.descricao} (${formatarData(conjunto.dados.versao.criadaEm)})`
              : `Visualizando a versão ${conjunto.dados.versao.numero} — ${conjunto.dados.versao.descricao} (${formatarData(conjunto.dados.versao.criadaEm)}), somente leitura.`}
            {visualizando !== undefined && (
              <>
                {' '}
                <button type="button" className="link-botao" onClick={() => setVisualizando(undefined)}>
                  Voltar à versão vigente
                </button>
              </>
            )}
          </p>
          {visualizando === undefined ? (
            <FormularioDeLimitesEPesos key={conjunto.dados.versao.numero} conjunto={conjunto.dados} aoSalvar={salvar} />
          ) : (
            <TabelaSomenteLeitura conjunto={conjunto.dados} />
          )}
          <TabelaDeAssinaturas conjunto={conjunto.dados} />
        </>
      )}

      <div className="cartao">
        <h2 className="cartao__titulo">Histórico de versões</h2>
        {versoes.erro !== undefined && !versoes.dados && <MensagemDeErro erro={versoes.erro} aoTentarDeNovo={versoes.recarregar} />}
        {ordenadas.length === 0 && versoes.dados && <p className="nota">Nenhuma versão registrada ainda.</p>}
        {ordenadas.length > 0 && (
          <ul className="lista-de-versoes">
            {ordenadas.map((v) => (
              <li key={v.numero} className="lista-de-versoes__item">
                <span>
                  <strong>Versão {v.numero}</strong>
                  {v.numero === vigente && <span className="etiqueta etiqueta--vigente">vigente</span>}
                  {' — '}
                  {v.descricao} <span className="texto-suave">· {formatarData(v.criadaEm)}</span>
                </span>
                <button
                  type="button"
                  className="botao botao--secundario botao--pequeno"
                  aria-label={`Ver a versão ${v.numero}`}
                  disabled={v.numero === vigente ? visualizando === undefined : visualizando === v.numero}
                  onClick={() => setVisualizando(v.numero === vigente ? undefined : v.numero)}
                >
                  Ver
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}

