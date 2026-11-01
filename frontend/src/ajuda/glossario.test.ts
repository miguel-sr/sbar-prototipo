import { describe, expect, it } from 'vitest';
import { glossarioBruto, lerSubsecao, paraHtml, SECOES_DO_GLOSSARIO } from './glossario';
import { alvoDaAjudaPara, idDoAlvo } from './contextoDaAjuda';

describe('glossário (fonte única da ajuda)', () => {
  it('lê as seções do docs/dominio/glossario.md', () => {
    const titulos = SECOES_DO_GLOSSARIO.map((s) => s.titulo);
    expect(titulos).toContain('Nó');
    expect(titulos).toContain('Aresta');
    expect(titulos).toContain('Radar Arquitetural');
  });

  it('todo alvo contextual aponta para uma seção/subseção que existe no glossário', () => {
    const caminhos = ['/casos/x/nos', '/casos/x/arestas', '/casos/x/fluxos', '/casos/x/mapa', '/casos/x/radar', '/casos/x/recomendacao', '/casos/x/rastreabilidade', '/perfis', '/parametros'];
    const html = SECOES_DO_GLOSSARIO.map((s) => `<section id="ajuda-${s.id}">${paraHtml(s.markdown)}</section>`).join('');
    for (const caminho of caminhos) {
      const alvo = alvoDaAjudaPara(caminho);
      expect(alvo, caminho).not.toBeNull();
      expect(html, `${caminho} → ${idDoAlvo(alvo!)}`).toContain(`id="${idDoAlvo(alvo!)}"`);
    }
  });

  it('extrai o critério de peso usado como texto de apoio na T4', () => {
    const criterio = lerSubsecao(glossarioBruto, 'Peso da ligação');
    expect(criterio).toMatch(/\| \*\*3\*\* \| Alto \|/);
    expect(criterio).not.toMatch(/## Carga/);
  });
});
