import { criarCaso, criarNo } from '../support/api';
import { expect, test } from '../support/fixtures';

// POR QUE É E2E: o PUT precisa chegar ao banco e voltar certo — a Banda do Nó é re-derivada do
// Tipo pelo BACKEND (RegraDeBanda) e a tela reagrupa por Banda com o que a API devolve; um teste
// de componente com a API mockada não prova nada disso. Persistência provada com reload.
// (Regra Tipo → Banda: testada no backend; validação de formulário: testada em componente.)

test('editar o tipo do nó muda a Banda dele; editar o nome do caso persiste', async ({ page, request, prefixo }) => {
  const caso = await criarCaso(request, `${prefixo} Edição`);
  await criarNo(request, caso.id, { nome: 'Cliente', tipo: 'Ator' });
  await criarNo(request, caso.id, { nome: 'Etapa de pagamento', tipo: 'Touchpoint' });

  await page.goto(`/casos/${caso.id}/nos`);
  const grupo = (banda: string) => page.locator('.grupo-banda').filter({ hasText: banda });
  await expect(grupo('Frontstage')).toContainText('Etapa de pagamento');

  await test.step('trocar Touchpoint por Processo move o nó de Frontstage para Backstage', async () => {
    await page.getByRole('button', { name: 'Editar Etapa de pagamento' }).click();
    await expect(page.getByRole('heading', { name: 'Editar Nó' })).toBeVisible();
    await page.getByRole('radio', { name: /^Processo/ }).check();
    await page.getByRole('button', { name: 'Salvar alterações' }).click();

    await expect(grupo('Backstage')).toContainText('Etapa de pagamento');
    await expect(grupo('Frontstage')).toHaveCount(0);
  });

  await test.step('editar o nome do Estudo de Caso persiste após recarregar', async () => {
    const novoNome = `${prefixo} Edição renomeada`;
    await page.getByRole('button', { name: 'Editar Estudo de Caso' }).click();
    await page.getByLabel('Nome do sistema').fill(novoNome);
    await page.getByRole('button', { name: 'Salvar alterações' }).click();
    await expect(page.getByRole('heading', { level: 1, name: novoNome })).toBeVisible();

    await page.reload();
    await expect(page.getByRole('heading', { level: 1, name: novoNome })).toBeVisible();
    await expect(grupo('Backstage')).toContainText('Etapa de pagamento');
  });
});
