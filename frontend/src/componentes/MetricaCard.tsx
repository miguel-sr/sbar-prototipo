import { useState } from 'react';
import { FORMULA_DE_NORMALIZACAO, INFO_DO_EIXO } from '../dominio/eixos';
import { formatarNumero } from '../dominio/rotulos';
import type { EixoResultado } from '../tipos/Metricas';

// Um cartão por eixo (POP-09, componente MetricaCard): valor normalizado, valor bruto e um
// "ver cálculo" que abre o que o eixo avalia e a fórmula usada (TCC seção 4.4.2).
export function MetricaCard({ resultado }: { resultado: EixoResultado }) {
  const [aberto, setAberto] = useState(false);
  const info = INFO_DO_EIXO[resultado.eixo];
  const normalizado = resultado.valorNormalizado ?? 0;

  return (
    <article className={`metrica${resultado.aplicavel ? '' : ' metrica--na'}${aberto ? ' metrica--aberta' : ''}`}>
      <header className="metrica__topo">
        <span className="metrica__codigo">{resultado.eixo}</span>
        <span className="metrica__nome">{info.nome}</span>
      </header>
      {resultado.aplicavel ? (
        <>
          <div className="metrica__valor">
            {formatarNumero(normalizado)}
            <span className="metrica__escala"> / 5</span>
          </div>
          <div className="barra" aria-hidden="true">
            <div className="barra__preenchida" style={{ width: `${(normalizado / 5) * 100}%` }} />
          </div>
          <p className="metrica__bruto">
            Valor bruto: <span className="numero">{formatarNumero(resultado.valorBruto ?? 0, 4)}</span>
          </p>
        </>
      ) : (
        <>
          <div className="metrica__valor metrica__valor--na">Não aplicável</div>
          <p className="metrica__motivo">{resultado.motivo}</p>
        </>
      )}
      <button type="button" className="link-botao" aria-expanded={aberto} onClick={() => setAberto((a) => !a)}>
        {aberto ? 'ocultar cálculo' : 'ver cálculo'}
      </button>
      {aberto && (
        <div className="metrica__calculo">
          <p>{info.avalia}</p>
          <p className="rotulo-mini">Fórmula</p>
          <code className="formula">{info.formula}</code>
          {resultado.aplicavel && (
            <>
              <p className="rotulo-mini">Resultado</p>
              <p>
                {resultado.eixo} = <span className="numero">{formatarNumero(resultado.valorBruto ?? 0, 4)}</span> → normalizado{' '}
                <span className="numero">{formatarNumero(normalizado)}</span>
              </p>
              <code className="formula">{FORMULA_DE_NORMALIZACAO}</code>
            </>
          )}
          {info.limitacao && <p className="metrica__limitacao">{info.limitacao}</p>}
        </div>
      )}
    </article>
  );
}
