import { COR_DA_BANDA } from '../dominio/rotulos';
import type { Banda } from '../tipos/No';

export function ChipDeBanda({ banda }: { banda: Banda }) {
  return (
    <span className="chip" style={{ ['--cor-chip' as string]: COR_DA_BANDA[banda] }}>
      <span className="chip__ponto" aria-hidden="true" />
      {banda}
    </span>
  );
}
