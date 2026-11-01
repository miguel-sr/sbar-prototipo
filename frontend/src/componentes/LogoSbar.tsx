// Ícone do SBAR (o mesmo de public/favicon.svg): hexágono do Radar sobre o fundo da cor primária.
export function LogoSbar({ tamanho = 28 }: { tamanho?: number }) {
  return (
    <svg viewBox="0 0 32 32" width={tamanho} height={tamanho} aria-hidden="true" focusable="false">
      <rect width="32" height="32" rx="7" fill="var(--primaria)" />
      <polygon points="16,5 26,12 22.5,25 9.5,25 6,12" fill="none" stroke="var(--destaque)" strokeWidth="2.4" strokeLinejoin="round" />
      <circle cx="16" cy="16" r="2.6" fill="#fff" />
    </svg>
  );
}
