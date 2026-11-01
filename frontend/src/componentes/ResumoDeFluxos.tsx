import { Link } from 'react-router';
import type { FluxoDasMetricas } from '../tipos/Metricas';

// Fluxos principais que alimentam o DEP: os válidos entram na média; os inválidos ficam de fora (US03).
export function ResumoDeFluxos({ fluxos }: { fluxos: FluxoDasMetricas[] }) {
  const validos = fluxos.filter((f) => f.valido);
  const invalidos = fluxos.filter((f) => !f.valido);

  return (
    <section className="cartao painel" aria-labelledby="titulo-fluxos-dep">
      <h3 id="titulo-fluxos-dep" className="cartao__titulo">
        Fluxos principais (base do DEP)
      </h3>
      {fluxos.length === 0 ? (
        <p className="texto-suave">
          Nenhum fluxo cadastrado — o eixo DEP fica “não aplicável”. <Link to="../fluxos">Cadastrar fluxos</Link>
        </p>
      ) : (
        <>
          <p className="texto-suave painel__descricao">
            {validos.length} {validos.length === 1 ? 'fluxo válido entra' : 'fluxos válidos entram'} no cálculo.
            {invalidos.length > 0 && (
              <>
                {' '}
                {invalidos.length} {invalidos.length === 1 ? 'inválido ficou' : 'inválidos ficaram'} de fora —{' '}
                <Link to="../fluxos">corrigir</Link>.
              </>
            )}
          </p>
          <ul className="resumo-de-fluxos">
            {fluxos.map((f) => (
              <li key={f.id}>
                <span className={`selo-estado ${f.valido ? 'selo-estado--ok' : 'selo-estado--invalido'}`}>
                  {f.valido ? 'válido' : 'inválido'}
                </span>
                <span>{f.nome}</span>
                <span className="texto-suave">
                  {f.bandasPercorridas} {f.bandasPercorridas === 1 ? 'banda' : 'bandas'}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}
