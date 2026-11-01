import { cadastrarClinicaPelaApi, GARGALOS_ESPERADOS, NO_ATOR, QUANTIDADE_DE_FLUXOS } from '../support/clinica';
import { expect, test } from '../support/fixtures';

// POR QUE É E2E: é o único teste que atravessa front + API + cálculo + banco com um caso real
// (Estudo de Caso 2 do TCC revisado). Pega o que nenhuma camada de baixo vê: o front mapeando
// errado o contrato da API (eixo → cartão, `nosGargalo` → destaque no Mapa, comunidades/Q, margem
// e distâncias do perfil mais próximo, categorias da matriz, contagens da home) e a API devolvendo
// algo que a UI não sabe exibir. As FÓRMULAS não são testadas aqui — são do backend (xUnit); este
// teste só confere que a UI mostra, em pt-BR, os números publicados no TCC revisado (Quadros 35 e
// 37; fluxos F1–F4b como entrada) e que não há erro de integração.
//
// Ordem de cadastro: o Paciente nasce pela interface e os demais nós vêm na ordem do Quadro 31 (a mesma dos
// quadros do TCC). As comunidades NÃO dependem dela — o TCC explora todos os empates de modularidade e adota a
// divisão de maior Q —; ela só mantém a numeração C1…C4 igual à dos quadros.

const EIXOS_ESPERADOS: Array<[string, string]> = [
  ['CO', '2,79'],
  ['ESC', '3,75'],
  ['INT', '1,19'],
  ['UX', '2,08'],
  ['DISP', '3,75'],
  ['FLEX', '2,76'],
  ['TR', '0,33'],
  ['AUT', '2,50'],
  ['VOL', '2,86'],
  ['DEP', '3,75'],
];

test('Estudo de Caso 2 (Clínica): do cadastro ao perfil mais próximo e à rastreabilidade', async ({ page, request, prefixo }) => {
  const nome = `${prefixo} Agendamento de Clínica`;

  await test.step('cria o Estudo de Caso e o nó Ator pela interface', async () => {
    await page.goto('/');
    await page.getByRole('link', { name: '+ Novo Estudo de Caso' }).click();
    await page.getByLabel('Nome do sistema').fill(nome);
    await page.getByRole('button', { name: 'Criar e cadastrar Nós' }).click();
    await page.waitForURL(/\/casos\/[0-9a-f-]{36}\/nos$/);

    await page.getByLabel('Nome', { exact: true }).fill(NO_ATOR);
    await page.getByRole('radio', { name: /^Ator/ }).check();
    await page.getByRole('button', { name: 'Adicionar Nó' }).click();
    await expect(page.getByRole('heading', { name: 'Nós cadastrados (1)' })).toBeVisible();
  });

  const casoId = page.url().match(/\/casos\/([0-9a-f-]{36})\//)![1];
  await cadastrarClinicaPelaApi(request, casoId);

  await test.step('a home mostra as contagens do caso', async () => {
    await page.goto('/');
    await expect(page.getByRole('link', { name: new RegExp(nome.replace(/[[\]]/g, '\\$&')) })).toContainText('15 nós · 21 arestas');
  });

  await test.step('a aba Fluxos lista os 5 fluxos principais, todos válidos', async () => {
    await page.goto(`/casos/${casoId}/fluxos`);
    await expect(page.getByRole('heading', { name: `Fluxos principais (${QUANTIDADE_DE_FLUXOS})` })).toBeVisible();
    await expect(page.getByText('válido', { exact: true })).toHaveCount(QUANTIDADE_DE_FLUXOS);
    await expect(page.getByText('inválido', { exact: true })).toHaveCount(0);
  });

  await test.step('o Radar mostra os 10 eixos normalizados e as 4 comunidades (Q ≈ 0,34)', async () => {
    await page.goto(`/casos/${casoId}/radar`);
    for (const [eixo, valor] of EIXOS_ESPERADOS) {
      const cartao = page.getByRole('article').filter({ has: page.getByText(eixo, { exact: true }) });
      await expect(cartao, `eixo ${eixo}`).toContainText(`${valor} / 5`);
    }

    const comunidades = page.getByRole('region', { name: 'Comunidades estruturais' });
    await expect(comunidades).toContainText(/Modularidade Q = 0,34\d\d/);
    await expect(comunidades.getByRole('listitem')).toHaveCount(4);
  });

  await test.step('o perfil mais próximo é Monólito Modular, com margem pequena, e as distâncias do TCC', async () => {
    await page.goto(`/casos/${casoId}/recomendacao`);
    await expect(page.getByRole('heading', { level: 2, name: /^Monólito Modular/ })).toBeVisible();
    await expect(page.getByRole('listitem').filter({ hasText: /^Monólito Modular/ })).toContainText('1,107');
    await expect(page.getByRole('listitem').filter({ hasText: /^Microsserviços/ })).toContainText('1,346');
    await expect(page.getByTestId('margem')).toContainText('0,239');
    await expect(page.getByText('não substitui a análise do arquiteto').first()).toBeVisible();
  });

  await test.step('o Mapa destaca exatamente os 3 gargalos (inclusive o de Frontstage)', async () => {
    await page.goto(`/casos/${casoId}/mapa`);
    await expect(page.getByRole('group', { name: /, gargalo$/ })).toHaveCount(3);
    for (const nomeDoNo of GARGALOS_ESPERADOS) {
      await expect(page.getByRole('group', { name: new RegExp(`^${nomeDoNo}, .*, gargalo$`) })).toBeVisible();
    }
  });

  await test.step('a Matriz de Rastreabilidade lista, por eixo, os elementos que contribuíram', async () => {
    await page.goto(`/casos/${casoId}/rastreabilidade`);
    const eixo = (sigla: string) => page.getByRole('region', { name: `Eixo ${sigla}` });

    await expect(eixo('DISP')).toContainText('Registro do Agendamento');
    await expect(eixo('DISP')).toContainText('Repositório de Agendas');
    await expect(eixo('UX')).toContainText('Tela de Seleção de Horário'); // gargalo de Frontstage: alimenta UX, não DISP
    await expect(eixo('TR')).toContainText('Check-in da Consulta'); // única aresta crítica
    await expect(eixo('INT')).toContainText('Gateway SMS/E-mail');
    await expect(eixo('CO')).toContainText('Banda Backstage'); // CO por Banda ocupada
    await expect(eixo('ESC')).toContainText('pares possíveis');
    await expect(eixo('VOL')).toContainText('soma');
    await expect(eixo('FLEX')).toBeVisible();
    await expect(eixo('AUT')).toBeVisible();
    await expect(eixo('DEP')).toBeVisible();
  });
});
