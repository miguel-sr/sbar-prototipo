import { criarAresta, criarCaso, criarFluxo, criarNo } from '../support/api';
import { expect, test } from '../support/fixtures';

// POR QUE É E2E: (1) o <dialog> nativo — Esc, foco e fundo — não existe no jsdom (o Vitest só tem
// um polyfill de abrir/fechar, de propósito); (2) a cascata (excluir o Nó remove as Arestas
// ligadas) atravessa front, API e banco, e precisa continuar valendo após recarregar a página;
// (3) o fluxo principal que passava pelo Nó NÃO é apagado em silêncio: a API o marca como
// inválido e a aba Fluxos sinaliza a etapa (US03, CT09) — de novo só se prova com a API real.
// A REGRA da cascata/invalidação é testada no backend; o aviso de "N arestas" e o fluxo do modal
// são testados em componente. Aqui só o que depende de navegador + persistência reais.

test('excluir um nó com arestas: Esc e Cancelar não excluem; Confirmar remove o nó e suas arestas e invalida o fluxo', async ({ page, request, prefixo }) => {
  const caso = await criarCaso(request, `${prefixo} Pedidos`);
  const cliente = await criarNo(request, caso.id, { nome: 'Cliente', tipo: 'Ator' });
  const portal = await criarNo(request, caso.id, { nome: 'Portal', tipo: 'Touchpoint' });
  const pedido = await criarNo(request, caso.id, { nome: 'Registrar pedido', tipo: 'Processo' });
  const gateway = await criarNo(request, caso.id, { nome: 'Gateway de pagamento', tipo: 'Externo' });
  await criarAresta(request, caso.id, { noOrigemId: cliente.id, noDestinoId: portal.id, tipo: 'Sincrona', peso: 3, justificativa: 'O cliente acessa o portal.' });
  await criarAresta(request, caso.id, { noOrigemId: portal.id, noDestinoId: pedido.id, tipo: 'Sincrona', peso: 2, justificativa: 'O portal envia o pedido.' });
  // Aresta independente do Portal: mantém o caso com Arestas depois da exclusão, para a aba Fluxos
  // continuar listando (e sinalizando) o fluxo invalidado.
  await criarAresta(request, caso.id, { noOrigemId: cliente.id, noDestinoId: gateway.id, tipo: 'Assincrona', peso: 1, justificativa: 'O cliente paga pelo gateway externo.' });
  await criarFluxo(request, caso.id, { nome: 'Pedido', nosIds: [cliente.id, portal.id, pedido.id] });

  await page.goto(`/casos/${caso.id}/nos`);
  const contagem = page.getByText(/\d+ nós? · \d+ arestas?/);
  await expect(contagem).toHaveText('4 nós · 3 arestas');

  const abrirModal = async () => {
    await page.getByRole('button', { name: 'Excluir Nó Portal' }).click();
    const modal = page.getByRole('dialog', { name: 'Confirmar exclusão de Nó Portal' });
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('remove 2 arestas ligadas');
    return modal;
  };

  await test.step('Esc fecha o modal sem excluir', async () => {
    const modal = await abrirModal();
    await page.keyboard.press('Escape');
    await expect(modal).toBeHidden();
    await expect(contagem).toHaveText('4 nós · 3 arestas');
  });

  await test.step('Cancelar fecha o modal sem excluir', async () => {
    const modal = await abrirModal();
    await modal.getByRole('button', { name: 'Cancelar' }).click();
    await expect(modal).toBeHidden();
    await expect(contagem).toHaveText('4 nós · 3 arestas');
  });

  await test.step('Confirmar exclui o nó e as duas arestas ligadas', async () => {
    const modal = await abrirModal();
    await modal.getByRole('button', { name: 'Confirmar exclusão' }).click();
    await expect(modal).toBeHidden();
    await expect(contagem).toHaveText('3 nós · 1 aresta');
    await expect(page.getByRole('button', { name: 'Excluir Nó Portal' })).toHaveCount(0);
  });

  const etapas = page.getByRole('navigation', { name: 'Etapas do método' });

  await test.step('continua assim após recarregar, e a lista de arestas ficou só com a independente', async () => {
    await page.reload();
    await expect(contagem).toHaveText('3 nós · 1 aresta');
    await etapas.getByRole('link', { name: /Arestas$/ }).click();
    await expect(page.getByRole('heading', { name: 'Arestas cadastradas (1)' })).toBeVisible();
  });

  await test.step('o fluxo que passava pelo Portal não foi apagado: aparece como inválido, com a etapa e o Nó removido', async () => {
    await etapas.getByRole('link', { name: /Fluxos$/ }).click();
    await expect(page.getByRole('heading', { name: 'Fluxos principais (1)' })).toBeVisible();
    await expect(page.getByText('inválido', { exact: true })).toBeVisible();
    await expect(page.getByText('1 fluxo ficou inválido e não entra')).toBeVisible();
    await expect(page.getByText('Nó removido').first()).toBeVisible();
  });
});
