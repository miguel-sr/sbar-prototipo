import { marked } from 'marked';
// Fonte única (POP-09 seção 8.5): o conteúdo da ajuda é o próprio docs/dominio/glossario.md,
// importado como texto no build. Atualizou o glossário, a ajuda muda junto.
import textoDoGlossario from '../../../docs/dominio/glossario.md?raw';

export interface Subsecao {
  id: string;
  titulo: string;
}

export interface SecaoDoGlossario {
  id: string;
  titulo: string;
  /** Markdown da seção inteira, incluindo as subseções (###). */
  markdown: string;
  subsecoes: Subsecao[];
}

export function slug(texto: string): string {
  return texto
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

/** Quebra o glossário em seções "## Termo", ignorando o título (#) e a nota de fonte (>). */
export function lerGlossario(markdown: string): SecaoDoGlossario[] {
  const secoes: SecaoDoGlossario[] = [];
  let atual: SecaoDoGlossario | null = null;
  let linhas: string[] = [];

  const fechar = () => {
    if (atual) {
      atual.markdown = linhas.join('\n').trim();
      secoes.push(atual);
    }
  };

  for (const linha of markdown.split(/\r?\n/)) {
    const h2 = /^##\s+(.+)$/.exec(linha);
    if (h2 && !linha.startsWith('###')) {
      fechar();
      const titulo = h2[1].trim();
      atual = { id: slug(titulo), titulo, markdown: '', subsecoes: [] };
      linhas = [];
      continue;
    }
    if (!atual) continue;
    const h3 = /^###\s+(.+)$/.exec(linha);
    if (h3) atual.subsecoes.push({ id: slug(h3[1].trim()), titulo: h3[1].trim() });
    linhas.push(linha);
  }
  fechar();
  return secoes;
}

/** Devolve só o corpo de uma subseção "### Título" (sem o próprio cabeçalho). */
export function lerSubsecao(markdown: string, titulo: string): string {
  const linhas = markdown.split(/\r?\n/);
  const inicio = linhas.findIndex((l) => l.replace(/^###\s+/, '').trim() === titulo && l.startsWith('###'));
  if (inicio < 0) return '';
  const resto = linhas.slice(inicio + 1);
  const fim = resto.findIndex((l) => /^#{2,3}\s/.test(l));
  return (fim < 0 ? resto : resto.slice(0, fim)).join('\n').trim();
}

/** Converte Markdown em HTML e dá id às subseções, para a ajuda poder rolar até elas. */
export function paraHtml(markdown: string): string {
  const html = marked.parse(markdown, { async: false, gfm: true }) as string;
  return html.replace(/<h3>(.*?)<\/h3>/g, (_, titulo: string) => {
    const texto = titulo.replace(/<[^>]+>/g, '');
    return `<h3 id="ajuda-${slug(texto)}">${titulo}</h3>`;
  });
}

export const glossarioBruto: string = textoDoGlossario;
export const SECOES_DO_GLOSSARIO: SecaoDoGlossario[] = lerGlossario(textoDoGlossario);
