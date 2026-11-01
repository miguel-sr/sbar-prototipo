import { expect, test } from '../support/fixtures';

// POR QUE É E2E: a tela de Parâmetros é preenchida com o que a API REAL devolve — a versão vigente
// (criada sob demanda), os limites/pesos do seed (Quadro 7, p_i = 0,1) e as assinaturas do
// Quadro 8 que a MIGRATION de seed grava. Nenhum teste de componente vê migration → API → tela.
// O bloqueio de Σ pesos ≠ 1 (RN07, CT10) é só conferido em cima desses dados reais; a regra em si
// está testada no backend e em componente. NÃO SALVA nada: parâmetros e perfis são GLOBAIS (um
// salvamento criaria uma versão que vazaria para os outros testes e para o banco do E2E).

test('Parâmetros: mostra a versão vigente e as assinaturas do Quadro 8; soma dos pesos ≠ 1 bloqueia o salvar', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Parâmetros' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Parâmetros do cálculo' })).toBeVisible();
  await expect(page.getByTestId('versao-exibida')).toContainText(/Versão vigente: \d+/);

  await test.step('as assinaturas vêm do seed do Quadro 8 (provisórias; VOL de Microsserviços = 2,0)', async () => {
    const assinaturas = page.getByRole('table', { name: 'Assinaturas de referência' });
    // colunas: perfil, CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL (índice 9), DEP
    await expect(assinaturas.getByRole('row', { name: /^Monólito Modular \(provisório\)/ }).getByRole('cell').nth(9)).toHaveText('3,0');
    await expect(assinaturas.getByRole('row', { name: /^Microsserviços \(provisório\)/ }).getByRole('cell').nth(9)).toHaveText('2,0');
  });

  await test.step('com os pesos do seed (soma 1) não há o que salvar', async () => {
    await expect(page.getByTestId('soma-dos-pesos')).toHaveText('1,0000');
    await expect(page.getByRole('button', { name: 'Salvar como nova versão' })).toBeDisabled();
  });

  await test.step('mudar um peso para soma ≠ 1 mostra o erro e mantém o salvar bloqueado, mesmo com descrição', async () => {
    await page.getByLabel('Peso de CO', { exact: true }).fill('0,2');
    await page.getByLabel('O que mudou (descrição da nova versão)').fill('Teste de bloqueio (não deve salvar)');

    await expect(page.getByTestId('soma-dos-pesos')).toHaveText('1,1000');
    await expect(page.getByRole('alert').filter({ hasText: 'Soma dos pesos igual a 1,1000; deve ser 1' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Salvar como nova versão' })).toBeDisabled();
  });

  await test.step('recarregar descarta o rascunho: nada foi gravado', async () => {
    await page.reload();
    await expect(page.getByTestId('soma-dos-pesos')).toHaveText('1,0000');
    await expect(page.getByLabel('Peso de CO', { exact: true })).toHaveValue('0.1');
  });
});
