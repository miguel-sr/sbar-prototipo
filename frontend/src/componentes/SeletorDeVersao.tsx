import { formatarData } from '../dominio/rotulos';
import type { VersaoDeParametros } from '../tipos/Parametros';

interface Props {
  id: string;
  /** Versões existentes (qualquer ordem); a de maior número é a vigente. */
  versoes: VersaoDeParametros[];
  selecionada: number;
  aoMudar: (numero: number) => void;
}

// Seletor da versão dos parâmetros (limites, pesos e assinaturas): permite reaplicar uma versão antiga
// aos mesmos dados do Mapa e comparar o resultado (TCC 4.6.1 e protocolo 5.4; RF11, US06).
export function SeletorDeVersao({ id, versoes, selecionada, aoMudar }: Props) {
  const ordenadas = [...versoes].sort((a, b) => b.numero - a.numero);
  const vigente = ordenadas[0]?.numero;
  return (
    <div className="seletor-de-versao">
      <label htmlFor={id} className="seletor-de-versao__rotulo">
        Versão dos parâmetros
      </label>
      <select id={id} className="campo" value={selecionada} onChange={(e) => aoMudar(Number(e.target.value))}>
        {ordenadas.map((v) => (
          <option key={v.numero} value={v.numero}>
            {`Versão ${v.numero}${v.numero === vigente ? ' (vigente)' : ''} — ${v.descricao} · ${formatarData(v.criadaEm)}`}
          </option>
        ))}
      </select>
    </div>
  );
}
