import { expect, test } from '../support/fixtures';

// POR QUE É E2E: o 404 de um Estudo de Caso vem da API REAL (ProblemDetails) e passa pelo
// roteador real; "id que não é GUID" também depende de a rota nem chegar à API. Em componente só
// dá para simular esses 404 com mocks (já cobertos lá); aqui garante-se que, de ponta a ponta, o
// usuário vê português, sem GUID cru, sem "Not Found." e sem um "Tentar de novo" inútil.

for (const [descricao, rota] of [
  ['Estudo de Caso inexistente (GUID válido)', '/casos/00000000-0000-0000-0000-000000000000/radar'],
  ['id que não é GUID', '/casos/nao-e-um-guid/mapa'],
] as const) {
  test(`${descricao}: mensagem em português, sem retry, com link de volta`, async ({ page }) => {
    await page.goto(rota);

    await expect(page.getByText('Estudo de Caso não encontrado')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Tentar de novo' })).toHaveCount(0);
    await expect(page.getByText(/not found/i)).toHaveCount(0);
    await expect(page.getByText(/[0-9a-f]{8}-[0-9a-f]{4}-/)).toHaveCount(0);

    await page.getByRole('link', { name: 'Voltar para os Estudos de Caso' }).click();
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { level: 1, name: 'Estudos de Caso' })).toBeVisible();
  });
}

test('rota desconhecida: página não encontrada com link de volta', async ({ page }) => {
  await page.goto('/qualquer-coisa-que-nao-existe');
  await expect(page.getByText('Página não encontrada')).toBeVisible();

  await page.getByRole('link', { name: 'Voltar para os Estudos de Caso' }).click();
  await expect(page).toHaveURL('/');
});
