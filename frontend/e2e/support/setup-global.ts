import { request } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import { URL_API } from './api';

export const ARQUIVO_DA_SESSAO = 'e2e/.auth/usuario.json';
export const CODIGO_DE_CONVITE = 'convite-e2e'; // o mesmo de Sbar__CodigoDeConvite no playwright.config.ts
export const SENHA = 'SenhaE2e123';
const EMAIL_PADRAO = 'e2e@sbar.test';

// Cria (ou reaproveita) a conta usada pelos testes e guarda o cookie de sessão. Com `storageState` no
// playwright.config.ts, tanto o navegador (`page`) quanto a API de setup (`request`) já chegam logados;
// só o cenário de contas (08) sai desse estado de propósito.
export default async function setupGlobal() {
  const api = await request.newContext({ baseURL: URL_API });

  const cadastro = await api.post('/api/account/register', {
    data: { email: EMAIL_PADRAO, senha: SENHA, codigoDeConvite: CODIGO_DE_CONVITE },
  });
  if (cadastro.status() === 409) {
    // Banco `sbar_e2e` reaproveitado de uma execução anterior: a conta já existe.
    const entrada = await api.post('/api/account/login', { data: { email: EMAIL_PADRAO, senha: SENHA } });
    if (!entrada.ok()) throw new Error(`login do usuário E2E falhou: HTTP ${entrada.status()} ${await entrada.text()}`);
  } else if (!cadastro.ok()) {
    throw new Error(`cadastro do usuário E2E falhou: HTTP ${cadastro.status()} ${await cadastro.text()}`);
  }

  mkdirSync(dirname(ARQUIVO_DA_SESSAO), { recursive: true });
  await api.storageState({ path: ARQUIVO_DA_SESSAO });
  await api.dispose();
}
