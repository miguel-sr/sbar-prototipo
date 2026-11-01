import { formatarNumero } from '../dominio/rotulos';
import type { EstatisticasDaBanda } from '../tipos/Metricas';
import type { No } from '../tipos/No';
import { ChipDeBanda } from './ChipDeBanda';

interface Props {
  bandas: EstatisticasDaBanda[];
  nos: No[];
  nosGargalo: string[];
}

// μ, σ e limiar μ + σ das cargas de cada Banda, com os Nós que passam do limiar (TCC 4.2.6; US04).
// Um Nó é "potencialmente crítico" (Gargalo) quando carga(v) > μ + σ da própria Banda.
export function TabelaDeBandas({ bandas, nos, nosGargalo }: Props) {
  if (bandas.length === 0) return null;
  const gargalos = new Set(nosGargalo);

  return (
    <section className="cartao painel" aria-labelledby="titulo-bandas">
      <h3 id="titulo-bandas" className="cartao__titulo">
        Carga por Banda (μ, σ e limiar)
      </h3>
      <p className="texto-suave painel__descricao">
        Um Nó é potencialmente crítico (Gargalo) quando sua carga é <strong>maior</strong> que μ + σ da própria Banda.
      </p>
      <div className="tabela-rolagem">
        <table className="tabela">
          <thead>
            <tr>
              <th>Banda</th>
              <th>Nós</th>
              <th>
                <span className="simbolo-grego">μ</span> (média)
              </th>
              <th>
                <span className="simbolo-grego">σ</span> (desvio)
              </th>
              <th>
                Limiar <span className="simbolo-grego">μ</span> + <span className="simbolo-grego">σ</span>
              </th>
              <th>Potencialmente críticos</th>
            </tr>
          </thead>
          <tbody>
            {bandas.map((b) => {
              const criticos = nos.filter((n) => n.banda === b.banda && gargalos.has(n.id)).map((n) => n.nome);
              return (
                <tr key={b.banda}>
                  <td>
                    <ChipDeBanda banda={b.banda} />
                  </td>
                  <td className="numero">{b.quantidadeDeNos}</td>
                  <td className="numero">{formatarNumero(b.media)}</td>
                  <td className="numero">{formatarNumero(b.desvioPadrao)}</td>
                  <td className="numero">{formatarNumero(b.limiar)}</td>
                  <td>{criticos.length === 0 ? <span className="texto-suave">nenhum</span> : criticos.join(', ')}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}
