import { randomUUID } from 'node:crypto';
import { expect, test, type Page } from '@playwright/test';
import { CODIGO_DE_CONVITE, SENHA } from '../support/setup-global';

// POR QUE É E2E: o que se prova é a soma de cookie de sessão HttpOnly + proxy do navegador + política de
// autorização da API + filtro por dono no banco. Nenhuma camada de baixo enxerga isso junta: o Vitest simula a
// API e os testes da Api não têm navegador nem o cookie real. As regras finas (404 em cada rota, parâmetros por
// usuário, convite) já estão nos testes da Api; aqui só o caminho completo do usuário.
//
// Sai do estado compartilhado (conta E2E padrão) de propósito: começa sem sessão e cria duas contas novas.
test.use({ storageState: { cookies: [], origins: [] } });

async function cadastrar(page: Page, email: string) {
  await page.goto('/cadastrar');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel(/^Senha/).fill(SENHA);
  await page.getByLabel('Código de convite').fill(CODIGO_DE_CONVITE);
  await page.getByRole('button', { name: 'Criar conta' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Estudos de Caso' })).toBeVisible();
}

test('cada conta vê só os seus Estudos de Caso; sem sessão, tudo leva ao login', async ({ page }) => {
  const id = randomUUID().slice(0, 8);
  const nomeDoCaso = `[E2E ${id}] Caso da primeira conta`;

  // Sem sessão: a rota protegida leva ao login e guarda a origem.
  await page.goto('/parametros');
  await expect(page).toHaveURL(/\/entrar\?de=%2Fparametros/);

  await cadastrar(page, `primeira-${id}@sbar.test`);

  await page.getByRole('link', { name: 'Novo Estudo de Caso' }).click();
  await page.getByRole('textbox', { name: 'Nome do sistema' }).fill(nomeDoCaso);
  await page.getByRole('button', { name: 'Criar e cadastrar Nós' }).click();
  await expect(page).toHaveURL(/\/casos\/[0-9a-f-]{36}\/nos/);
  const urlDoCaso = page.url();

  // Sair encerra a sessão: a mesma URL agora leva ao login.
  await page.getByRole('button', { name: 'Sair' }).click();
  await expect(page).toHaveURL(/\/entrar/);
  await page.goto(urlDoCaso);
  await expect(page).toHaveURL(/\/entrar\?de=/);

  // Outra conta: lista vazia e o caso da primeira é "não encontrado" (404, sem revelar que existe).
  await cadastrar(page, `segunda-${id}@sbar.test`);
  await expect(page.getByText(nomeDoCaso)).toHaveCount(0);
  await page.goto(urlDoCaso);
  await expect(page.getByText('Estudo de Caso não encontrado')).toBeVisible();

  // Limpeza: o caso é da primeira conta, então entra de volta nela para apagá-lo (a limpeza padrão usa a conta E2E).
  await page.goto('/');
  await page.getByRole('button', { name: 'Sair' }).click();
  await page.goto('/entrar');
  await page.getByLabel('E-mail').fill(`primeira-${id}@sbar.test`);
  await page.getByLabel('Senha').fill(SENHA);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByText(nomeDoCaso)).toBeVisible();
  const casoId = urlDoCaso.match(/\/casos\/([0-9a-f-]{36})/)![1];
  expect((await page.request.delete(`/api/casos/${casoId}`)).ok()).toBe(true);
});
