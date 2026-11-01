# Regras de Negócio e Requisitos do Sistema SBAR

> Requisitos do protótipo (RF, RNF, RN, histórias de usuário e casos de teste), conforme a seção 5.3 do TCC, com o estado de cada item no código. As fórmulas estão em [`formulas-metricas.md`](./formulas-metricas.md) e o desenho do sistema em [`../projeto/ARQUITETURA.md`](../projeto/ARQUITETURA.md). Marcas de estado: ✅ conforme o TCC · 🟡 parcial · ❌ não implementado.

## Visão de produto (enquadramento atualizado pelo TCC revisado)

O sistema é uma ferramenta para qualquer usuário modelar o Service Blueprint do seu próprio projeto (atores, pontos de contato, processos, dados, integrações e as ligações entre eles) e obter, sem cálculo manual, um **subsídio estruturado à decisão arquitetural**. O software é responsável por todo o pipeline: Mapa de Interações → comunidades → métricas → Radar → comparação com perfis de referência → matriz de rastreabilidade. O usuário fornece os dados de entrada (o Blueprint/Mapa, os fluxos principais e a justificativa de cada interação); todo o resto é automático.

**Mudança de enquadramento (TCC 4.6, 4.7, 4.8, RN09):** o SBAR **não recomenda nem determina uma arquitetura**. A saída é o **perfil de referência mais próximo** do serviço analisado, apresentado como subsídio à análise do arquiteto, que permanece responsável pela decisão (custos, competências da equipe, previsão de crescimento e restrições não representadas no Mapa). O termo *recommendation* do nome do método significa "indicação fundamentada e rastreável", não decisão. Isso vale para a interface, a API e a documentação (ver **D14**).

O problema que o protótipo resolve (TCC 5.3.1): a aplicação manual do SBAR envolve dezenas de cálculos por estudo de caso (cargas, médias e desvios, dez métricas, normalizações, distâncias), o que é demorado e sujeito a erros de transcrição — o próprio TCC registra inconsistências encontradas entre cálculos detalhados e quadros-resumo. O protótipo automatiza os cálculos, garante que a mesma entrada produza sempre o mesmo resultado e registra a cadeia de rastreabilidade do Blueprint até os resultados.

## 1. Requisitos funcionais (TCC Quadro 11)

| ID | Requisito funcional | Origem (seção do TCC) | Estado no código |
|---|---|---|---|
| RF01 | Cadastrar os elementos do Service Blueprint e associá-los às camadas de jornada, frontstage, backstage, suporte e integração externa | 4.2.1 | ✅ (o elemento é o próprio Nó; a camada é a Banda). A **etapa do Blueprint** de cada elemento não tem campo próprio: a matriz a aproxima por "Nome (Banda)" (ver RF12) |
| RF02 | Converter os elementos em nós, atribuindo tipo (A, TP, P, S, DS ou E) e banda | 4.2.2 e 4.3.2 | ✅ |
| RF03 | Cadastrar arestas direcionadas com origem, destino, tipo (SYN, ASY, CRI ou BAT), intensidade e **justificativa** | 4.2.3 a 4.2.5 | ✅ `justificativa` obrigatória (trim, ≤500; 422) em entidade, API, banco (migration com backfill) e formulário |
| RF04 | Calcular a carga de cada nó e identificar os pontos potencialmente críticos de cada banda | 4.2.6 | ✅ cálculo e telas (a aba Radar exibe μ, σ e limiar de cada banda) |
| RF05 | Cadastrar os fluxos principais e verificar se cada um possui caminho dirigido correspondente no grafo | 4.2.7 | ✅ entidade `Fluxo`, rotas `/api/casos/{id}/fluxos`, validação por par consecutivo (422 indicando a etapa), aba "Fluxos"; fluxo invalidado por exclusão de Nó/Aresta fica sinalizado e fora do DEP |
| RF06 | Identificar as comunidades estruturais e informar o valor da modularidade Q | 4.4.1 | ✅ `DetectorDeComunidades` (CNM guloso, aritmética inteira); `GET /metricas` devolve `comunidades`, `modularidadeQ` e `avisos`; exibidos na aba Radar e no Mapa |
| RF07 | Calcular as dez métricas brutas e indicar quando uma métrica não for aplicável | 4.4.2 | ✅ FLEX/AUT por comunidades e DEP por fluxos cadastrados |
| RF08 | Normalizar as métricas para a escala de 0 a 5, conforme os limites cadastrados | 4.5 | ✅ (limites na tabela `ConfiguracoesDeEixo`, seed do Quadro 7) |
| RF09 | Exibir o Radar Arquitetural do serviço sobreposto às assinaturas de referência | 4.5 e 4.6 | ✅ (seed do Quadro 8, migration `AssinaturasDoQuadro8`) |
| RF10 | Calcular a distância até cada assinatura e exibir a contribuição de cada eixo | 4.6 | ✅ `distancias[].contribuicoes`, `percentualDoMaximo` e `recomendacao.margem`; tela "Perfil mais próximo" com tabela de contribuições |
| RF11 | Permitir editar limites de normalização, pesos dos eixos e assinaturas, registrando a versão utilizada | 4.5, 4.5.1 e 4.6.1 | ✅ `PUT /api/parametros/limites-e-pesos`, assinaturas (CRUD de Perfis) e versões por snapshot (`/api/parametros`); `versaoDosParametros` em cada análise; `?versao=` reaplica uma versão; tela "Parâmetros" |
| RF12 | Gerar a matriz de rastreabilidade entre elementos do Blueprint, nós, arestas, métricas e eixos | 4.6.1 | ✅ derivada das fórmulas: gargalos de qualquer banda, arestas CRI/externas, comunidades (FLEX/AUT), fluxos válidos (DEP), Touchpoints de grau de entrada acima da média (UX), Bandas ocupadas (CO), pares de nós por Banda (ESC) e cada aresta pelo seu peso (VOL); visão por eixo. A **etapa do Blueprint** não tem campo próprio: a coluna "Elemento" é "Nome (Banda)" (aproximação adotada) |
| RF13 | Exportar os resultados da análise | 4.6.1 | ✅ `GET /api/casos/{id}/exportacao?formato=json` ou `formato=csv` (opcional `&versao=N`) e botões de exportar na matriz |

### RF01 e RF02 — Nós e Banda (TCC 4.1.1, 4.2.1, 4.2.2, 4.3.2)

Nó com nome e um dos seis tipos: Ator, Touchpoint, Processo, Suporte, Dados, Externo. A **Banda é sempre derivada** do Tipo (**RN01**: cada nó pertence a exatamente uma banda) — nunca é um campo editável pelo usuário (`RegraDeBanda` é o único ponto que decide):

| Tipo (código TCC) | Banda |
|---|---|
| Ator (A) | Ator |
| Touchpoint (TP) | Frontstage |
| Processo (P) | Backstage |
| Suporte (S) | Suporte |
| Dados (DS) | Suporte |
| Externo (E) | Externo |

### RF03 — Arestas (TCC 4.1.2, 4.2.3, 4.2.4, 4.2.5; RN02, RN03, RN04)

Aresta direcionada com nó de origem e de destino (ambos do mesmo Estudo de Caso), tipo (Síncrona/SYN, Assíncrona/ASY, Crítica/CRI, Batch/BAT), peso `w(e) ∈ {1,2,3}` (**RN02**, validado na camada de aplicação, não só no banco — POP-09 seção 5) e **justificativa rastreável ao Blueprint** (**RN03**). A regra de 4.2.3 já existia no método ("toda aresta deve possuir rastreabilidade até uma interação descrita no Blueprint ou uma inferência devidamente justificada"); o TCC revisado a transforma em requisito de sistema. Regras adicionais:

- **RN04**: quando uma interação for assíncrona **e** tiver restrição explícita de tempo real, prevalece CRI. A classificação é escolha do usuário; o sistema apenas orienta (texto de ajuda) — ver D14 sobre linguagem.
- Arestas paralelas entre o mesmo par de nós são permitidas (os dois estudos de caso as têm; o TCC soma seus pesos na modularidade). Self-loop é proibido (D6).

### RF04 e RN05 — Carga e ponto potencialmente crítico ("gargalo") (TCC 4.2.6)

```
carga(v) = soma de w(e) para toda aresta e incidente a v (entrada OU saída)
```
Para cada banda `b` com nós, calcular `μ_b` (média) e `σ_b` (**desvio padrão populacional**, não amostral — é o que faz uma banda de 1 único nó resultar em `σ=0`, conforme POP-06) das cargas dos nós da banda. Um nó `v` é **potencialmente crítico** (termo do TCC; "gargalo" no código, linguagem ubíqua do projeto) quando:
```
carga(v) > μ_b + σ_b
```
**Estritamente maior** — carga igual ao limiar NÃO é crítica (caso de borda do POP-06). A classificação indica concentração estrutural no modelo, não comprova ponto único de falha (TCC 6.5, 7.5). Carga e classificação são recalculadas a cada leitura, **nunca persistidas** (RX02).

> ⚠️ **Caso-limite do método:** para bandas com **exatamente 2 nós**, `μ+σ` é sempre igual ao maior dos dois valores (identidade algébrica), então esse nó **nunca** é crítico, por maior que seja sua carga relativa. Diferente da banda de 1 nó (`σ=0`, esperada e testada no POP-06). Implementar exatamente como o TCC define; os testes devem tratar isso como comportamento **esperado, não um bug** (já coberto por `Classificar_NuncaDeveMarcarGargalo_QuandoBandaTiverExatamenteDoisNos`).

### RF05 — Fluxos principais como entrada (TCC 4.2.7, 4.4.1, 4.4.2 eixo DEP, 6.6, 7.6)

O conjunto `F = {f1, …, fk}` de **fluxos principais validados** é **definido e cadastrado pelo usuário** (os pesquisadores, nos estudos de caso) — não é derivado automaticamente do grafo. Cada fluxo é uma **sequência ordenada de nós** que representa uma jornada do Blueprint (ex.: CE1 F1 = `A → TP1 → S1 → DS`). O sistema deve **verificar** que cada fluxo possui um caminho dirigido correspondente: cada par consecutivo de nós `(vi, vi+1)` precisa ter uma aresta `vi → vi+1`; fluxo com etapa sem aresta é sinalizado como inválido, indicando a etapa ausente (US03, CT09). Observações extraídas dos estudos de caso:

- O fluxo **não precisa começar no Ator** nem terminar num sink (CE1 F3 = `P4 → E2 → A`; CE2 F2 = `P2 → P3 → S2 → E1 → TP4 → A`).
- O fluxo é uma **sequência de nós, não um caminho simples**: CE1 F4 = `P3 → P5 → S2 → E1 → P5 → DS` revisita P5 (o retorno `E1 → P5` é a resposta do gateway). A validação deve aceitar repetição de nó.
- Arestas paralelas (ASY/BAT entre os mesmos nós, CE2) satisfazem o mesmo passo do fluxo; o fluxo não distingue qual das paralelas usa.
- `DEP` usa `bandas_percorridas(f)` = número de bandas **distintas** visitadas pelo fluxo, incluindo origem e destino; `F = ∅` → DEP não aplicável (RN08).

Isso **supera a decisão D2** do projeto (derivação automática "Opção B", `DetectorDeFluxosPrincipais`), que era um desvio do texto do TCC antigo. Modelagem e migração: **D10**. Valores de referência: DEP 0,6 → 2,50 (CE1) e 0,8 → 3,75 (CE2) — ver `estudos-de-caso.md`.

### RF06 — Comunidades estruturais e modularidade (TCC 4.4.1, 3.2, 6.8.1, 7.8.1)

`cluster(v)` deixa de ser o componente conexo (conectividade fraca) e passa a ser a **comunidade estrutural** de `v`, obtida por detecção de comunidades por modularidade:

- Grafo **não direcionado** derivado do Mapa; o peso `A_ij` da ligação entre dois nós é a **soma das `w(e)`** das arestas existentes entre eles (nos dois sentidos e paralelas).
- Qualidade da divisão: `Q = (1/2m) · Σ_ij [A_ij − k_i·k_j/2m] · δ(c_i, c_j)`, com `k_i` = soma dos pesos das ligações do nó `i`, `m` = soma de todos os pesos e `δ` = 1 se `i` e `j` estão na mesma comunidade.
- Algoritmo **guloso de Clauset-Newman-Moore (2004)**: cada nó começa como uma comunidade; a cada passo unem-se as duas comunidades cuja fusão produz o maior aumento de `Q`; para quando nenhuma fusão aumenta `Q`.
- `Q > ~0,3` indica estrutura de comunidades significativa. **O valor de `Q` e a partição obtida devem ser registrados junto aos resultados de cada aplicação** (RF06, TCC 4.4.1) — o próprio TCC reconhece (6.12 d) que empates no ganho de modularidade podem gerar divisões diferentes (**D9**).
- Empates (TCC de 2026-10-25, 4.4.1): explora-se, a cada passo, todas as fusões empatadas e adota-se a divisão final de maior `Q`, independente da ordem dos nós — CE1: 3 comunidades, `Q = 0,4117`; CE2: 4 comunidades, `Q = 0,3440` (CT02) (D9).

Efeito: FLEX e AUT deixam de ser constantes em 5,00 (que era a consequência da definição antiga de `cluster(v)`). Casos-limite novos a tratar: grafo sem arestas (`m = 0`, `Q` indefinido), partição de uma única comunidade (FLEX = AUT = 1 de novo — degenerado) e empates (**D9**).

### RF07 — Dez métricas (TCC 4.4.2)

Implementar as dez fórmulas de `formulas-metricas.md` (CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP), cada uma como unidade testável isoladamente (Strategy, POP-09 seção 3.2). Dependências: **FLEX e AUT** consomem as comunidades (RF06); **DEP** consome os fluxos (RF05). Casos especiais (**RN08**): denominador zero ⇒ "não aplicável", **exceto ESC, que assume 1** (nenhuma banda com ≥2 nós); INT com `M = 0`; UX sem nó Frontstage; DEP com `F = ∅`; AUT com `C = ∅` — todos como objeto explícito `{aplicavel, motivo}` (CT08).

### RF08 e RN06 — Normalização (TCC 4.5)

```
Norm_i = clamp( 5 × (M_i − M_i_min) / (M_i_max − M_i_min), 0, 5 )
```
Os limites `M_i_min`/`M_i_max` ficam num único local de configuração (nunca hardcoded por calculadora — POP-03), com o Quadro 7 como padrão provisório (CO 6–40; UX 0–3,0 "provisório"; VOL 1–3; DEP 0,2–1; demais 0–1), editáveis (RF11). Valor acima do máximo ⇒ 5 (CT07).

### RF09 — Radar e assinaturas de referência (TCC 4.5, 4.6; Quadro 8)

O Radar exibe os dez eixos com peso visual equivalente, com o perfil do serviço **sobreposto às assinaturas de referência**. As assinaturas são **provisórias**: definidas pelos pesquisadores para o experimento, com base nas características usualmente associadas a cada estilo, **sem medição empírica**, e ainda a validar por especialistas (protocolo da seção 5.4). Valores publicados (Quadro 8, com a justificativa por eixo no TCC):

| Eixo | CO | ESC | INT | UX | DISP | FLEX | TR | AUT | VOL | DEP |
|---|---|---|---|---|---|---|---|---|---|---|
| Monólito Modular | 2,0 | 1,5 | 1,0 | 2,5 | 3,0 | 1,5 | 1,5 | 1,0 | 3,0 | 3,0 |
| Microsserviços | 4,0 | 4,0 | 3,5 | 2,5 | 4,0 | 4,0 | 2,5 | 4,5 | **2,0** | 4,0 |

Os perfis são **da conta** (cada conta nasce com cópias dos dois perfis do Quadro 8, com os nomes "(provisório)" e Microsserviços VOL = 2,0) e não se vinculam a nenhum Estudo de Caso. O CRUD de Perfis existe, e cada alteração gera uma nova versão dos parâmetros — RF11.

### RF10, RN07 e RN09 — Distância, contribuição e apresentação como subsídio (TCC 4.5.1, 4.6)

```
d(Servico, Estilo) = √( Σ_i p_i × (Norm_i^Servico − Norm_i^Estilo)² )
```
Pesos `p_i` configuráveis, padrão `0,1` nos dez eixos; **RN07**: `Σ p_i = 1` — soma diferente de 1 bloqueia o cálculo com mensagem de erro (CT10). O sistema deve exibir, para cada assinatura, a distância (**3 casas decimais**, RNF03) **e a contribuição de cada eixo** `p_i · (Norm_i^Servico − Norm_i^Estilo)²` (Quadros 26/36 usam os normalizados **sem arredondar**). Para interpretar (TCC 4.6): a distância máxima possível é 5, logo cada distância pode ser lida como % desse máximo; e a **margem** entre as distâncias dos estilos importa — margem pequena indica perfil intermediário em que a comparação isolada pouco diferencia (CE2: 0,239, ≈4,8% da escala; CE1: 0,553, ≈11%).

**RN09**: o resultado é apresentado como **perfil de referência mais próximo**, acompanhado de aviso de que **não substitui a análise do arquiteto** — nunca como decisão nem como recomendação definitiva (TCC 4.6, 5.1). A aba "Perfil mais próximo" traz o título "Perfil de referência mais próximo", o aviso fixo de subsídio, distâncias com 3 casas, % do máximo, margem e a tabela de contribuições; os identificadores da API (`recomendacao`, `estiloRecomendado`) foram mantidos (**D14**).

### RF11 — Parâmetros editáveis e versão utilizada (TCC 4.5, 4.5.1, 4.6.1; US06)

Limites de normalização, pesos dos eixos e assinaturas devem poder ser alterados, e **cada análise deve indicar a versão dos parâmetros** com que foi calculada; alterar um parâmetro recalcula os resultados. O TCC (4.6.1, 5.4) exige registrar os limites, as assinaturas e as distâncias usados para preservar a reprodutibilidade e a rastreabilidade entre versões do método, inclusive na reaplicação das assinaturas revisadas pelos especialistas. Implementação: estado vigente nas tabelas `ConfiguracoesDeEixo`/`PerfisArquiteturais` mais a tabela `VersoesDeParametros` (snapshot imutável em JSON, numeração sequencial com `pg_advisory_xact_lock`); a v1 nasce sob demanda; soma dos pesos ≠ 1 bloqueia a edição e o cálculo (422, RN07/CT10). Decisão **D12**.

### RF12 — Matriz de Rastreabilidade (TCC 4.6.1, Quadros 28 e 38; US07)

Cadeia: elemento do Service Blueprint → nó ou aresta do Mapa → métrica → eixo do Radar → comparação arquitetural → perfil de referência mais próximo. Colunas do TCC: **Elemento (do Blueprint, com a etapa)** · **Nó/Aresta** · **Métrica** · **Eixo(s)** · **Contribuição**. A matriz **não é uma tabela fixa**: é derivada a cada consulta do estado atual do Mapa (RX02). Categorias de linha que o TCC usa: nós potencialmente críticos de **qualquer banda** (ex.: TP2 → DISP, UX; P1 → DISP, ESC), arestas críticas (TR), arestas externas (INT), comunicação assíncrona/batch (VOL, DEP) e **comunidades** (isolamento(v), saídas_externas(c) → FLEX, AUT). US07: cada eixo lista os nós e arestas que contribuíram. Também devem ser registrados (4.6.1): elementos, classificações de nós e arestas, pesos, divergências na codificação e critérios de resolução, valores das métricas, limites, assinaturas e distâncias.

### RF13 — Exportação (TCC 4.6.1; US07)

Exportar os resultados da análise (inclui a matriz). Implementado como JSON completo e CSV (`;`, UTF-8 com BOM, seções `# Nome`) — decisão **D13**.

---

## 2. Requisitos não funcionais (TCC Quadro 12)

| ID | Requisito não funcional | Categoria | Estado no código |
|---|---|---|---|
| RNF01 | O protótipo deve ser acessível por navegador web, sem necessidade de instalação local | Portabilidade | ✅ (front React + API, publicados e acessíveis pelo navegador) |
| RNF02 | A mesma entrada deve produzir sempre os mesmos resultados | Confiabilidade (reprodutibilidade) | ✅ a detecção de comunidades tem desempate fixo (D9) e é estável com a lista de arestas embaralhada (há teste); a regra só reproduz o TCC se o cadastro seguir a ordem dos Quadros 20/31 |
| RNF03 | Valores normalizados com, no mínimo, duas casas decimais; distâncias com três | Exatidão | ✅ (2 e 3 casas na interface) |
| RNF04 | A interface deve permitir que um usuário sem conhecimento de teoria dos grafos execute a análise seguindo as etapas do método | Usabilidade | 🟡 há ajuda contextual, glossário e abas na ordem do método, mas não foi avaliada com usuários (é objeto da avaliação do protocolo 5.4) |
| RNF05 | Os cálculos de um Mapa com até 50 nós devem ser concluídos em até 2 segundos | Desempenho | ✅ teste de desempenho nas camadas Application e API: mapa sintético de 50 nós e 70 arestas; medido: `GET /metricas` em 38 ms, cálculo em 4 ms |
| RNF06 | Código em repositório versionado, com separação entre o cálculo das métricas e a interface | Manutenibilidade | ✅ (Clean Architecture; `Sbar.Domain`/`Sbar.Application` sem dependência de tela, HTTP ou banco) |

---

## 3. Regras de negócio (TCC Quadro 13)

| ID | Regra de negócio | Origem | Estado no código |
|---|---|---|---|
| RN01 | Cada nó pertence a exatamente uma banda | 4.3.1 | ✅ |
| RN02 | A intensidade de uma aresta assume apenas 1, 2 ou 3 | 4.2.5 | ✅ (`Peso`, 422 na API) |
| RN03 | Toda aresta deve possuir justificativa rastreável ao Blueprint | 4.2.3 | ✅ (`TextoValidado`, 422; arestas antigas recebem o texto-padrão "Justificativa pendente…" e a tela as marca) |
| RN04 | Interação assíncrona com restrição de tempo real ⇒ prevalece CRI | 4.2.4 | 🟡 a classificação é escolha do usuário; só há orientação em texto |
| RN05 | Nó potencialmente crítico quando `carga(v) > μ + σ` das cargas da banda | 4.2.6 | ✅ |
| RN06 | Valores normalizados limitados a `[0, 5]` | 4.5 | ✅ |
| RN07 | A soma dos pesos dos eixos deve ser igual a 1 | 4.5.1 | ✅ `ConjuntoDeParametros.ValidarEixos` (edição) e bloqueio no cálculo (`ServicoDeMetricas`, 422) |
| RN08 | Métricas com denominador zero são "não aplicáveis", exceto ESC, que assume 1 | 4.4.2 | ✅ |
| RN09 | Resultado apresentado como perfil de referência mais próximo, com aviso de que não substitui o arquiteto | 4.6 | ✅ aba "Perfil mais próximo" (aviso fixo, % do máximo, margem, contribuições) |

---

## 4. Histórias de usuário (TCC Quadro 14)

Formato "Como ‹papel›, quero ‹objetivo›, para ‹benefício›" (Cohn, 2004), critérios de qualidade de Lucassen et al. (2016); cada história tem critérios de aceitação que alimentam os casos de teste.

| ID | História | Critérios de aceitação | Requisitos | Estado |
|---|---|---|---|---|
| US01 | Como designer de serviço, quero cadastrar os elementos do meu Service Blueprint por camada, para que o serviço seja convertido no Mapa de Interações | Todo elemento aparece como nó na banda correspondente; não é possível salvar nó sem banda | RF01, RF02, RN01 | ✅ (a banda é derivada do tipo, então nunca falta) |
| US02 | Como arquiteto, quero registrar as interações com tipo, intensidade e justificativa, para que cada aresta seja rastreável ao Blueprint | Aresta sem justificativa não é salva; intensidade fora de {1,2,3} é rejeitada | RF03, RN02, RN03, RN04 | ✅ |
| US03 | Como pesquisador, quero verificar se cada fluxo principal possui caminho no grafo, para garantir a completude da malha | Fluxos sem caminho são sinalizados, com a etapa ausente | RF05 | ✅ aba "Fluxos" (selo válido/inválido e etapa ausente) |
| US04 | Como arquiteto, quero visualizar os pontos potencialmente críticos, para identificar concentrações de carga | Nós críticos destacados com halo; μ e σ de cada banda exibidos | RF04, RN05 | ✅ halo no Mapa; μ, σ e limiar por banda na aba Radar |
| US05 | Como arquiteto, quero ver o Radar do serviço comparado às assinaturas de referência, para entender de qual perfil o serviço se aproxima | Radar com os dez eixos; distâncias e contribuições por eixo; aviso de subsídio preliminar | RF06 a RF10, RN06 a RN09 | ✅ |
| US06 | Como pesquisador, quero alterar limites, pesos e assinaturas e registrar a versão utilizada, para recalcular e comparar resultados | Cada análise indica a versão dos parâmetros; alterar um parâmetro recalcula | RF11 | ✅ tela "Parâmetros" e seletor de versão (a comparação automática entre versões não foi implementada — D12) |
| US07 | Como avaliador, quero consultar a matriz de rastreabilidade, para verificar quais elementos do Blueprint influenciaram cada eixo | Cada eixo lista os nós e arestas que contribuíram; a matriz pode ser exportada | RF12, RF13 | ✅ visão "Por eixo" e exportação; todos os eixos listam os nós e arestas que contribuíram (CO por Banda ocupada, ESC por Banda, VOL por aresta) |

---

## 5. Casos de teste de validação (TCC Quadro 15, seção 5.3.6)

Técnica funcional (caixa-preta), com **classes de equivalência** (entradas válidas e inválidas, ex.: intensidade dentro e fora de {1,2,3}) e **análise do valor limite** (fronteiras, ex.: métricas nos limites de normalização, carga igual a μ+σ). **Oráculo de teste**: os valores calculados manualmente nos Capítulos 6 e 7 (reproduzidos em `estudos-de-caso.md`) — ao receber os nós, arestas e fluxos de cada estudo de caso, o protótipo deve reproduzir cargas, pontos críticos, comunidades, métricas, valores normalizados e distâncias.

| ID | Caso de teste | Entrada | Resultado esperado | Requisitos | Estado |
|---|---|---|---|---|---|
| CT01 | Pontos potencialmente críticos | Mapa do CE1 | P1 e DS | RF04, RN05 | ✅ ver `[Trait("Requisito", "CT01")]` no backend e os comentários `// CT01` no front (mapeamento a fechar após a auditoria dos testes) |
| CT02 | Detecção de comunidades | Mapa do CE2 | Quatro comunidades, Q ≈ 0,34 | RF06 | ✅ ver `[Trait("Requisito", "CT02")]` no backend e os comentários `// CT02` no front (mapeamento a fechar após a auditoria dos testes) |
| CT03 | Métricas e normalização | Mapas dos CE1 e CE2 | Valores dos Quadros 25 e 35 | RF07, RF08 | ✅ ver `[Trait("Requisito", "CT03")]` no backend e os comentários `// CT03` no front (mapeamento a fechar após a auditoria dos testes) |
| CT04 | Distâncias | Perfis dos CE1 e CE2 | Valores dos Quadros 27 e 37 | RF10 | ✅ ver `[Trait("Requisito", "CT04")]` no backend e os comentários `// CT04` no front (mapeamento a fechar após a auditoria dos testes) |
| CT05 | Intensidade inválida | Aresta com w(e) = 4 | Cadastro rejeitado com mensagem | RF03, RN02 | ✅ ver `[Trait("Requisito", "CT05")]` no backend e os comentários `// CT05` no front (mapeamento a fechar após a auditoria dos testes) |
| CT06 | Aresta sem justificativa | Justificativa vazia | Cadastro rejeitado | RN03 | ✅ ver `[Trait("Requisito", "CT06")]` no backend e os comentários `// CT06` no front (mapeamento a fechar após a auditoria dos testes) |
| CT07 | Valor limite da normalização | Métrica acima do máximo | Normalizado = 5 | RF08, RN06 | ✅ ver `[Trait("Requisito", "CT07")]` no backend e os comentários `// CT07` no front (mapeamento a fechar após a auditoria dos testes) |
| CT08 | Métrica não aplicável | Mapa sem nós de Frontstage | UX "não aplicável" | RF07, RN08 | ✅ ver `[Trait("Requisito", "CT08")]` no backend e os comentários `// CT08` no front (mapeamento a fechar após a auditoria dos testes) |
| CT09 | Fluxo incompleto | Fluxo com etapa sem aresta | Fluxo sinalizado como inválido | RF05 | ✅ ver `[Trait("Requisito", "CT09")]` no backend e os comentários `// CT09` no front (mapeamento a fechar após a auditoria dos testes) |
| CT10 | Pesos inválidos | Soma dos pesos ≠ 1 | Cálculo bloqueado com mensagem | RF11, RN07 | ✅ ver `[Trait("Requisito", "CT10")]` no backend e os comentários `// CT10` no front (mapeamento a fechar após a auditoria dos testes) |

> **Mapeamento CT → teste.** Cada caso é coberto por testes automatizados marcados com `[Trait("Requisito", "CTxx")]` (backend, xUnit) e por comentários `// CTxx` (front, Vitest); os nomes dos testes não são listados aqui porque estão sendo consolidados na auditoria dos testes — o mapeamento definitivo é fechado depois dela.

Validação dos requisitos em duas etapas (TCC 5.3.6): revisão dos requisitos e histórias com o orientador (completos, consistentes e rastreáveis a uma seção do método) e verificação do protótipo pelos testes acima.

---

## 6. Stakeholders (TCC Quadro 10)

| Stakeholder | Interesse | Participação |
|---|---|---|
| Pesquisadores (autores) | O protótipo implementar fielmente o SBAR | Elicitação, especificação, desenvolvimento e testes |
| Orientador | Coerência metodológica e viabilidade do escopo | Revisão e validação dos requisitos |
| Especialistas avaliadores (profissionais de desenvolvimento ou arquitetura, sem exigência de formação acadêmica) | Verificar se o método é claro, útil e coerente com a prática | Avaliação do método e das assinaturas de referência (protocolo 5.4) |
| Arquiteto ou desenvolvedor (usuário-alvo) | Obter subsídios rastreáveis para a decisão arquitetural | Usuário final do protótipo; representado pelos especialistas na avaliação |
| Designer de serviço, analista de negócio ou Product Owner | Garantir que o serviço seja representado corretamente | Fornece a entrada do método (o Blueprint) |
| Banca examinadora | Avaliar o rigor e a contribuição do trabalho | Avaliação final |

Elicitação (TCC 5.3.3): análise documental do manual do método (Cap. 4) e dos estudos de caso (Caps. 6 e 7) — cada regra, fórmula e passo foi convertido em RF ou RN rastreável a uma seção — e reuniões de revisão com o orientador.

---

## 7. Requisitos do projeto que não vêm do TCC (extras)

Decisões de arquitetura e processo do projeto (POP-03/04/09), mantidas por serem necessárias à implementação; não conflitam com o TCC.

- **RX01 — Gestão de Estudos de Caso (POP-09 T1/T2).** Criar, listar, editar e excluir "Estudos de Caso" (cada um é um projeto/sistema modelado) com nome (≤120 caracteres), domínio/descrição (≤1000) e data de criação. O Estudo de Caso é o limite de agregação de Nós, Arestas (e, a partir de RF05, Fluxos); nada é compartilhado entre Estudos de Caso, exceto os Perfis Arquiteturais, que são globais. Excluir um Nó remove as Arestas ligadas; excluir o caso apaga tudo. Estado: ✅.
- **RX02 — Persistência mínima, cálculo sempre ao vivo (POP-09 seção 5).** Persistem apenas as **entradas** (EstudoDeCaso, Nó, Aresta, PerfilArquitetural, a configuração de eixos, Fluxo e versões de parâmetros). **Carga, gargalo, comunidades, Q, as dez métricas, o Radar, as distâncias e a Matriz nunca são gravados** — são recalculados a cada leitura, para jamais haver divergência entre um número desatualizado e a fórmula corrigida (POP-09 seção 5, motivado por erros reais de transcrição do TCC). Estado: ✅. Observação: guardar o **snapshot de parâmetros** (D12) é guardar entrada, não resultado derivado.
- **RX03 — Nomenclatura de domínio (POP-04).** O código usa os termos exatos do método em português (Nó, Aresta, Banda, Carga, Gargalo, Radar, Peso, Comunidade, Fluxo, Perfil Arquitetural, Matriz de Rastreabilidade), nunca termos genéricos ("score", "threshold"). Estado: ✅.
- **RX04 — Auditabilidade das fórmulas (POP-04).** Toda função que implementa fórmula do capítulo 4 cita, em comentário, a seção exata do TCC (numeração do TCC revisado). Estado: ✅ os comentários citam a numeração do TCC revisado.

---

## 8. Decisões de projeto

Decisões que moldam o comportamento do sistema e que o código preserva. Os identificadores (D4, D6…) aparecem em comentários do código.

- **D4 — Contas de usuário.** Cada pessoa tem conta (e-mail e senha), cadastro protegido por código de convite e sessão por cookie `HttpOnly`. Estudos de Caso, parâmetros e perfis são **por conta**; o recurso de outra conta responde 404. Fora do escopo: compartilhamento entre contas, papéis, recuperação de senha e confirmação de e-mail.
- **D5 — Assistente de Tradução (linguagem de negócio → peso e tipo de aresta).** Fora do escopo desta versão. O núcleo cobre cadastro manual → cálculo → Radar → perfil mais próximo → matriz.
- **D6 — Validações de aresta.** Arestas paralelas entre o mesmo par de nós são **permitidas** (os dois Estudos de Caso do TCC as têm, e a modularidade soma os pesos); a auto-referência é **proibida** (`ArestaAutoReferenciadaException`); cada nó pertence a um único Estudo de Caso.
- **D9 — Comunidades (determinismo).** O algoritmo explora todos os empates do ganho de modularidade e adota a divisão final de maior Q, em aritmética inteira, de modo que o resultado não depende da ordem dos nós. Sem arestas, Q é indefinido e o caso é tratado explicitamente.
- **D10 — Fluxo principal como entrada.** Entidade `Fluxo` (nome e sequência ordenada de Nós, com repetição permitida) validada por caminho dirigido (422 indicando a etapa sem aresta). Excluir Nó ou Aresta torna o fluxo inválido, sem apagá-lo. Sem fluxos válidos, DEP é "não aplicável".
- **D11 — Justificativa obrigatória da aresta.** Texto com `trim` e até 500 caracteres, exigido na criação e na edição (422 se vazio).
- **D12 — Parâmetros versionados.** O estado vigente fica nas tabelas de configuração e de perfis; cada alteração grava, na mesma transação, um snapshot imutável (limites, pesos e assinaturas). `GET /metricas?versao=N` refaz a análise com uma versão antiga. Não há comparação automática entre versões.
- **D13 — Exportação.** JSON completo (caso, parâmetros e versão, nós, arestas, fluxos, comunidades e Q, métricas, distâncias com contribuições e matriz) e CSV, em `GET /api/casos/{id}/exportacao`. PDF fora do escopo.
- **D14 — Vocabulário.** A interface diz "perfil de referência mais próximo" (subsídio, não recomendação), com aviso fixo de que não substitui o arquiteto. Os identificadores da API (`recomendacao`, `estiloRecomendado`) foram mantidos para não quebrar o contrato.
