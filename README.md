# SBAR — Service Blueprint Architectural Recommendation

TCC II de Jeane Cristina Oliveira Martins e Miguel de Souza Ramos (UNIALFA — Engenharia de Software, 2026).

O **SBAR** é um método que transforma o Service Blueprint de um sistema em um Mapa de Interações Arquiteturais (um grafo de nós e arestas), calcula dez indicadores, monta o Radar Arquitetural e o compara com perfis de referência de estilos arquiteturais (ex.: Monólito Modular vs. Microsserviços). O resultado é o **perfil de referência mais próximo** — um subsídio estruturado e rastreável à decisão do arquiteto, não uma decisão automática. O usuário modela o fluxo de negócio do próprio projeto (atores, pontos de contato, processos, dados, integrações e as ligações entre eles) e o software calcula tudo o mais, com rastreabilidade até o elemento de origem.

## Estado do projeto

**Protótipo funcional implementado** (Etapas 0–9 do plano): API em C#/.NET 8 (Clean Architecture, PostgreSQL) e front-end em React + TypeScript, com testes automatizados no backend (xUnit), no front (Vitest) e de ponta a ponta (Playwright, em atualização) — contagens a confirmar após a auditoria dos testes —, CI no GitHub Actions e **publicado**: front na Vercel (https://sbar-miguelsrs-projects.vercel.app) e API em EC2 + Docker (https://18-223-229-238.sslip.io/swagger), com infraestrutura em Terraform e CD por runner self-hosted (runbook em [`docs/projeto/DEPLOY.md`](docs/projeto/DEPLOY.md)).

**O código acompanha o TCC revisado** ([`docs/fontes/TCC.pdf`](docs/fontes/TCC.pdf), versão de 2026-11-06). A **Etapa 9** (2026-10-21/22) adequou o protótipo: `cluster(v)` é comunidade por modularidade (com o valor de Q), os fluxos principais são entrada do usuário e validados, a aresta tem justificativa obrigatória, as assinaturas de referência são as do Quadro 7, limites/pesos/assinaturas são versionados, a saída mostra contribuição por eixo, % do máximo e margem, e a análise pode ser exportada (JSON/CSV). O que ficou pendente (ex.: etapa do Blueprint na matriz, comparação automática entre versões, atualização do E2E) e as decisões D9–D14, adotadas por padrão e aguardando confirmação, estão em [`docs/projeto/FLUXO_E_STATUS.md`](docs/projeto/FLUXO_E_STATUS.md) e [`docs/dominio/duvidas-pendentes.md`](docs/dominio/duvidas-pendentes.md).

## Como rodar (desenvolvimento)

- **Visual Studio:** abra `backend/src/Sbar.sln`, escolha o perfil **Docker Compose** e dê F5 — sobe a API (Swagger em `http://localhost:5185/swagger`) e o Postgres.
- **Linha de comando:** `docker compose up -d` na raiz (Postgres) e `dotnet run` em `backend/src/Sbar.Api`; depois, em `frontend/`, `npm install` e `npm run dev` (`http://localhost:5173`).
- **Testes:** `dotnet test backend/src/Sbar.CI.slnf`; em `frontend/`, `npm run test:unit` (rápido), `npm run test:e2e` (exige o Postgres) ou `npm test` (os dois). Detalhes em [`frontend/README.md`](frontend/README.md).

## Por onde começar

| Se você quer... | Leia |
|---|---|
| Entender o método SBAR e a matemática por trás dele | [`docs/dominio/metodo-sbar.md`](docs/dominio/metodo-sbar.md) e [`docs/dominio/formulas-metricas.md`](docs/dominio/formulas-metricas.md) |
| Acompanhar um exemplo pequeno calculado à mão | [`docs/dominio/passo-a-passo-sbar.md`](docs/dominio/passo-a-passo-sbar.md) |
| Ver os requisitos e as decisões/dúvidas em aberto | [`docs/dominio/regras-de-negocio.md`](docs/dominio/regras-de-negocio.md) e [`docs/dominio/duvidas-pendentes.md`](docs/dominio/duvidas-pendentes.md) |
| Ver os dados numéricos usados como referência para os testes | [`docs/dominio/estudos-de-caso.md`](docs/dominio/estudos-de-caso.md) |
| Ver o que já está pronto e o que ainda falta em relação ao TCC revisado | [`docs/projeto/FLUXO_E_STATUS.md`](docs/projeto/FLUXO_E_STATUS.md) |
| Entender a arquitetura do backend (e o desenho final da adequação ao TCC revisado) | [`docs/projeto/ARQUITETURA.md`](docs/projeto/ARQUITETURA.md) |
| Consumir a API | [`docs/projeto/API.md`](docs/projeto/API.md) (ou `/swagger`) |
| Ver o plano de implementação dividido em etapas | [`docs/projeto/PLANO_DE_IMPLEMENTACAO.md`](docs/projeto/PLANO_DE_IMPLEMENTACAO.md) |
| Trabalhar no front-end | [`frontend/README.md`](frontend/README.md) |
| Orientar um assistente de IA trabalhando neste repositório | [`CLAUDE.md`](CLAUDE.md) |
| Ver o que está dentro/fora do escopo (POP-01) | [`docs/projeto/ESCOPO.md`](docs/projeto/ESCOPO.md) |
| Ver o histórico de decisões e mudanças relevantes (POP-08) | [`CHANGELOG.md`](CHANGELOG.md) |
| Ler o TCC e os documentos dos POPs | [`docs/fontes/`](docs/fontes/) |
| Navegar todo o índice de documentação | [`docs/README.md`](docs/README.md) |

## Escopo desta versão

- Contas de usuário com cookie de sessão e Estudos de Caso, parâmetros e perfis por conta (D4 revista em 2026-10-30; publicado em 2026-10-31). Sem compartilhamento entre usuários nem recuperação de senha.
- Cadastro manual de Nós e Arestas (sem o "Assistente de Tradução" em linguagem de negócio, que fica para uma iteração posterior).
- Núcleo: cadastro → cálculo das dez métricas → Radar Arquitetural → comparação com perfis de referência → perfil mais próximo (subsídio) → matriz de rastreabilidade.
- Implementado na Etapa 9 (TCC revisado): comunidades por modularidade, fluxos principais cadastrados, justificativa das arestas, parâmetros versionados, contribuição por eixo, matriz derivada das fórmulas e exportação dos resultados (JSON/CSV).
