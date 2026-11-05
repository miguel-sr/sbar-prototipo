import { readFileSync } from 'node:fs';
import { criarCaso } from '../support/api';
import { cadastrarClinicaPelaApi, QUANTIDADE_DE_FLUXOS } from '../support/clinica';
import { expect, test } from '../support/fixtures';

// POR QUE É E2E: a exportação (RF13) é um DOWNLOAD de verdade — a UI baixa um blob da API e o salva
// com o nome do `Content-Disposition`. O jsdom não faz download nem expõe esse cabeçalho, e um
// teste de componente com o `apiClient` mockado não prova que o arquivo que o navegador grava é o
// que a API gerou (nome, codificação, conteúdo). O CONTEÚDO só é conferido por amostra (valores
// publicados do CE2: versão dos parâmetros, comunidades/Q, fluxos, perfil mais próximo); o formato
// completo e as fórmulas são testados no backend.

test('Exportar JSON e CSV na Matriz baixa os arquivos reais com os resultados do CE2', async ({ page, request, prefixo }) => {
  const caso = await criarCaso(request, `${prefixo} Clínica exportação`);
  await cadastrarClinicaPelaApi(request, caso.id);
  // (o Paciente é o 1º nó criado pela fixture — mesma ordem do Quadro 57; a numeração C1…C4 das comunidades segue essa ordem)
  await page.goto(`/casos/${caso.id}/rastreabilidade`);
  await expect(page.getByRole('button', { name: 'Exportar CSV' })).toBeEnabled();

  await test.step('JSON: nome do arquivo, versão dos parâmetros e resultados do CE2', async () => {
    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Exportar JSON' }).click()]);
    expect(download.suggestedFilename()).toMatch(/^sbar-.+-v\d+\.json$/);

    const exportacao = JSON.parse(readFileSync((await download.path())!, 'utf-8').replace(/^﻿/, ''));
    expect(exportacao.caso.nome).toBe(`${prefixo} Clínica exportação`);
    expect(exportacao.versaoDosParametros.numero).toBeGreaterThanOrEqual(1);
    expect(exportacao.nos).toHaveLength(15);
    expect(exportacao.arestas).toHaveLength(21);
    expect(exportacao.arestas.every((a: { justificativa: string }) => a.justificativa.trim() !== '')).toBe(true);
    expect(exportacao.fluxos).toHaveLength(QUANTIDADE_DE_FLUXOS);
    expect(exportacao.comunidades).toHaveLength(4);
    expect(exportacao.modularidadeQ).toBeCloseTo(0.3440, 3);
    expect(exportacao.recomendacao.estiloRecomendado).toMatch(/^Monólito Modular/);
    expect(exportacao.recomendacao.margem).toBeCloseTo(0.239, 2);
    const flex = exportacao.eixos.find((e: { eixo: string }) => e.eixo === 'FLEX');
    expect(flex.valorNormalizado).toBeCloseTo(2.76, 2);
  });

  await test.step('CSV: nome do arquivo, BOM, seções e valores do CE2', async () => {
    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Exportar CSV' }).click()]);
    expect(download.suggestedFilename()).toMatch(/^sbar-.+-v\d+\.csv$/);

    const bruto = readFileSync((await download.path())!, 'utf-8');
    expect(bruto.charCodeAt(0)).toBe(0xfeff); // BOM: o Excel abre os acentos corretamente
    const csv = bruto.slice(1);
    for (const secao of ['# Caso', '# Fluxos principais', '# Comunidades', '# Modularidade', '# Eixos', '# Distâncias', '# Contribuições por eixo', '# Matriz de rastreabilidade']) {
      expect(csv, secao).toContain(secao);
    }
    expect(csv).toContain('F1 – Agendamento');
    expect(csv).toContain('Monólito Modular (provisório);1.10'); // ponto decimal invariante, reimportável
    expect(csv).toContain('Microsserviços (provisório);1.34');
  });
});
