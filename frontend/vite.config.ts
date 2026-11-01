/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// Em desenvolvimento, as chamadas a /api são repassadas para a API .NET local
// (launchSettings.json, perfil "http"), então não é preciso configurar CORS para rodar
// front e back na mesma máquina. Em produção, VITE_API_URL aponta para a API publicada.
// O alvo do proxy pode ser trocado com a variável de ambiente SBAR_API_URL (ex.: API rodando em
// outra porta ou em container); sem ela, usa o perfil "http" do launchSettings (5185).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', ['SBAR_']);
  const alvoDaApi = env.SBAR_API_URL || 'http://localhost:5185';

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': alvoDaApi,
      },
      // O glossário (docs/dominio/glossario.md) mora fora de frontend/ e é importado como
      // texto pela ajuda flutuante — fonte única, POP-09 seção 8.5.
      fs: { allow: ['..'] },
    },
    // `vite preview` (usado pelo E2E para servir o build de produção) não herda o proxy do `server`.
    preview: {
      proxy: {
        '/api': alvoDaApi,
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/setupTests.ts'],
      // O E2E (Playwright) roda à parte, em e2e/ — nunca pelo Vitest.
      include: ['src/**/*.test.{ts,tsx}'],
      // Alguns testes digitam centenas de caracteres com user-event; com a cobertura ligada (ou em
      // máquina carregada) passam de 5 s sem que nada esteja errado.
      testTimeout: 15_000,
      // `npm run test:cov`. O que só um navegador exercita (Radar, download, rotas) é coberto pelo
      // E2E e não entra aqui.
      coverage: {
        provider: 'v8',
        include: ['src/**/*.{ts,tsx}'],
        exclude: ['src/**/*.test.{ts,tsx}', 'src/testes/**'],
        reporter: ['text-summary', 'html', 'json-summary'],
      },
    },
  };
});
