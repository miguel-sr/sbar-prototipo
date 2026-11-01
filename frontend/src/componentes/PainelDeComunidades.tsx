import { estruturaDeComunidadesEhSignificativa } from '../dominio/comunidades';
import { formatarNumero } from '../dominio/rotulos';
import type { Comunidade } from '../tipos/Metricas';
import type { No } from '../tipos/No';

interface Props {
  comunidades: Comunidade[];
  modularidadeQ: number | null;
  nos: No[];
}

// Comunidades estruturais e modularidade Q (TCC 4.4.1; RF06). FLEX e AUT usam esta partição:
// "saídas externas" é o que AUT conta (autônoma = no máximo uma saída para outra comunidade).
export function PainelDeComunidades({ comunidades, modularidadeQ, nos }: Props) {
  const nomeDo = (id: string) => nos.find((n) => n.id === id)?.nome ?? '?';
  const significativa = estruturaDeComunidadesEhSignificativa(modularidadeQ);

  return (
    <section className="cartao painel" aria-labelledby="titulo-comunidades">
      <div className="painel__topo">
        <h3 id="titulo-comunidades" className="cartao__titulo">
          Comunidades estruturais
        </h3>
        <span className="painel__q">
          Modularidade Q ={' '}
          <strong className="numero">{modularidadeQ === null ? 'indefinida' : formatarNumero(modularidadeQ, 4)}</strong>
        </span>
        {modularidadeQ !== null && (
          <span className={`selo-estado ${significativa ? 'selo-estado--ok' : 'selo-estado--alerta'}`}>
            {significativa ? 'estrutura significativa' : 'estrutura pouco significativa'}
          </span>
        )}
      </div>
      <p className="texto-suave painel__descricao">
        Grupos de Nós mais conectados entre si do que com o resto do Mapa (detecção por modularidade). Servem de base para Flexibilidade
        (FLEX) e Autonomia (AUT).
      </p>
      {comunidades.length === 0 ? (
        <p className="texto-suave">Nenhuma comunidade identificada.</p>
      ) : (
        <ul className="comunidades">
          {comunidades.map((c) => (
            <li key={c.indice} className="comunidade">
              <span className="comunidade__rotulo">C{c.indice}</span>
              <span className="comunidade__nos">{c.nosIds.map(nomeDo).join(', ')}</span>
              <span className="comunidade__saidas texto-suave">
                {c.saidasExternas} {c.saidasExternas === 1 ? 'saída externa' : 'saídas externas'}
                {c.saidasExternas <= 1 && <span className="comunidade__autonoma"> · autônoma</span>}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
