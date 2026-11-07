# Documentação do Projeto SBAR

## Estrutura

- **`fontes/`** — os documentos originais (PDFs) do TCC e dos POPs, intocados. Fonte de verdade para citações de seção. **Em caso de divergência entre um PDF dos POPs e o TCC revisado, prevalece o TCC.**
  - `TCC.pdf` — **o TCC mais recente (2026-11-06)**: fundamentação teórica, definição formal do método (capítulo 4, com `cluster(v)` por comunidades/modularidade e as assinaturas de referência do Quadro 7), a Engenharia de Requisitos do protótipo (5.3: RF01–RF13, RNF01–RNF06, RN01–RN10, histórias de usuário e casos de teste CT01–CT12 com o resultado da execução), o protocolo de avaliação com especialistas (5.4; resultados em 9.7), os três Estudos de Caso aplicados (capítulos 6, 7 e 8) e os apêndices (instrumento, sensibilidade e tabelas completas do CE2 e do CE3). Substitui `TCC_II.pdf` (2026-10-25) e `TCC_II_Entrega_3.pdf` (removidos; os Quadros foram renumerados a cada versão — ex.: limites de normalização: Quadro 5 na Entrega 3, 7 no `TCC_II.pdf` e 6 hoje).
  - `POP09_Especificacao_Tecnica_Prototipo_SBAR.pdf` — especificação técnica do protótipo: telas, arquitetura, classes, banco de dados, deploy (anterior à revisão do TCC).
  - `POPs_Ciclo_de_Vida_Prototipo_SBAR.pdf` — os 8 procedimentos de processo (escopo, requisitos, arquitetura, código/revisão, git, testes, CI/deploy, changelog).
  - `GLOSSARIO.pdf` — glossário original do projeto (fonte do `dominio/glossario.md`).

- **`dominio/`** — documentação extraída e organizada a partir das fontes acima, pronta para orientar a implementação. **Reflete o TCC revisado** (2026-10-21), que é a referência dos testes; o código foi adequado a ele na Etapa 9 (as divergências DV-01…DV-10 estão fechadas, salvo as parciais listadas em `projeto/FLUXO_E_STATUS.md`):
  - [`glossario.md`](./dominio/glossario.md) — termos do domínio (Nó, Aresta, Banda, Peso, Carga, Gargalo, Comunidade, Radar, Perfil, Recomendação/subsídio). É importado pela ajuda flutuante do front.
  - [`metodo-sbar.md`](./dominio/metodo-sbar.md) — visão conceitual do método: como um Service Blueprint vira um subsídio à decisão arquitetural.
  - [`passo-a-passo-sbar.md`](./dominio/passo-a-passo-sbar.md) — **comece por aqui para entender a mecânica**: guia único, com um exemplo pequeno e totalmente calculado à mão, do Service Blueprint até o perfil de referência mais próximo.
  - [`formulas-metricas.md`](./dominio/formulas-metricas.md) — as fórmulas exatas dos dez eixos do Radar, detecção de comunidades, normalização e distância — o núcleo a implementar no backend.
  - [`estudos-de-caso.md`](./dominio/estudos-de-caso.md) — dados numéricos completos dos **três** Estudos de Caso, usados como golden data para os testes (POP-06) — valores do TCC revisado (Quadros 18–46 e 57–62).
  - [`analise-critica.md`](./dominio/analise-critica.md) — **auditoria matemática das fórmulas**: achados sobre comportamentos degenerados (gargalo em bandas de 2 nós, saturação de FLEX/AUT na definição anterior de `cluster(v)`, casos-limite das comunidades, etc.) — ler antes de implementar as calculadoras.
  - [`regras-de-negocio.md`](./dominio/regras-de-negocio.md) — requisitos funcionais/não funcionais numerados, com o histórico completo de cada débito técnico/dúvida (D1–D14) e como cada um foi resolvido.

- **`projeto/`** — documentos de processo/gestão do projeto (POPs 01, 03 e 08), separados do conhecimento de domínio acima porque tratam de *como construir*, não de *o que é o método*:
  - [`ESCOPO.md`](./projeto/ESCOPO.md) — o que está dentro/fora desta primeira versão (POP-01), com histórico de alterações.
  - [`ARQUITETURA.md`](./projeto/ARQUITETURA.md) — fluxo do produto, as quatro camadas do backend, convenções de engenharia e débitos técnicos (POP-03); a seção 12 registra o desenho final (implementado) da adequação ao TCC revisado.
  - [`FLUXO_E_STATUS.md`](./projeto/FLUXO_E_STATUS.md) — **comece por aqui para uma visão rápida**: diagramas de sequência (Mermaid) do fluxo real do backend uma tabela do que está pronto/o que falta, por Etapa, e o estado das divergências entre o código e o TCC revisado (DV-01…DV-10).
  - [`API.md`](./projeto/API.md) — **guia para quem vai consumir a API** (o front-end): convenções gerais, formato de erro, fluxo de telas recomendado e o contrato completo (request/response/erros) de cada rota, com exemplos, incluindo justificativa, fluxos, parâmetros versionados, comunidades/Q, contribuição por eixo, matriz e exportação. Também disponível interativo em `/swagger` sempre que a API estiver rodando.
  - [`PLANO_DE_IMPLEMENTACAO.md`](./projeto/PLANO_DE_IMPLEMENTACAO.md) — a construção dividida em etapas sequenciais, cada uma com Definition of Done (inclui a Etapa 9, adequação ao TCC revisado).
  - [`PLANO_CONTAS_DE_USUARIO.md`](./projeto/PLANO_CONTAS_DE_USUARIO.md) — **plano (não implementado)** de contas de usuário e vários Estudos de Caso por usuário: decisões, HTTPS na API, posse e isolamento, parâmetros por usuário, migração dos dados publicados, fases e testes.
  - [`DEPLOY.md`](./projeto/DEPLOY.md) — runbook de deploy (publicado): Terraform da EC2, runner self-hosted do GitHub Actions, segredos, CD por `docker run` e front na Vercel (débito D8).

Ver também: [`frontend/README.md`](../frontend/README.md) (front-end, scripts e estratégia de testes).
