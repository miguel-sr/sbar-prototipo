# SBAR — Front-end (React + TypeScript + Vite)

Interface do SBAR sobre a API .NET (`backend/`): cadastro e visualização do Mapa, Radar Arquitetural, perfil de referência mais próximo, Matriz de Rastreabilidade, parâmetros versionados e perfis. Inclui login e cadastro, e uma ajuda flutuante alimentada pelo glossário. O Assistente de Tradução (linguagem de negócio → peso e tipo de aresta) está fora do escopo desta versão.

## Como rodar localmente

Pré-requisitos: Node.js 20+, .NET 8 e Docker (para o Postgres).

1. Na raiz do repositório, suba o Postgres: `docker compose up -d`.
2. Prepare a configuração da API: copie `backend/src/Sbar.Api/appsettings.Development.json.example` para `appsettings.Development.json` (ele já traz a conexão local e um código de convite de desenvolvimento).
3. Suba a API na porta 5185: `cd backend/src/Sbar.Api` e `dotnet run --launch-profile http`.
4. Em outro terminal:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
5. Abra http://localhost:5173, vá em **Criar conta** e use o código de convite de desenvolvimento (`desenvolvimento`, definido em `Sbar:CodigoDeConvite` no arquivo do passo 2).

Em desenvolvimento o Vite repassa `/api/*` para `http://localhost:5185` (`vite.config.ts`), então **não é preciso configurar CORS**. Para apontar para outra API use `SBAR_API_URL`, e `--port` para trocar a porta do front:

```bash
# PowerShell
$env:SBAR_API_URL = "http://localhost:5186"; npm run dev -- --port 5174
# bash
SBAR_API_URL=http://localhost:5186 npm run dev -- --port 5174
```

Também pode ficar em `frontend/.env.local` (`SBAR_API_URL=...`), que não é versionado.

## Scripts

| Comando | O que faz |
|---|---|
| `npm run dev` | Servidor de desenvolvimento (porta 5173) |
| `npm test` | **Tudo**: unidade/componente (Vitest) e depois E2E (Playwright). Exige o Postgres de pé |
| `npm run test:unit` | Só unidade e componente (Vitest, jsdom) — rápido, sem pré-requisitos |
| `npm run test:cov` | O mesmo, com cobertura (v8): resumo no terminal e HTML em `frontend/coverage/` |
| `npm run test:watch` | Vitest em modo watch |
| `npm run test:e2e` | Testes E2E (Playwright, Chromium) |
| `npm run typecheck` | Checagem de tipos (TypeScript) |
| `npm run typecheck:e2e` | Checagem de tipos da pasta `e2e/` (tsconfig próprio) |
| `npm run build` | Typecheck + build de produção em `dist/` |
| `npm run preview` | Serve o build de produção localmente |

## Testes

Cada comportamento vai para a **camada mais baixa que consegue falhar pelo motivo que se quer testar**. Hoje são 194 testes Vitest em 28 arquivos e 10 testes E2E em 8 arquivos.

| Camada | Ferramenta | O que entra |
|---|---|---|
| Unitário | Vitest | Lógica pura, sem DOM: erros do `apiClient`, validação, layout do mapa, glossário, hooks |
| Componente | Vitest + Testing Library + user-event (jsdom) | Comportamento de React numa tela, com a API simulada na fronteira do `apiClient`: formulários, modal de exclusão, estados de erro e vazio, sessão, Mapa e Radar |
| E2E | Playwright (só Chromium) | Só o que exige **navegador, API e banco reais** juntos |

O que **não** se testa no front: as fórmulas e regras do método (eixos, gargalo, cascata, Banda). Elas são do back-end (xUnit + Testcontainers); o front só confere que mostra o que a API devolve.

### E2E

```bash
docker compose up -d               # na raiz: Postgres em localhost:5432 (usuário e senha sbar)
npx playwright install chromium    # uma vez por máquina
npm run test:e2e                   # sobe API + front sozinho (webServer) e roda os fluxos
npx playwright show-report         # relatório HTML (traces, screenshots e vídeos das falhas)
```

O E2E é **isolado do ambiente de desenvolvimento**: API na porta **5195**, front de produção servido por `vite preview` na **5175** e banco próprio **`sbar_e2e`** no mesmo Postgres. Antes dos testes, um *setup* global (`e2e/support/setup-global.ts`) cria uma conta de teste e guarda o cookie de sessão (`e2e/.auth/`, ignorado pelo Git); todos os testes usam essa sessão, menos o de contas (`08`), que parte sem login. Cada teste cria o **próprio Estudo de Caso** (nome com prefixo único `[E2E xxxxxxxx]`) e o apaga no fim, então podem rodar em paralelo.

Regras para novos testes E2E:

1. **Justifique, em comentário no topo do arquivo, por que não cabe numa camada inferior.** Sem justificativa, vira teste de componente ou unitário.
2. **Não duplique regra de negócio:** não recalcule eixos nem reimplemente validações; confira só a exibição e a integração.
3. Poucos fluxos longos, não muitos curtos.
4. Seletores por papel e rótulo acessível (`getByRole`, `getByLabel`) e asserções web-first do Playwright, **nunca `sleep`**.

O `src/setupTests.ts` traz um polyfill mínimo de `<dialog>.showModal()/close()`, porque o jsdom não os implementa; Esc, clique no fundo e foco preso só são verificáveis no E2E.

Para acrescentar Firefox ou WebKit, inclua o projeto em `playwright.config.ts`, rode `npx playwright install firefox webkit` e ajuste o passo de instalação do job `e2e` em `.github/workflows/ci.yml`.

### CI

`.github/workflows/ci.yml`: o job `frontend` (tipos, build e Vitest) roda em paralelo ao do back-end; o `e2e` roda depois dos dois, com Postgres como serviço, e envia `playwright-report/` e `test-results/` como artifact, mesmo em falha. Falhas de E2E são reexecutadas até 2 vezes.

## Estrutura

```
src/
├── paginas/      uma por tela: ListaDeCasos, CasoForm, MapaNos, MapaArestas, Fluxos, MapaVisual,
│                 MetricasRadar, Recomendacao (perfil mais próximo), MatrizRastreabilidade,
│                 PerfisArquiteturais, Parametros, Entrar, Cadastrar, NaoEncontrada
├── componentes/  formulários (Nó, Aresta, Fluxo, Estudo de Caso), MapaSvg (+ layoutDoMapa), RadarChart,
│                 MetricaCard, TabelaDeBandas, PainelDeComunidades, SeletorDeVersao, Layout,
│                 RotaProtegida, PaginaDeConta, LogoSbar, ajuda flutuante e estados de carga/erro
├── sessao/       SessaoProvider: quem está logado (GET /api/account/me) e o que fazer em um 401
├── servicos/     apiClient.ts: o único arquivo que conhece as URLs da API
├── tipos/        contrato da API (docs/projeto/API.md) em TypeScript
├── dominio/      só texto e formatação de tela: rótulos dos enums, descrição e fórmula de cada eixo
├── hooks/        useCarregar
└── ajuda/        leitura do glossário e contexto da ajuda por tela
e2e/              testes Playwright e o setup da sessão
```

### Regras que o front segue

- **O front não calcula nada do método.** Carga, gargalo, os dez eixos, normalização, distância e matriz vêm sempre da API, e nenhum resultado é guardado no front: cada tela pede o estado atual.
- **A Banda nunca é escolhida na tela.** Ela vem do back-end, derivada do Tipo.
- **A sessão é um cookie `HttpOnly`** emitido pela API: o front não guarda token nem senha. Um 401 em qualquer chamada derruba a sessão local e leva ao login, guardando a página de origem.
- **A ajuda tem fonte única.** O painel "?" lê `docs/dominio/glossario.md` no build (import `?raw`). Para mudar o texto da ajuda, edite o glossário, não o front.
- **Linguagem ubíqua.** Arquivos, componentes e variáveis usam os termos do método (Nó, Aresta, Banda, Carga, Gargalo…).

## Publicação

- Build: `npm run build` (saída em `dist`). É um SPA: sirva `dist` em qualquer hospedagem estática e repasse `/api/*` para a API **na mesma origem**, para o cookie de sessão funcionar sem CORS.
- Não defina `VITE_API_URL` quando a hospedagem repassar `/api/*`.
- O build lê `../docs/dominio/glossario.md`, então a pasta `docs/` precisa estar disponível durante o build.

## Limitações conhecidas

- A Matriz de Rastreabilidade tem tudo o que as fórmulas citam, mas a etapa do Blueprint de cada elemento é aproximada por "Nome (Banda)", sem campo próprio.
- "Ver cálculo" mostra a fórmula e o resultado de cada eixo, sem os limites mínimo e máximo de normalização.
- Layout para telas pequenas: o cabeçalho e as telas de conta são responsivos; as telas do Mapa e do Radar têm só regras básicas abaixo de 900 px e 600 px.
