# SBAR — Service Blueprint Architectural Recommendation

Projeto de conclusão de curso (TCC II) de Engenharia de Software da UNIALFA, 2026: um método de apoio à decisão arquitetural e o protótipo que o automatiza.

O **SBAR** é um método que transforma o Service Blueprint de um sistema em um Mapa de Interações Arquiteturais (um grafo de nós e arestas), calcula dez indicadores, monta o Radar Arquitetural e o compara com perfis de referência de estilos arquiteturais (ex.: Monólito Modular vs. Microsserviços). O resultado é o **perfil de referência mais próximo** — um subsídio estruturado e rastreável à decisão do arquiteto, não uma decisão automática. O usuário modela o fluxo de negócio do próprio projeto (atores, pontos de contato, processos, dados, integrações e as ligações entre eles) e o software calcula tudo o mais, com rastreabilidade até o elemento de origem.

## Estado do projeto

Protótipo funcional: API em C#/.NET 8 (Clean Architecture, PostgreSQL) e front-end em React + TypeScript, com testes automatizados no back-end (xUnit), no front (Vitest) e de ponta a ponta (Playwright), e integração contínua no GitHub Actions.

O código acompanha o TCC revisado: `cluster(v)` é comunidade por modularidade (com o valor de Q), os fluxos principais são entrada do usuário e validados, a aresta tem justificativa obrigatória, as assinaturas de referência são as do Quadro 8, limites/pesos/assinaturas são versionados e a análise pode ser exportada (JSON/CSV). Cada conta de usuário tem os seus próprios Estudos de Caso, parâmetros e perfis. As decisões que moldam o comportamento do sistema estão em [`docs/dominio/regras-de-negocio.md`](docs/dominio/regras-de-negocio.md).

## Como rodar (desenvolvimento)

- **Visual Studio:** abra `backend/src/Sbar.sln`, escolha o perfil **Docker Compose** e dê F5 — sobe a API (Swagger em `http://localhost:5185/swagger`) e o Postgres.
- **Linha de comando:** `docker compose up -d` na raiz (Postgres); copie `backend/src/Sbar.Api/appsettings.Development.json.example` para `appsettings.Development.json` e rode `dotnet run --launch-profile http` em `backend/src/Sbar.Api`; depois, em `frontend/`, `npm install` e `npm run dev` (`http://localhost:5173`).
- **Contas:** o cadastro exige um código de convite. Em desenvolvimento o código é `desenvolvimento` (definido em `appsettings.Development.json.example` e no compose do Visual Studio). Em outro ambiente, defina `Sbar__CodigoDeConvite`; sem ele o cadastro fica fechado.
- **Testes:** `dotnet test backend/src/Sbar.CI.slnf`; em `frontend/`, `npm run test:unit` (rápido), `npm run test:e2e` (exige o Postgres) ou `npm test` (os dois). Detalhes em [`frontend/README.md`](frontend/README.md).

## Por onde começar

| Se você quer... | Leia |
|---|---|
| Entender o método SBAR e a matemática por trás dele | [`docs/dominio/metodo-sbar.md`](docs/dominio/metodo-sbar.md) e [`docs/dominio/formulas-metricas.md`](docs/dominio/formulas-metricas.md) |
| Acompanhar um exemplo pequeno calculado à mão | [`docs/dominio/passo-a-passo-sbar.md`](docs/dominio/passo-a-passo-sbar.md) |
| Ver os requisitos e as decisões de projeto | [`docs/dominio/regras-de-negocio.md`](docs/dominio/regras-de-negocio.md) |
| Ver os dados numéricos usados como referência para os testes | [`docs/dominio/estudos-de-caso.md`](docs/dominio/estudos-de-caso.md) |
| Entender a arquitetura do back-end | [`docs/projeto/ARQUITETURA.md`](docs/projeto/ARQUITETURA.md) |
| Consumir a API | [`docs/projeto/API.md`](docs/projeto/API.md) (ou `/swagger`) |
| Trabalhar no front-end | [`frontend/README.md`](frontend/README.md) |
| Navegar todo o índice de documentação | [`docs/README.md`](docs/README.md) |

## Escopo desta versão

- Contas de usuário com cookie de sessão; sem compartilhamento entre usuários nem recuperação de senha.
- Cadastro manual de Nós e Arestas (sem o "Assistente de Tradução" em linguagem de negócio, que fica para uma iteração posterior).
- Núcleo: cadastro → cálculo das dez métricas → Radar Arquitetural → comparação com perfis de referência → perfil mais próximo (subsídio) → matriz de rastreabilidade.
- Comunidades por modularidade, fluxos principais cadastrados, justificativa das arestas, parâmetros versionados, contribuição por eixo, matriz derivada das fórmulas e exportação dos resultados (JSON/CSV).

## Autoria

Jeane Cristina Oliveira Martins e Miguel de Souza Ramos — UNIALFA, Engenharia de Software, 2026.

Parte do desenvolvimento (código, testes e documentação) contou com auxílio de ferramentas de inteligência artificial.
