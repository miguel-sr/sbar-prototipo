import type { ReactNode } from 'react';
import { LogoSbar } from './LogoSbar';

// Moldura das telas fora do app (Entrar e Cadastrar): marca do SBAR no alto e um cartão centralizado.
export function PaginaDeConta({ titulo, descricao, children }: { titulo: string; descricao: string; children: ReactNode }) {
  return (
    <main className="pagina-conta">
      <div className="pagina-conta__marca">
        <LogoSbar tamanho={56} />
        <span className="marca__nome">SBAR</span>
        <span className="marca__sub">Service Blueprint Architectural Recommendation</span>
      </div>
      <section className="pagina-conta__cartao">
        <h1 className="titulo-pagina">{titulo}</h1>
        <p className="texto-suave">{descricao}</p>
        {children}
      </section>
    </main>
  );
}
