import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation } from 'react-router';
import { alvoDaAjudaPara, idDoAlvo } from '../ajuda/contextoDaAjuda';
import { paraHtml, SECOES_DO_GLOSSARIO, slug } from '../ajuda/glossario';

function normalizar(texto: string) {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

// POP-09 seção 8.5 — botão "?" sempre visível; painel lateral com o glossário do projeto,
// aberto já no termo da tela atual, com busca.
export function AjudaFlutuante() {
  const [aberta, setAberta] = useState(false);
  const [busca, setBusca] = useState('');
  const { pathname } = useLocation();
  const corpoRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLInputElement>(null);
  const alvo = alvoDaAjudaPara(pathname);

  const secoes = useMemo(() => {
    const termo = normalizar(busca.trim());
    return SECOES_DO_GLOSSARIO.filter(
      (s) => !termo || normalizar(s.titulo).includes(termo) || normalizar(s.markdown).includes(termo),
    ).map((s) => ({ ...s, html: paraHtml(s.markdown) }));
  }, [busca]);

  useEffect(() => {
    if (!aberta) return;
    campoRef.current?.focus({ preventScroll: true });
    if (!alvo || busca) return;
    const el = corpoRef.current?.querySelector<HTMLElement>(`#${CSS.escape(idDoAlvo(alvo))}`);
    el?.scrollIntoView({ block: 'start' });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [aberta, pathname]);

  useEffect(() => {
    if (!aberta) return;
    const aoTeclar = (e: KeyboardEvent) => e.key === 'Escape' && setAberta(false);
    window.addEventListener('keydown', aoTeclar);
    return () => window.removeEventListener('keydown', aoTeclar);
  }, [aberta]);

  return (
    <>
      <button
        type="button"
        className="ajuda-botao"
        aria-label={aberta ? 'Fechar ajuda' : 'Abrir ajuda'}
        aria-expanded={aberta}
        onClick={() => setAberta((a) => !a)}
      >
        {aberta ? '×' : '?'}
      </button>
      {aberta && <div className="ajuda-fundo" onClick={() => setAberta(false)} aria-hidden="true" />}
      <aside className={`ajuda-painel${aberta ? ' ajuda-painel--aberto' : ''}`} aria-hidden={!aberta} aria-label="Ajuda">
        <header className="ajuda-painel__topo">
          <div className="ajuda-painel__titulo">Ajuda — termos do método</div>
          <input
            ref={campoRef}
            type="search"
            className="campo"
            placeholder="Buscar um termo (ex.: gargalo)"
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            tabIndex={aberta ? 0 : -1}
          />
        </header>
        <div className="ajuda-painel__corpo" ref={corpoRef}>
          {secoes.length === 0 && <p className="texto-suave">Nenhum termo encontrado para “{busca}”.</p>}
          {secoes.map((s) => (
            <section
              key={s.id}
              id={`ajuda-${slug(s.titulo)}`}
              className={`ajuda-secao${alvo?.secao === s.titulo ? ' ajuda-secao--destaque' : ''}`}
            >
              <h2>{s.titulo}</h2>
              <div className="conteudo-md" dangerouslySetInnerHTML={{ __html: s.html }} />
            </section>
          ))}
        </div>
      </aside>
    </>
  );
}
