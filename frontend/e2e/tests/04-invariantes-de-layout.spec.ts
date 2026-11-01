import { criarAresta, criarCaso, criarFluxo, criarNo } from '../support/api';
import { expect, test } from '../support/fixtures';

// POR QUE É E2E: o jsdom não faz layout (scrollWidth/clientHeight são sempre 0), então nenhum
// teste de componente consegue detectar estouro de página ou barra de rolagem indevida. Foi
// exatamente o bug real encontrado nos testes manuais: scroll vertical na barra de abas e nomes
// longos estourando a página. Um teste parametrizado cobre todas as telas por quase nenhum custo
// — inclusive as da Etapa 9 (Fluxos, Parâmetros), com nome de fluxo e justificativa no limite.

test('nomes longos sem espaço não geram scroll horizontal, e as abas não têm scroll vertical', async ({ page, request, prefixo }) => {
  const longo = 'x'.repeat(100); // limite de nome de Nó
  const fluxoLongo = 'f'.repeat(100); // limite de nome de Fluxo
  const justificativaLonga = 'j'.repeat(500); // limite de justificativa de Aresta (sem espaços)
  const nomeDoCaso = `${prefixo} ${'y'.repeat(120 - prefixo.length - 1)}`; // limite de nome de caso

  const caso = await criarCaso(request, nomeDoCaso, 'z'.repeat(300));
  const ator = await criarNo(request, caso.id, { nome: 'Cliente', tipo: 'Ator' });
  const tela = await criarNo(request, caso.id, { nome: longo, tipo: 'Touchpoint' });
  const processo = await criarNo(request, caso.id, { nome: 'Processar', tipo: 'Processo' });
  const externo = await criarNo(request, caso.id, { nome: 'Gateway externo', tipo: 'Externo' });
  await criarAresta(request, caso.id, { noOrigemId: ator.id, noDestinoId: tela.id, tipo: 'Sincrona', peso: 3, justificativa: justificativaLonga });
  await criarAresta(request, caso.id, { noOrigemId: tela.id, noDestinoId: processo.id, tipo: 'Critica', peso: 2, justificativa: 'A tela aciona o processamento.' });
  await criarAresta(request, caso.id, { noOrigemId: tela.id, noDestinoId: externo.id, tipo: 'Assincrona', peso: 1, justificativa: 'A tela notifica o gateway externo.' });
  await criarFluxo(request, caso.id, { nome: fluxoLongo, nosIds: [ator.id, tela.id, processo.id] });

  const base = `/casos/${caso.id}`;
  const prefixoEscapado = prefixo.replace(/[[\]]/g, '\\$&');
  // [rota, algo que só aparece depois que os dados da tela carregaram]
  const rotas: Array<[string, () => ReturnType<typeof page.locator>]> = [
    ['/', () => page.getByRole('link', { name: new RegExp(prefixoEscapado) })],
    [`${base}/nos`, () => page.getByRole('cell', { name: longo }).first()],
    [`${base}/arestas`, () => page.getByRole('cell', { name: longo }).first()],
    [`${base}/fluxos`, () => page.getByRole('heading', { name: fluxoLongo })],
    [`${base}/mapa`, () => page.getByRole('group', { name: /^x{20}/ })],
    [`${base}/radar`, () => page.getByRole('article').first()],
    [`${base}/recomendacao`, () => page.getByRole('heading', { level: 2 }).first()],
    [`${base}/rastreabilidade`, () => page.getByRole('cell', { name: longo }).first()],
    ['/perfis', () => page.getByLabel('Nome do estilo arquitetural').first()],
    ['/parametros', () => page.getByRole('heading', { name: 'Limites de normalização e pesos dos eixos' })],
  ];

  for (const [rota, pronto] of rotas) {
    await test.step(rota.replace(caso.id, ':id'), async () => {
      await page.goto(rota);
      await expect(pronto()).toBeVisible();

      const medidas = await page.evaluate(() => {
        const nav = document.querySelector('nav.etapas');
        return {
          larguraDoConteudo: document.documentElement.scrollWidth,
          larguraDaJanela: window.innerWidth,
          // "Rolável" = o usuário vê uma barra: overflow auto/scroll E conteúdo maior que a caixa. O item
          // ativo usa margin-bottom:-1px (scrollHeight 43 > clientHeight 42) de propósito; com
          // overflow-y:hidden isso é inofensivo — o bug original era overflow-y:auto implícito.
          etapasRolaveisVerticalmente: nav
            ? ['auto', 'scroll'].includes(getComputedStyle(nav).overflowY) && nav.scrollHeight > nav.clientHeight
            : false,
        };
      });

      expect(medidas.larguraDoConteudo, 'scroll horizontal na página').toBeLessThanOrEqual(medidas.larguraDaJanela);
      expect(medidas.etapasRolaveisVerticalmente, 'scroll vertical na barra de abas').toBe(false);
    });
  }
});
