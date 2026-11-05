# SBAR — Front-end (React + TypeScript + Vite)

As 9 telas do POP-09 (T1–T9) sobre a API .NET do SBAR (`backend/`), mais a ajuda flutuante (POP-09 seção 8.5). O Assistente de Tradução (T0/T0b) está fora do escopo desta versão (decisão D5).

> **O front acompanha o contrato real da API, que ainda segue a versão anterior do TCC.** O TCC revisado (`docs/fontes/TCC.pdf`, 2026-10-21) pede recursos que o front ainda não tem — veja "Limitações conhecidas" no fim deste arquivo e as divergências DV-01…DV-10 em `docs/projeto/FLUXO_E_STATUS.md`.

## Como rodar localmente

Pré-requisitos: Node.js 20+ e a API rodando (ver `docs/projeto/FLUXO_E_STATUS.md`).

1. Na raiz do repositório, suba o Postgres: `docker compose up -d`.
2. Suba a API no perfil `http` (porta 5185): `cd backend/src/Sbar.Api` e depois `dotnet run --launch-profile http`.
3. Em outro terminal, rode o front:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
4. Abra http://localhost:5173.

Em desenvolvimento o Vite repassa `/api/*` para `http://localhost:5185` (`vite.config.ts`), então **não é preciso configurar CORS** para rodar tudo na mesma máquina.

Para apontar o proxy para outra API (outra porta, container, ambiente remoto) use a variável de ambiente `SBAR_API_URL`, e `--port` para trocar a porta do front:

```bash
# PowerShell
$env:SBAR_API_URL = "http://localhost:5186"; npm run dev -- --port 5174
# bash
SBAR_API_URL=http://localhost:5186 npm run dev -- --port 5174
```

Também pode ficar num `frontend/.env.local` (`SBAR_API_URL=...`), que não é versionado.

## Scripts

| Comando | O que faz |
|---|---|
| `npm run dev` | Servidor de desenvolvimento (porta 5173) |
| `npm test` | **Tudo**: unidade/componente (Vitest) e depois E2E (Playwright). Exige o Postgres de pé |
| `npm run test:unit` | Só unidade e componente (Vitest, jsdom) — rápido, sem pré-requisitos |
| `npm run test:cov` | O mesmo, com cobertura (v8): resumo no terminal e HTML em `frontend/coverage/` (o CI publica como artifact) |
| `npm run test:watch` | Vitest em modo watch |
| `npm run test:e2e` | Testes E2E (Playwright, Chromium) — ver "Testes" abaixo |
| `npm run typecheck` | Checagem de tipos (TypeScript) |
| `npm run typecheck:e2e` | Checagem de tipos da pasta `e2e/` (tsconfig próprio) |
| `npm run build` | Typecheck + build de produção em `dist/` |

## Testes

Duas camadas, com responsabilidades diferentes. **Regra de escolha: cada comportamento vai para a camada mais baixa que consegue falhar pelo motivo que queremos testar.** (Estado em 2026-10-07: 163 testes Vitest em 21 arquivos e 7 testes E2E em 5 arquivos.)

| Camada | Ferramenta | O que entra |
|---|---|---|
| Unitário | Vitest | Lógica pura, sem DOM: formatação de erros (`apiClient`), validação, layout do mapa, glossário, hooks. |
| Componente | Vitest + Testing Library + user-event (jsdom) | Comportamento de React em uma tela, com a API mockada na fronteira do `apiClient`: formulários, modal de exclusão, estados de erro/vazio, renderização do Mapa/Radar. |
| E2E | Playwright (só Chromium) | Só o que exige **navegador real + API real + banco real** juntos. |

O que **não** se testa no front: as fórmulas e regras do método (eixos, gargalo, cascata, Banda). Elas são do backend (xUnit + Testcontainers); o front só confere que mostra o que a API devolve.

**Valores esperados:** os números que os testes de componente e o E2E da Clínica (`01-clinica-ponta-a-ponta`) conferem são os que a API devolve hoje, ou seja, do critério **anterior** do TCC (no CE2 da Clínica: FLEX e AUT em 5,00, DEP 3,00 e distâncias 1,900 (Monólito) / 1,258 (Microsserviços) — DV-10). Quando a Etapa 9 do plano trocar o cálculo, eles passam a ser os do TCC revisado (Quadros 25/27 e 33/35 de `docs/dominio/estudos-de-caso.md`).

### Como rodar

```bash
npm run test:unit             # Vitest (rápido, sem pré-requisitos)

docker compose up -d          # na raiz do repositório: Postgres em localhost:5432 (usuário/senha sbar)
npx playwright install chromium   # uma vez por máquina
npm run test:e2e              # sobe API + front sozinho (webServer) e roda os fluxos
npm test                      # os dois, em sequência (unidade primeiro; o E2E só roda se ela passar)
npx playwright show-report    # relatório HTML (traces, screenshots e vídeos das falhas)
```

O E2E é **isolado do ambiente de desenvolvimento**: API na porta **5195** (o F5 do Visual Studio usa 5185), front de produção servido por `vite preview` na **5175** (o `vite dev` usa 5173) e banco próprio **`sbar_e2e`** no mesmo servidor Postgres (o `Migrate()` da API cria o banco se ele não existir). O banco `sbar` de desenvolvimento nunca é tocado. Para apontar para outro Postgres: `E2E_DB_CONNECTION="Host=...;Port=...;Database=sbar_e2e;Username=...;Password=..."`.

Cada teste E2E cria **o próprio Estudo de Caso** (nome com prefixo único `[E2E xxxxxxxx]`), então os testes são independentes e rodam em paralelo; o caso é apagado no fim, mesmo se o teste falhar. Os Perfis Arquiteturais são globais e nenhum teste os altera.

### Regras para novos testes E2E

1. **Todo E2E precisa justificar, em comentário no topo do arquivo, por que não cabe adequadamente numa camada inferior** ("que bug só seria pego aqui?"). Sem justificativa, vira teste de componente ou unitário.
2. **Não duplicar regra de negócio**: não recalcular eixos nem reimplementar validações; conferir só a exibição/integração.
3. Poucos fluxos longos, não muitos curtos. Hoje são 5 arquivos em `e2e/tests/` (Clínica ponta a ponta, excluir nó com cascata, editar nó/caso, invariantes de layout, rotas inexistentes).
4. Seletores por papel e rótulo acessível (`getByRole`, `getByLabel`); asserções web-first do Playwright, **nunca `sleep`**. Os nós do Mapa expõem `role="group"` com `aria-label` e `data-gargalo`.

### Por que o `<dialog>` tem um polyfill mínimo no Vitest

O jsdom não implementa `<dialog>.showModal()`/`close()` (verificado no jsdom 30). O `src/setupTests.ts` adiciona só o mínimo — alternar o atributo `open` e disparar `close` — para os testes de componente conseguirem abrir e fechar os modais de confirmação. Esc, clique no fundo, foco preso e posicionamento **não** são simulados de propósito: não vale "simular um navegador inteiro" no jsdom; isso é comportamento de navegador real e fica no E2E (`02-excluir-no-com-cascata`). Pelo mesmo motivo, o jsdom não faz layout (`scrollWidth`/`clientHeight` são sempre 0): regressões de CSS/rolagem só o E2E vê (`04-invariantes-de-layout`).

### Adicionar Firefox/WebKit (segunda etapa)

Por ora só Chromium. Para acrescentar, em `playwright.config.ts` inclua em `projects`:
`{ name: 'firefox', use: { ...devices['Desktop Firefox'] } }` e/ou `{ name: 'webkit', use: { ...devices['Desktop Safari'] } }`, rode `npx playwright install firefox webkit` e ajuste o passo de instalação do job `e2e` em `.github/workflows/ci.yml` (`--with-deps chromium firefox webkit`).

### CI

`.github/workflows/ci.yml`: o job `frontend` (typecheck, build, Vitest) roda em paralelo ao do backend; o job `e2e` roda depois dos dois, com Postgres como serviço, e envia `playwright-report/` e `test-results/` (traces, screenshots, vídeos) como artifact por 7 dias, mesmo em falha. Falhas de E2E são reexecutadas até 2 vezes no CI (trace no primeiro retry); o Vitest não tem retry.

## Estrutura (POP-09 seção 2.1)

```
src/
├── paginas/      uma por tela: ListaDeCasos (T1), CasoForm (T2), MapaNos (T3), MapaArestas (T4),
│                 MapaVisual (T5), MetricasRadar (T6), Recomendacao (T7),
│                 MatrizRastreabilidade (T8), PerfisArquiteturais (T9)
├── componentes/  NoForm, ArestaForm, MapaSvg (+ layoutDoMapa), RadarChart, MetricaCard,
│                 AjudaFlutuante, layouts e estados de carregamento/erro
├── servicos/     apiClient.ts: o único arquivo que conhece as URLs da API
├── tipos/        contrato da API (docs/projeto/API.md) em TypeScript
├── dominio/      só texto de tela: rótulos dos enums, descrição/fórmula de cada eixo
└── ajuda/        leitura do glossário e contexto da ajuda por tela
```

### Regras que o front segue

- **O front não calcula nada do método.** Carga, gargalo, os dez eixos, normalização, distância e matriz vêm sempre da API. O front também não cacheia esses resultados: cada tela pede o estado atual (RF-11).
- **A Banda nunca é escolhida na tela.** Ela vem do backend (`RegraDeBanda`), derivada do Tipo.
- **A ajuda tem fonte única.** O painel "?" lê `docs/dominio/glossario.md` no build (import `?raw`). Para atualizar o texto da ajuda, edite o glossário, não o front.
- **Linguagem ubíqua.** Nomes de arquivos, componentes e variáveis usam os termos do TCC (Nó, Aresta, Banda, Carga, Gargalo…).

## Publicação (Vercel)

- Root Directory: `frontend`. Build: `npm run build`. Output: `dist`. O `vercel.json` já redireciona as rotas do SPA para o `index.html`.
- **Não defina `VITE_API_URL`** na Vercel: o `vercel.json` repassa `/api/*` para a API (`https://18-223-229-238.sslip.io`, a API atrás do Caddy) antes do *fallback* do SPA, então o navegador só fala com a própria origem.
- A opção "Include files outside of the Root Directory" precisa ficar ligada (é o padrão), porque o build lê `../docs/dominio/glossario.md`.

**Publicado (2026-10-25):** https://sbar-miguelsrs-projects.vercel.app, projeto `sbar` na Vercel (branch `main`, Root Directory `frontend`, preset Vite). Como a página é HTTPS e a API só tem HTTP, o proxy do `vercel.json` evita o bloqueio de conteúdo misto e dispensa CORS e certificado na EC2. Se o IP da EC2 mudar, atualize o `destination` em `frontend/vercel.json`. A *Vercel Authentication* do projeto está desligada (o site é público, para os especialistas). Um domínio com HTTPS na API continua sendo a evolução natural.

## Limitações conhecidas

**Do contrato atual da API:**

- **A Matriz de Rastreabilidade só lista gargalos de Backstage e Suporte** (os que entram em DISP). O Mapa (T5) destaca os gargalos de **qualquer** banda porque usa `nosGargalo` de `/metricas`; a carga e o μ/σ de cada Nó, porém, não são expostos pela API.
- **"Ver cálculo" mostra a fórmula e o resultado, mas não os limites mín./máx.** Os limites de normalização ficam na tabela `ConfiguracoesDeEixo` e não saem em `/metricas`. Para não duplicar essa configuração (POP-03), o front mostra só a fórmula de normalização.

**Do TCC revisado (2026-10-21) — o front ainda não tem, e dependem de mudanças na API (`docs/projeto/API.md` seção 9):**

- **Justificativa da aresta** (RF03, RN03): os formulários de Aresta não pedem justificativa (DV-05).
- **Fluxos principais** (RF05, US03): não há tela para cadastrá-los nem validação visual de caminho; hoje o DEP vem de uma derivação automática no backend (DV-02, DV-08).
- **Comunidades e Q** (RF06): o Mapa não mostra comunidades nem a modularidade; os textos de ajuda por eixo (`src/dominio/eixos.ts`) ainda descrevem `cluster(v)` como componente conexo e a "limitação conhecida do método (D7)" (DV-01).
- **Contribuição por eixo, % do máximo e margem entre as distâncias** (RF10) e o vocabulário **"perfil de referência mais próximo"** com o aviso de que não substitui o arquiteto (RN09): a tela T7 ainda diz "Recomendação" e mostra só a distância total (DV-04).
- **μ e σ de cada banda** (US04), **parâmetros editáveis com versão** (RF11, US06) e **exportação dos resultados** (RF13) (DV-06, DV-07, DV-08).
- **Layout para telas pequenas:** há apenas regras mínimas abaixo de 900 px e 600 px; o layout mobile não está implementado.
