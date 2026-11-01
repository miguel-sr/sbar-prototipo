interface Props {
  icone: 'lapis' | 'lixeira' | 'cima' | 'baixo';
  /** Texto completo da ação (ex.: "Editar Nó Carrinho") — vira o nome acessível e o tooltip. */
  rotulo: string;
  perigo?: boolean;
  onClick: () => void;
}

// Ícone no lugar de texto (reconhecimento em vez de memorização); o rótulo continua disponível
// para leitores de tela (aria-label) e ao passar o mouse (title).
export function BotaoDeIcone({ icone, rotulo, perigo = false, onClick }: Props) {
  return (
    <button
      type="button"
      className={`botao-icone${perigo ? ' botao-icone--perigo' : ''}`}
      aria-label={rotulo}
      title={rotulo}
      onClick={onClick}
    >
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        {icone === 'lapis' && (
          <>
            <path d="M12 20h9" />
            <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" />
          </>
        )}
        {icone === 'lixeira' && (
          <>
            <path d="M3 6h18" />
            <path d="M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2" />
            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
            <path d="M10 11v6M14 11v6" />
          </>
        )}
        {icone === 'cima' && <path d="M12 19V5M5 12l7-7 7 7" />}
        {icone === 'baixo' && <path d="M12 5v14M19 12l-7 7-7-7" />}
      </svg>
    </button>
  );
}
