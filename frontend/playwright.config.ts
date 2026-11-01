import { defineConfig, devices } from '@playwright/test';

// E2E do SBAR (Playwright). Só roda o que exige navegador real + API real + banco real — o resto
// fica no Vitest (ver frontend/README.md, seção "Testes").
//
// Isolamento total do ambiente de desenvolvimento:
//  - API na porta 5195 (o F5 do Visual Studio usa 5185) e front (build de produção servido por
//    `vite preview`) na 5175 (o `vite dev` usa 5173);
//  - banco PRÓPRIO `sbar_e2e`, no mesmo servidor Postgres do docker-compose (o `Migrate()` do
//    startup da API cria o banco e aplica as migrations se ele não existir). O banco de
//    desenvolvimento (`sbar`) nunca é tocado.
const PORTA_API = 5195;
const PORTA_WEB = 5175;
const URL_API = `http://localhost:${PORTA_API}`;
const URL_WEB = `http://localhost:${PORTA_WEB}`;
const CONEXAO_E2E =
  process.env.E2E_DB_CONNECTION ?? 'Host=localhost;Port=5432;Database=sbar_e2e;Username=sbar;Password=sbar';

const noCI = !!process.env.CI;
// Convite e limite de taxa da API do E2E (e2e/support/setup-global.ts cria a conta dos testes com este convite).
const CONVITE_E2E = 'convite-e2e';

export default defineConfig({
  testDir: './e2e/tests',
  // Poucos fluxos longos e independentes: cada teste cria o próprio Estudo de Caso (nome único)
  // e o apaga no fim. Os Perfis Arquiteturais e parâmetros são da conta E2E e NÃO são alterados pelos testes.
  fullyParallel: true,
  workers: noCI ? 2 : undefined,
  forbidOnly: noCI,
  retries: noCI ? 2 : 0,
  // Conta E2E criada uma vez antes dos testes; o cookie fica em e2e/.auth (ignorado pelo git).
  globalSetup: './e2e/support/setup-global.ts',
  timeout: 45_000,
  expect: { timeout: 8_000 },
  reporter: noCI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: URL_WEB,
    storageState: 'e2e/.auth/usuario.json',
    locale: 'pt-BR',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  // Primeira etapa: só Chromium. Firefox/WebKit ficam para uma segunda etapa (basta acrescentar
  // os projetos abaixo e `npx playwright install firefox webkit`).
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      name: 'api',
      command: 'dotnet run --project ../backend/src/Sbar.Api --no-launch-profile',
      url: `${URL_API}/swagger/index.html`, // /api/casos exige login; o Swagger é público
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: URL_API,
        ConnectionStrings__Sbar: CONEXAO_E2E,
        Sbar__CodigoDeConvite: CONVITE_E2E,
        Sbar__TentativasPorMinuto: '1000',
      },
      timeout: 180_000,
      reuseExistingServer: !noCI,
    },
    {
      name: 'web',
      // Build de produção servido pelo preview (é o que de fato vai para a Vercel); o proxy do
      // preview (vite.config.ts) repassa /api para a API do E2E.
      command: `npm run build && npx vite preview --port ${PORTA_WEB} --strictPort`,
      url: URL_WEB,
      env: { SBAR_API_URL: URL_API },
      timeout: 120_000,
      reuseExistingServer: !noCI,
    },
  ],
});
