import { BANDAS, type No } from '../tipos/No';

interface Props {
  id: string;
  rotulo: string;
  nos: No[];
  valor: string;
  aoMudar: (valor: string) => void;
  /** Campo obrigatório do formulário (padrão). Desligue quando o seletor só escolhe o próximo item de uma lista. */
  obrigatorio?: boolean;
  /** Texto da opção vazia. */
  vazio?: string;
}

// Lista de Nós agrupada por Banda (na ordem do Service Blueprint), usada nos formulários de Aresta e de Fluxo.
export function SeletorDeNo({ id, rotulo, nos, valor, aoMudar, obrigatorio = true, vazio = 'Escolha um Nó…' }: Props) {
  return (
    <>
      <label className="rotulo" htmlFor={id}>
        {rotulo}
      </label>
      <select id={id} className="campo" value={valor} onChange={(e) => aoMudar(e.target.value)} required={obrigatorio}>
        <option value="">{vazio}</option>
        {BANDAS.map((banda) => {
          const daBanda = nos.filter((n) => n.banda === banda);
          if (daBanda.length === 0) return null;
          return (
            <optgroup key={banda} label={banda}>
              {daBanda.map((n) => (
                <option key={n.id} value={n.id}>
                  {n.nome}
                </option>
              ))}
            </optgroup>
          );
        })}
      </select>
    </>
  );
}
