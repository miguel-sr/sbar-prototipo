import type { Ref } from 'react';

interface Props {
  id: string;
  valor: string;
  aoMudar: (valor: string) => void;
  max: number;
  placeholder?: string;
  multilinha?: boolean;
  autoFocus?: boolean;
  obrigatorio?: boolean;
  inputRef?: Ref<HTMLInputElement>;
}

// Campo de texto com maxLength e contador "n/max" — o backend recusa (422) texto acima do limite,
// então o formulário avisa antes. O contador fica em destaque perto do limite.
export function CampoComContador({ id, valor, aoMudar, max, placeholder, multilinha, autoFocus, obrigatorio, inputRef }: Props) {
  const perto = valor.length >= max * 0.9;
  return (
    <>
      {multilinha ? (
        <textarea
          id={id}
          className="campo"
          rows={3}
          value={valor}
          onChange={(e) => aoMudar(e.target.value)}
          placeholder={placeholder}
          maxLength={max}
          aria-describedby={`${id}-contador`}
        />
      ) : (
        <input
          id={id}
          ref={inputRef}
          className="campo"
          value={valor}
          onChange={(e) => aoMudar(e.target.value)}
          placeholder={placeholder}
          required={obrigatorio}
          autoFocus={autoFocus}
          maxLength={max}
          aria-describedby={`${id}-contador`}
        />
      )}
      <span id={`${id}-contador`} className={`contador${perto ? ' contador--limite' : ''}`}>
        {valor.length}/{max}
      </span>
    </>
  );
}
