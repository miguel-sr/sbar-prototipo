import { CODIGO_TIPO_DE_ARESTA, ROTULO_TIPO_DE_ARESTA } from '../dominio/rotulos';
import type { TipoDeAresta } from '../tipos/Aresta';

export function SeloDeAresta({ tipo }: { tipo: TipoDeAresta }) {
  return (
    <span className={`selo selo--${tipo.toLowerCase()}`} title={ROTULO_TIPO_DE_ARESTA[tipo]}>
      {CODIGO_TIPO_DE_ARESTA[tipo]}
    </span>
  );
}
