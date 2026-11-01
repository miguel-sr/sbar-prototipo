import { randomUUID } from 'node:crypto';
import { test as base, expect } from '@playwright/test';
import { apagarCasosComPrefixo } from './api';

// `prefixo` é único por teste: todo Estudo de Caso criado pelo teste (pela UI ou pela API) DEVE
// começar com ele, e é apagado no fim — mesmo se o teste falhar. Assim os testes são independentes
// entre si (podem rodar em paralelo) e o banco `sbar_e2e` não acumula lixo.
export const test = base.extend<{ prefixo: string }>({
  prefixo: async ({ request }, use) => {
    const prefixo = `[E2E ${randomUUID().slice(0, 8)}]`;
    await use(prefixo);
    await apagarCasosComPrefixo(request, prefixo);
  },
});

export { expect };
