import { useMemo, useState } from 'react';
import { CODIGO_TIPO_DE_ARESTA, COR_DA_BANDA, ROTULO_PESO, ROTULO_TIPO_DE_ARESTA } from '../dominio/rotulos';
import { TIPOS_DE_ARESTA, type Aresta, type TipoDeAresta } from '../tipos/Aresta';
import type { No } from '../tipos/No';
import { caminhoDaAresta, calcularLayout, GEOMETRIA } from './layoutDoMapa';

interface Props {
  nos: No[];
  arestas: Aresta[];
  gargalos: Set<string>;
  /** Índice (1…n) da comunidade de cada Nó; quando informado, desenha um selo "C#" discreto sobre o Nó. */
  comunidadePorNo?: Map<string, number>;
}

const TRACO: Record<TipoDeAresta, string | undefined> = {
  Sincrona: undefined,
  Critica: undefined,
  Assincrona: '7 5',
  Batch: '2 5',
};

function truncar(texto: string, max = 18) {
  return texto.length > max ? `${texto.slice(0, max - 1)}…` : texto;
}

// Desenho do Mapa de Interações (POP-09, componente MapaSvg): raias por Banda, Nós coloridos
// pela Banda, Arestas estilizadas pelo Tipo (espessura pelo Peso) e halo nos gargalos —
// a versão interativa da Figura 5 do TCC. Passe o mouse num Nó para destacar suas ligações.
export function MapaSvg({ nos, arestas, gargalos, comunidadePorNo }: Props) {
  const layout = useMemo(() => calcularLayout(nos, arestas), [nos, arestas]);
  const [emFoco, setEmFoco] = useState<string | null>(null);
  const g = GEOMETRIA;

  const ligadas = useMemo(() => {
    if (!emFoco) return null;
    const ids = new Set<string>([emFoco]);
    for (const a of arestas) {
      if (a.noOrigemId === emFoco) ids.add(a.noDestinoId);
      if (a.noDestinoId === emFoco) ids.add(a.noOrigemId);
    }
    return ids;
  }, [emFoco, arestas]);

  return (
    <div className="mapa">
      <div className="mapa__rolagem">
        <svg
          className="mapa__svg"
          viewBox={`0 0 ${layout.largura} ${layout.altura}`}
          // Encolhe até 75% para caber na tela; abaixo disso, rola na horizontal.
          style={{ minWidth: layout.largura * 0.75, maxWidth: layout.largura }}
          role="img"
          aria-label={`Mapa de Interações com ${nos.length} nós e ${arestas.length} arestas`}
        >
          <defs>
            {TIPOS_DE_ARESTA.map((t) => (
              <marker key={t} id={`seta-${t}`} viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
                <path d="M 0 0 L 10 5 L 0 10 z" className={`seta seta--${t.toLowerCase()}`} />
              </marker>
            ))}
          </defs>

          {layout.raias.map(({ banda, y, altura }, i) => (
            <g key={banda}>
              <rect x={g.margem} y={y} width={layout.largura - g.margem * 2} height={altura} className={`raia${i % 2 ? ' raia--alternada' : ''}`} />
              <rect x={g.margem} y={y} width={4} height={altura} fill={COR_DA_BANDA[banda]} />
              <text x={g.margem + 14} y={y + altura / 2} className="raia__rotulo" dominantBaseline="middle">
                {banda}
              </text>
            </g>
          ))}

          {arestas.map((a) => {
            const origem = layout.nos.get(a.noOrigemId);
            const destino = layout.nos.get(a.noDestinoId);
            if (!origem || !destino) return null;
            const { d } = caminhoDaAresta(origem, destino);
            const apagada = ligadas && !(a.noOrigemId === emFoco || a.noDestinoId === emFoco);
            return (
              <path
                key={a.id}
                d={d}
                className={`aresta aresta--${a.tipo.toLowerCase()}${apagada ? ' apagado' : ''}`}
                strokeWidth={0.8 + a.peso * 0.7}
                strokeDasharray={TRACO[a.tipo]}
                markerEnd={`url(#seta-${a.tipo})`}
              >
                <title>
                  {`${origem.no.nome} → ${destino.no.nome}\n${ROTULO_TIPO_DE_ARESTA[a.tipo]} (${CODIGO_TIPO_DE_ARESTA[a.tipo]}) · peso ${a.peso} (${ROTULO_PESO[a.peso]})`}
                </title>
              </path>
            );
          })}

          {[...layout.nos.values()].map(({ no, x, y }) => {
            const gargalo = gargalos.has(no.id);
            const comunidade = comunidadePorNo?.get(no.id);
            const apagado = ligadas && !ligadas.has(no.id);
            return (
              <g
                key={no.id}
                className={`no${apagado ? ' apagado' : ''}`}
                role="group"
                aria-label={`${no.nome}, ${no.tipo}, Banda ${no.banda}${gargalo ? ', gargalo' : ''}${comunidade !== undefined ? `, comunidade ${comunidade}` : ''}`}
                data-gargalo={gargalo ? 'true' : 'false'}
                transform={`translate(${x - g.larguraNo / 2}, ${y - g.alturaNo / 2})`}
                onMouseEnter={() => setEmFoco(no.id)}
                onMouseLeave={() => setEmFoco(null)}
                onFocus={() => setEmFoco(no.id)}
                onBlur={() => setEmFoco(null)}
                tabIndex={0}
              >
                <title>{`${no.nome}\n${no.tipo} · Banda ${no.banda}${gargalo ? '\nGargalo: carga acima de μ+σ da Banda' : ''}`}</title>
                {gargalo && <rect className="no__halo" x={-7} y={-7} width={g.larguraNo + 14} height={g.alturaNo + 14} rx={14} />}
                <rect className="no__caixa" width={g.larguraNo} height={g.alturaNo} rx={9} style={{ ['--cor-no' as string]: COR_DA_BANDA[no.banda] }} />
                <rect width={5} height={g.alturaNo} rx={2} fill={COR_DA_BANDA[no.banda]} />
                <text x={14} y={g.alturaNo / 2 - 6} className="no__nome" dominantBaseline="middle">
                  {truncar(no.nome)}
                </text>
                {comunidade !== undefined && (
                  <g className="no__comunidade" transform={`translate(${g.larguraNo - 34}, -8)`} aria-hidden="true">
                    <rect width={28} height={15} rx={7.5} />
                    <text x={14} y={8} textAnchor="middle" dominantBaseline="middle">
                      C{comunidade}
                    </text>
                  </g>
                )}
                <text x={14} y={g.alturaNo / 2 + 10} className="no__tipo" dominantBaseline="middle">
                  {no.tipo}
                  {gargalo ? ' · gargalo' : ''}
                </text>
              </g>
            );
          })}
        </svg>
      </div>

      <div className="legenda" aria-label="Legenda">
        {TIPOS_DE_ARESTA.map((t) => (
          <span key={t} className="legenda__item">
            <svg width="34" height="10" aria-hidden="true">
              <line x1="1" y1="5" x2="33" y2="5" className={`aresta aresta--${t.toLowerCase()}`} strokeWidth={2.4} strokeDasharray={TRACO[t]} />
            </svg>
            {ROTULO_TIPO_DE_ARESTA[t]} ({CODIGO_TIPO_DE_ARESTA[t]})
          </span>
        ))}
        <span className="legenda__item">
          <span className="legenda__halo" aria-hidden="true" />
          Gargalo
        </span>
        <span className="legenda__item texto-suave">Linha mais grossa = peso maior</span>
      </div>
    </div>
  );
}
