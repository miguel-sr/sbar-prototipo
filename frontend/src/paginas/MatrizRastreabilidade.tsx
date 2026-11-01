import { useState } from 'react';
import { Link } from 'react-router';
import { useCaso } from '../componentes/CasoLayout';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { INFO_DO_EIXO } from '../dominio/eixos';
import { salvarComoArquivo } from '../dominio/download';
import { formatarData } from '../dominio/rotulos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient, type FormatoDeExportacao } from '../servicos/apiClient';
import { EIXOS, type Eixo } from '../tipos/Metricas';
import type { CategoriaDaMatriz, LinhaDeRastreabilidade } from '../tipos/Rastreabilidade';

const CATEGORIAS: Record<CategoriaDaMatriz, { rotulo: string; classe: string; sufixo?: string }> = {
  gargalo: { rotulo: 'Gargalo', classe: 'gargalo', sufixo: ' (gargalo)' },
  'aresta-critica': { rotulo: 'Aresta crítica', classe: 'critica', sufixo: ' (crítica)' },
  'aresta-externa': { rotulo: 'Aresta externa', classe: 'externa', sufixo: ' (externa)' },
  comunidade: { rotulo: 'Comunidade', classe: 'comunidade' },
  fluxo: { rotulo: 'Fluxo', classe: 'fluxo' },
  'no-frontstage': { rotulo: 'Ponto de contato', classe: 'frontstage' },
};

/** A API informa `categoria`; o sufixo do nome vale como reserva para respostas antigas. */
function categoriaDe(linha: LinhaDeRastreabilidade): CategoriaDaMatriz | undefined {
  if (linha.categoria in CATEGORIAS) return linha.categoria;
  return (Object.keys(CATEGORIAS) as CategoriaDaMatriz[]).find((c) => {
    const sufixo = CATEGORIAS[c].sufixo;
    return sufixo !== undefined && linha.noOuAresta.endsWith(sufixo);
  });
}

function nomeDoElemento(linha: LinhaDeRastreabilidade, categoria?: CategoriaDaMatriz): string {
  const sufixo = categoria ? CATEGORIAS[categoria].sufixo : undefined;
  return sufixo && linha.noOuAresta.endsWith(sufixo) ? linha.noOuAresta.slice(0, -sufixo.length) : linha.noOuAresta;
}

function TabelaDaMatriz({ linhas, mostrarEixo }: { linhas: LinhaDeRastreabilidade[]; mostrarEixo: boolean }) {
  return (
    <div className="tabela-rolagem">
      <table className="tabela tabela--matriz">
        <thead>
          <tr>
            <th>Elemento do Blueprint</th>
            <th>Nó / Aresta</th>
            <th>Métrica</th>
            {mostrarEixo && <th>Eixo</th>}
            <th>Contribuição</th>
          </tr>
        </thead>
        <tbody>
          {linhas.map((l, i) => {
            const categoria = categoriaDe(l);
            return (
              <tr key={`${l.noOuAresta}-${i}`}>
                <td>{l.elemento}</td>
                <td>
                  {nomeDoElemento(l, categoria)}
                  {categoria && (
                    <>
                      {' '}
                      <span className={`etiqueta etiqueta--${CATEGORIAS[categoria].classe}`}>{CATEGORIAS[categoria].rotulo}</span>
                    </>
                  )}
                </td>
                <td>
                  <code className="formula formula--inline">{l.metrica}</code>
                </td>
                {mostrarEixo && (
                  <td>
                    {l.eixos.length === 0
                      ? '—'
                      : l.eixos.map((e) => (
                          <span key={e} className="metrica__codigo" title={INFO_DO_EIXO[e].nome}>
                            {e}
                          </span>
                        ))}
                  </td>
                )}
                <td className="texto-suave">{l.contribuicao}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// T8 — MatrizRastreabilidade: elemento do Blueprint → Nó/Aresta → métrica → eixo → contribuição.
// GET /api/casos/{id}/matriz-rastreabilidade (RF12, RNF-01). Dois modos: "Por eixo" (US07: cada eixo lista os
// Nós e Arestas que contribuíram) e "Lista". Exporta os resultados em JSON ou CSV (RF13).
export function MatrizRastreabilidade() {
  const { caso } = useCaso();
  const chave = `${caso.id}:${caso.nos.length}:${caso.arestas.length}`;
  const matriz = useCarregar(() => apiClient.obterMatriz(caso.id), [chave]);
  const versoes = useCarregar(() => apiClient.listarVersoesDeParametros(), [chave]);
  const [modo, setModo] = useState<'eixo' | 'lista'>('eixo');
  const [filtroDeEixo, setFiltroDeEixo] = useState<Eixo | null>(null);
  const [filtroDeCategoria, setFiltroDeCategoria] = useState<CategoriaDaMatriz | null>(null);
  const [exportando, setExportando] = useState<FormatoDeExportacao | null>(null);
  const [erroDaExportacao, setErroDaExportacao] = useState<unknown>();

  if (matriz.carregando && !matriz.dados) return <Carregando />;
  if (!matriz.dados) return <MensagemDeErro erro={matriz.erro} aoTentarDeNovo={matriz.recarregar} />;

  const linhas = matriz.dados;
  const vigente = versoes.dados?.length ? versoes.dados.reduce((maior, v) => (v.numero > maior.numero ? v : maior)) : undefined;
  const categoriasPresentes = [...new Set(linhas.map(categoriaDe).filter((c): c is CategoriaDaMatriz => c !== undefined))];
  const eixosPresentes = [...new Set(linhas.flatMap((l) => l.eixos))];
  const porCategoria = filtroDeCategoria ? linhas.filter((l) => categoriaDe(l) === filtroDeCategoria) : linhas;
  const visiveis = modo === 'lista' && filtroDeEixo ? porCategoria.filter((l) => l.eixos.includes(filtroDeEixo)) : porCategoria;
  const foraDosEixos = porCategoria.filter((l) => l.eixos.length === 0);

  async function exportar(formato: FormatoDeExportacao) {
    setExportando(formato);
    setErroDaExportacao(undefined);
    try {
      const arquivo = await apiClient.exportarCaso(caso.id, formato);
      salvarComoArquivo(arquivo.blob, arquivo.nome);
    } catch (erro) {
      setErroDaExportacao(erro);
    } finally {
      setExportando(null);
    }
  }

  return (
    <div className="pilha">
      <div className="cabecalho-secao">
        <div>
          <h2 className="titulo-secao">Matriz de Rastreabilidade</h2>
          <p className="texto-suave">
            Quais elementos do Mapa alimentam cada eixo do Radar: gargalos, arestas críticas e externas, comunidades,
            fluxos e pontos de contato. Nada aqui é digitado: tudo é derivado do Mapa atual.
          </p>
          {vigente && (
            <p className="nota" data-testid="versao-dos-parametros">
              Parâmetros vigentes: versão {vigente.numero} ({formatarData(vigente.criadaEm)}) ·{' '}
              <Link to="/parametros">ver parâmetros</Link>
            </p>
          )}
        </div>
        <div className="acoes-exportar" role="group" aria-label="Exportar resultados">
          <button type="button" className="botao botao--secundario botao--pequeno" disabled={exportando !== null} onClick={() => void exportar('json')}>
            {exportando === 'json' ? 'Exportando…' : 'Exportar JSON'}
          </button>
          <button type="button" className="botao botao--secundario botao--pequeno" disabled={exportando !== null} onClick={() => void exportar('csv')}>
            {exportando === 'csv' ? 'Exportando…' : 'Exportar CSV'}
          </button>
        </div>
      </div>
      {erroDaExportacao !== undefined && <MensagemDeErro erro={erroDaExportacao} />}

      {linhas.length === 0 ? (
        <EstadoVazio titulo="Nenhum elemento relevante ainda">
          O Mapa ainda não tem gargalo, aresta Crítica, aresta ligada a um serviço Externo, comunidade nem fluxo
          válido. <Link to="../mapa">Ver o Mapa</Link>
        </EstadoVazio>
      ) : (
        <>
          <div className="filtros" role="group" aria-label="Modo de exibição">
            <button type="button" aria-pressed={modo === 'eixo'} className={`filtro${modo === 'eixo' ? ' filtro--ativo' : ''}`} onClick={() => setModo('eixo')}>
              Por eixo
            </button>
            <button type="button" aria-pressed={modo === 'lista'} className={`filtro${modo === 'lista' ? ' filtro--ativo' : ''}`} onClick={() => setModo('lista')}>
              Lista
            </button>
          </div>

          {categoriasPresentes.length > 1 && (
            <div className="filtros" role="group" aria-label="Filtrar por categoria">
              <button type="button" className={`filtro${filtroDeCategoria === null ? ' filtro--ativo' : ''}`} onClick={() => setFiltroDeCategoria(null)}>
                Todas as categorias ({linhas.length})
              </button>
              {categoriasPresentes.map((c) => (
                <button key={c} type="button" className={`filtro${filtroDeCategoria === c ? ' filtro--ativo' : ''}`} onClick={() => setFiltroDeCategoria(c)}>
                  {CATEGORIAS[c].rotulo}
                </button>
              ))}
            </div>
          )}

          {modo === 'lista' ? (
            <>
              {eixosPresentes.length > 1 && (
                <div className="filtros" role="group" aria-label="Filtrar por eixo">
                  <button type="button" className={`filtro${filtroDeEixo === null ? ' filtro--ativo' : ''}`} onClick={() => setFiltroDeEixo(null)}>
                    Todos ({porCategoria.length})
                  </button>
                  {eixosPresentes.map((e) => (
                    <button key={e} type="button" className={`filtro${filtroDeEixo === e ? ' filtro--ativo' : ''}`} onClick={() => setFiltroDeEixo(e)}>
                      {e} · {INFO_DO_EIXO[e].nome}
                    </button>
                  ))}
                </div>
              )}
              <TabelaDaMatriz linhas={visiveis} mostrarEixo />
            </>
          ) : (
            <>
              {EIXOS.map((eixo) => {
                const doEixo = porCategoria.filter((l) => l.eixos.includes(eixo));
                if (doEixo.length === 0) return null;
                return (
                  <section key={eixo} className="cartao" aria-label={`Eixo ${eixo}`}>
                    <h3 className="cartao__titulo">
                      <span className="metrica__codigo">{eixo}</span> {INFO_DO_EIXO[eixo].nome}{' '}
                      <span className="texto-suave">({doEixo.length})</span>
                    </h3>
                    <TabelaDaMatriz linhas={doEixo} mostrarEixo={false} />
                  </section>
                );
              })}
              {foraDosEixos.length > 0 && (
                <section className="cartao" aria-label="Fora do cálculo dos eixos">
                  <h3 className="cartao__titulo">
                    Fora do cálculo dos eixos <span className="texto-suave">({foraDosEixos.length})</span>
                  </h3>
                  <p className="nota">Elementos destacados no Mapa que não entram na fórmula de nenhum eixo (ex.: gargalo nas Bandas Ator e Externo).</p>
                  <TabelaDaMatriz linhas={foraDosEixos} mostrarEixo={false} />
                </section>
              )}
            </>
          )}
        </>
      )}
    </div>
  );
}
