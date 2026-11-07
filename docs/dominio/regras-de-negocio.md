# Regras de Negócio e Requisitos do Sistema SBAR

> **Fonte oficial**: a seção 5.3 do TCC II revisado (`docs/fontes/TCC.pdf`) — Engenharia de Requisitos do protótipo — especifica formalmente o protótipo: stakeholders (Quadro 9), requisitos funcionais RF01–RF13 (Quadro 10), requisitos não funcionais RNF01–RNF06 (Quadro 11), regras de negócio RN01–RN10 (Quadro 12), histórias de usuário US01–US07 (Quadro 13) e casos de teste CT01–CT12 (Quadro 14, com o resultado da execução). Este documento adota **os IDs do TCC como identificadores oficiais**, detalha cada requisito para a implementação em C# (a partir de `metodo-sbar.md` e `formulas-metricas.md`) e registra, para cada um, o **estado no código atual**. Todo requisito é rastreável a uma seção do método (coluna "Origem"). Ver também `estudos-de-caso.md` (números de referência de teste).
>
> Última revisão: 2026-10-22, após a **Etapa 9** (código adequado ao TCC revisado; substitui `TCC_II_Entrega_3.pdf`). Os estados ✅/🟡/❌ abaixo foram conferidos contra o código e os testes desta data. A numeração antiga (RF-01…RF-12, RNF-01…03, D1…D8) está mapeada na tabela de equivalência da seção 1.

## Visão de produto (enquadramento atualizado pelo TCC revisado)

O sistema é uma ferramenta para qualquer usuário modelar o Service Blueprint do seu próprio projeto (atores, pontos de contato, processos, dados, integrações e as ligações entre eles) e obter, sem cálculo manual, um **subsídio estruturado à decisão arquitetural**. O software é responsável por todo o pipeline: Mapa de Interações → comunidades → métricas → Radar → comparação com perfis de referência → matriz de rastreabilidade. O usuário fornece os dados de entrada (o Blueprint/Mapa, os fluxos principais e a justificativa de cada interação); todo o resto é automático.

**Mudança de enquadramento (TCC 4.6, 4.7, 4.8, RN09):** o SBAR **não recomenda nem determina uma arquitetura**. A saída é o **perfil de referência mais próximo** do serviço analisado, apresentado como subsídio à análise do arquiteto, que permanece responsável pela decisão (custos, competências da equipe, previsão de crescimento e restrições não representadas no Mapa). O termo *recommendation* do nome do método significa "indicação fundamentada e rastreável", não decisão. Isso vale para a interface, a API e a documentação (ver **D14**).

O problema que o protótipo resolve (TCC 5.3.1): a aplicação manual do SBAR envolve dezenas de cálculos por estudo de caso (cargas, médias e desvios, dez métricas, normalizações, distâncias), o que é demorado e sujeito a erros de transcrição — o próprio TCC registra inconsistências encontradas entre cálculos detalhados e quadros-resumo. O protótipo automatiza os cálculos, garante que a mesma entrada produza sempre o mesmo resultado e registra a cadeia de rastreabilidade do Blueprint até os resultados.

## Legenda do estado no código atual

| Marca | Significado |
|---|---|
| ✅ | Conforme o TCC revisado |
| 🟡 | Parcial — existe, mas diverge ou falta parte |
| ❌ | Não implementado |

`DV-xx` = divergência código × TCC revisado, registrada em `docs/projeto/PLANO_DE_IMPLEMENTACAO.md` (Etapa 9) e em `docs/projeto/FLUXO_E_STATUS.md`; na coluna "DV" aparece a divergência que **foi fechada** pelo item (ou o que ainda a mantém parcial). Código em 2026-10-22: backend .NET e front React (contagens de testes a confirmar após a auditoria dos testes; o E2E é atualizado em seguida), adequados ao TCC revisado — as exceções estão marcadas 🟡.

---

## 1. Equivalência entre a numeração antiga e a do TCC revisado

| ID antigo | Requisito | ID atual | Observação |
|---|---|---|---|
| RF-01 | Gestão de Estudos de Caso | **RX01** | Extra ao TCC (o TCC cita o Estudo de Caso, mas não especifica o CRUD) |
| RF-02 | Cadastro de Nós | **RF01 + RF02** | Banda derivada do tipo (RN01) |
| RF-03 | Cadastro de Arestas | **RF03** (+ RN02, RN03, RN04) | Novo: justificativa obrigatória |
| RF-04 | Carga de um nó | **RF04** | |
| RF-05 | Gargalo / ponto potencialmente crítico | **RF04 + RN05** | |
| — | Fluxos principais (antes derivados, D2) | **RF05** | Agora **entrada** do usuário |
| — | Comunidades e modularidade | **RF06** | Novo (substitui `cluster(v)` por conectividade fraca) |
| RF-06 | Dez métricas | **RF07** | |
| RF-07 | Normalização | **RF08 + RN06** | |
| RF-08 | Perfis Arquiteturais de referência | **RF09** (assinaturas) **+ RF11** (edição) | Valores agora publicados (Quadro 7) |
| RF-09 | Comparação e recomendação | **RF10 + RN07 + RN09** | Distância **e contribuição por eixo**; saída como subsídio |
| RF-10 | Matriz de Rastreabilidade | **RF12** (+ RF13) | |
| RF-11 | Persistência mínima, cálculo ao vivo | **RX02** | Extra ao TCC (decisão de arquitetura, POP-09) |
| RF-12 | Reprodutibilidade | **RNF02** | |
| RNF-01 | Rastreabilidade | **RF12 / RF13** | Objetivo central do método (TCC 4.6.1, 5.3.1) |
| RNF-02 | Nomenclatura de domínio | **RX03** | Extra ao TCC (POP-04) |
| RNF-03 | Auditabilidade das fórmulas | **RX04** | Extra ao TCC (POP-04) |
| — | Exportação de resultados | **RF13** | Novo |
| — | Versão dos parâmetros | **RF11** | Novo |

---

## 2. Requisitos funcionais (TCC Quadro 10)

| ID | Requisito funcional | Origem (seção do TCC) | Estado no código | DV |
|---|---|---|---|---|
| RF01 | Cadastrar os elementos do Service Blueprint e associá-los às camadas de jornada, frontstage, backstage, suporte e integração externa | 4.2.1 | ✅ (o elemento é o próprio Nó; a camada é a Banda). A **etapa do Blueprint** de cada elemento não tem campo próprio: a matriz a aproxima por "Nome (Banda)" (decisão da Etapa 9, ver RF12) | — |
| RF02 | Converter os elementos em nós, atribuindo tipo (A, TP, P, S, DS ou E) e banda | 4.2.2 e 4.3.2 | ✅ | — |
| RF03 | Cadastrar arestas direcionadas com origem, destino, tipo (SYN, ASY, CRI ou BAT), intensidade e **justificativa** | 4.2.3 a 4.2.5 | ✅ `justificativa` obrigatória (trim, ≤500; 422) em entidade, API, banco (migration com backfill) e formulário | DV-05 fechada |
| RF04 | Calcular a carga de cada nó e identificar os pontos potencialmente críticos de cada banda | 4.2.6 | ✅ cálculo e telas (a aba Radar exibe μ, σ e limiar de cada banda) | DV-08 fechada |
| RF05 | Cadastrar os fluxos principais e verificar se cada um possui caminho dirigido correspondente no grafo | 4.2.7 | ✅ entidade `Fluxo`, rotas `/api/casos/{id}/fluxos`, validação por par consecutivo (422 indicando a etapa), aba "Fluxos"; fluxo invalidado por exclusão de Nó/Aresta fica sinalizado e fora do DEP | DV-02 fechada |
| RF06 | Identificar as comunidades estruturais e informar o valor da modularidade Q | 4.4.1 | ✅ `DetectorDeComunidades` (CNM guloso, aritmética inteira); `GET /metricas` devolve `comunidades`, `modularidadeQ` e `avisos`; exibidos na aba Radar e no Mapa | DV-01 fechada |
| RF07 | Calcular as dez métricas brutas e indicar quando uma métrica não for aplicável | 4.4.2 | ✅ FLEX/AUT por comunidades e DEP por fluxos cadastrados | DV-01, DV-02 fechadas |
| RF08 | Normalizar as métricas para a escala de 0 a 5, conforme os limites cadastrados | 4.5 | ✅ (limites na tabela `ConfiguracoesDeEixo`, seed do Quadro 6) | — |
| RF09 | Exibir o Radar Arquitetural do serviço sobreposto às assinaturas de referência | 4.5 e 4.6 | ✅ (seed do Quadro 7, migration `AssinaturasDoQuadro8`) | DV-03 fechada |
| RF10 | Calcular a distância até cada assinatura e exibir a contribuição de cada eixo | 4.6 | ✅ `distancias[].contribuicoes`, `percentualDoMaximo` e `recomendacao.margem`; tela "Perfil mais próximo" com tabela de contribuições | DV-04 fechada |
| RF11 | Permitir editar limites de normalização, pesos dos eixos e assinaturas, registrando a versão utilizada | 4.5, 4.5.1 e 4.6.1 | ✅ `PUT /api/parametros/limites-e-pesos`, assinaturas (CRUD de Perfis) e versões por snapshot (`/api/parametros`); `versaoDosParametros` em cada análise; `?versao=` reaplica uma versão; tela "Parâmetros" | DV-06 fechada |
| RF12 | Gerar a matriz de rastreabilidade entre elementos do Blueprint, nós, arestas, métricas e eixos | 4.6.1 | ✅ derivada das fórmulas: gargalos de qualquer banda, arestas CRI/externas, comunidades (FLEX/AUT), fluxos válidos (DEP), Touchpoints de grau de entrada acima da média (UX), Bandas ocupadas (CO), pares de nós por Banda (ESC) e cada aresta pelo seu peso (VOL); visão por eixo. A **etapa do Blueprint** não tem campo próprio: a coluna "Elemento" é "Nome (Banda)" (aproximação adotada) | DV-07 fechada |
| RF13 | Exportar os resultados da análise | 4.6.1 | ✅ `GET /api/casos/{id}/exportacao?formato=json` ou `formato=csv` (opcional `&versao=N`) e botões de exportar na matriz | DV-07 fechada |

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
**Estritamente maior** — carga igual ao limiar NÃO é crítica (caso de borda do POP-06). A classificação indica concentração estrutural no modelo, não comprova ponto único de falha (TCC 6.5, 7.3). Carga e classificação são recalculadas a cada leitura, **nunca persistidas** (RX02).

> ⚠️ **Achado da auditoria matemática** (`analise-critica.md` Achado 2): para bandas com **exatamente 2 nós**, `μ+σ` é sempre igual ao maior dos dois valores (identidade algébrica), então esse nó **nunca** é crítico, por maior que seja sua carga relativa. Diferente da banda de 1 nó (`σ=0`, esperada e testada no POP-06). Implementar exatamente como o TCC define; os testes devem tratar isso como comportamento **esperado, não um bug** (já coberto por `Classificar_NuncaDeveMarcarGargalo_QuandoBandaTiverExatamenteDoisNos`). Segue válido no texto novo.

### RF05 — Fluxos principais como entrada (TCC 4.2.7, 4.4.1, 4.4.2 eixo DEP, 6.6, 7.2)

O conjunto `F = {f1, …, fk}` de **fluxos principais validados** é **definido e cadastrado pelo usuário** (os pesquisadores, nos estudos de caso) — não é derivado automaticamente do grafo. Cada fluxo é uma **sequência ordenada de nós** que representa uma jornada do Blueprint (ex.: CE1 F1 = `A → TP1 → S1 → DS`). O sistema deve **verificar** que cada fluxo possui um caminho dirigido correspondente: cada par consecutivo de nós `(vi, vi+1)` precisa ter uma aresta `vi → vi+1`; fluxo com etapa sem aresta é sinalizado como inválido, indicando a etapa ausente (US03, CT09). Observações extraídas dos estudos de caso:

- O fluxo **não precisa começar no Ator** nem terminar num sink (CE1 F3 = `P4 → E2 → A`; CE2 F2 = `P2 → P3 → S2 → E1 → TP4 → A`).
- O fluxo é uma **sequência de nós, não um caminho simples**: CE1 F4 = `P3 → P5 → S2 → E1 → P5 → DS` revisita P5 (o retorno `E1 → P5` é a resposta do gateway). A validação deve aceitar repetição de nó.
- Arestas paralelas (ASY/BAT entre os mesmos nós, CE2) satisfazem o mesmo passo do fluxo; o fluxo não distingue qual das paralelas usa.
- `DEP` usa `bandas_percorridas(f)` = número de bandas **distintas** visitadas pelo fluxo, incluindo origem e destino; `F = ∅` → DEP não aplicável (RN08).

Isso **supera a decisão D2** do projeto (derivação automática "Opção B", `DetectorDeFluxosPrincipais`), que era um desvio do texto do TCC antigo. Modelagem e migração: **D10**. Valores de referência: DEP 0,6 → 2,50 (CE1) e 0,8 → 3,75 (CE2) — ver `estudos-de-caso.md`.

### RF06 — Comunidades estruturais e modularidade (TCC 4.4.1, 3.2, 6.8.1, 7.4)

`cluster(v)` deixa de ser o componente conexo (conectividade fraca) e passa a ser a **comunidade estrutural** de `v`, obtida por detecção de comunidades por modularidade:

- Grafo **não direcionado** derivado do Mapa; o peso `A_ij` da ligação entre dois nós é a **soma das `w(e)`** das arestas existentes entre eles (nos dois sentidos e paralelas).
- Qualidade da divisão: `Q = (1/2m) · Σ_ij [A_ij − k_i·k_j/2m] · δ(c_i, c_j)`, com `k_i` = soma dos pesos das ligações do nó `i`, `m` = soma de todos os pesos e `δ` = 1 se `i` e `j` estão na mesma comunidade.
- Algoritmo **guloso de Clauset-Newman-Moore (2004)**: cada nó começa como uma comunidade; a cada passo unem-se as duas comunidades cuja fusão produz o maior aumento de `Q`; para quando nenhuma fusão aumenta `Q`.
- `Q > ~0,3` indica estrutura de comunidades significativa. **O valor de `Q` e a partição obtida devem ser registrados junto aos resultados de cada aplicação** (RF06, TCC 4.4.1) — o próprio TCC reconhece (6.12 d) que empates no ganho de modularidade podem gerar divisões diferentes (**D9**).
- Empates (TCC de 2026-10-25, 4.4.1): explora-se, a cada passo, todas as fusões empatadas e adota-se a divisão final de maior `Q`, independente da ordem dos nós — CE1: 3 comunidades, `Q = 0,4117`; CE2: 4 comunidades, `Q = 0,3440` (CT02) (D9 resolvida; `analise-critica.md` Achado 8).

Efeito: resolve o Achado 1 de `analise-critica.md` (FLEX e AUT constantes em 5,00) e o **D7**. Casos-limite novos a tratar: grafo sem arestas (`m = 0`, `Q` indefinido), partição de uma única comunidade (FLEX = AUT = 1 de novo — degenerado) e empates (**D9**).

### RF07 — Dez métricas (TCC 4.4.2)

Implementar as dez fórmulas de `formulas-metricas.md` (CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP), cada uma como unidade testável isoladamente (Strategy, POP-09 seção 3.2). Dependências: **FLEX e AUT** consomem as comunidades (RF06); **DEP** consome os fluxos (RF05). Casos especiais (**RN08**): denominador zero ⇒ "não aplicável", **exceto ESC, que assume 1** (nenhuma banda com ≥2 nós); INT com `M = 0`; UX sem nó Frontstage; DEP com `F = ∅`; AUT com `C = ∅` — todos como objeto explícito `{aplicavel, motivo}` (CT08).

### RF08 e RN06 — Normalização (TCC 4.5)

```
Norm_i = clamp( 5 × (M_i − M_i_min) / (M_i_max − M_i_min), 0, 5 )
```
Os limites `M_i_min`/`M_i_max` ficam num único local de configuração (nunca hardcoded por calculadora — POP-03), com o Quadro 6 como padrão provisório (CO 6–40; UX 0–3,0 "provisório"; VOL 1–3; DEP 0,2–1; demais 0–1), editáveis (RF11). Valor acima do máximo ⇒ 5 (CT07).

### RF09 — Radar e assinaturas de referência (TCC 4.5, 4.6; Quadro 7)

O Radar exibe os dez eixos com peso visual equivalente, com o perfil do serviço **sobreposto às assinaturas de referência**. As assinaturas são **provisórias**: definidas pelos pesquisadores para o experimento, com base nas características usualmente associadas a cada estilo, **sem medição empírica**, e ainda a validar por especialistas (protocolo da seção 5.4). Valores publicados (Quadro 7, com a justificativa por eixo no TCC):

| Eixo | CO | ESC | INT | UX | DISP | FLEX | TR | AUT | VOL | DEP |
|---|---|---|---|---|---|---|---|---|---|---|
| Monólito Modular | 2,0 | 1,5 | 1,0 | 2,5 | 3,0 | 1,5 | 1,5 | 1,0 | 3,0 | 3,0 |
| Microsserviços | 4,0 | 4,0 | 3,5 | 2,5 | 4,0 | 4,0 | 2,5 | 4,5 | **2,0** | 4,0 |

É uma tabela de configuração **global**, não vinculada a nenhum Estudo de Caso (CRUD de Perfis; cada alteração gera uma nova versão dos parâmetros — RF11). Resolve o **D1**. O seed do banco foi atualizado para o Quadro 7 (migration `AssinaturasDoQuadro8`: nomes "(provisório)", Microsserviços VOL = 2,0) — **DV-03 fechada**.

### RF10, RN07 e RN09 — Distância, contribuição e apresentação como subsídio (TCC 4.5.1, 4.6)

```
d(Servico, Estilo) = √( Σ_i p_i × (Norm_i^Servico − Norm_i^Estilo)² )
```
Pesos `p_i` configuráveis, padrão `0,1` nos dez eixos; **RN07**: `Σ p_i = 1` — soma diferente de 1 bloqueia o cálculo com mensagem de erro (CT10). Quando algum eixo é "não aplicável" (RN08), ele sai da distância e os pesos dos eixos aplicáveis são **renormalizados** (`p'_i = p_i ÷ Σ p_j`, DV-11), de modo que a soma dos pesos efetivos continue 1 e a distância máxima continue 5; a resposta traz os `pesosEfetivos` e os `eixosOmitidos`. O sistema deve exibir, para cada assinatura, a distância (**3 casas decimais**, RNF03) **e a contribuição de cada eixo** `p_i · (Norm_i^Servico − Norm_i^Estilo)²` (Quadros 26/34 usam os normalizados **sem arredondar**). Para interpretar (TCC 4.6): a distância máxima possível é 5, logo cada distância pode ser lida como % desse máximo; e a **margem** entre as distâncias dos estilos importa — margem pequena indica perfil intermediário em que a comparação isolada pouco diferencia (CE2: 0,239, ≈4,8% da escala; CE1: 0,553, ≈11%).

**RN09**: o resultado é apresentado como **perfil de referência mais próximo**, acompanhado de aviso de que **não substitui a análise do arquiteto** — nunca como decisão nem como recomendação definitiva (TCC 4.6, 5.1). A aba "Perfil mais próximo" traz o título "Perfil de referência mais próximo", o aviso fixo de subsídio, distâncias com 3 casas, % do máximo, margem e a tabela de contribuições; os identificadores da API (`recomendacao`, `estiloRecomendado`) foram mantidos (**D14**, opção 1; **DV-04 fechada**).

### RF11 — Parâmetros editáveis e versão utilizada (TCC 4.5, 4.5.1, 4.6.1; US06)

Limites de normalização, pesos dos eixos e assinaturas devem poder ser alterados, e **cada análise deve indicar a versão dos parâmetros** com que foi calculada; alterar um parâmetro recalcula os resultados. O TCC (4.6.1, 5.4) exige registrar os limites, as assinaturas e as distâncias usados para preservar a reprodutibilidade e a rastreabilidade entre versões do método, inclusive na reaplicação das assinaturas revisadas pelos especialistas. Implementação: estado vigente nas tabelas `ConfiguracoesDeEixo`/`PerfisArquiteturais` mais a tabela `VersoesDeParametros` (snapshot imutável em JSON, numeração sequencial com `pg_advisory_xact_lock`); a v1 nasce sob demanda; soma dos pesos ≠ 1 bloqueia a edição e o cálculo (422, RN07/CT10). Decisão **D12**.

### RF12 — Matriz de Rastreabilidade (TCC 4.6.1, Quadros 28 e 36; US07)

Cadeia: elemento do Service Blueprint → nó ou aresta do Mapa → métrica → eixo do Radar → comparação arquitetural → perfil de referência mais próximo. Colunas do TCC: **Elemento (do Blueprint, com a etapa)** · **Nó/Aresta** · **Métrica** · **Eixo(s)** · **Contribuição**. A matriz **não é uma tabela fixa**: é derivada a cada consulta do estado atual do Mapa (RX02). Categorias de linha que o TCC usa: nós potencialmente críticos de **qualquer banda** (ex.: TP2 → DISP, UX; P1 → DISP, ESC), arestas críticas (TR), arestas externas (INT), comunicação assíncrona/batch (VOL, DEP) e **comunidades** (isolamento(v), saídas_externas(c) → FLEX, AUT). US07: cada eixo lista os nós e arestas que contribuíram. Também devem ser registrados (4.6.1): elementos, classificações de nós e arestas, pesos, divergências na codificação e critérios de resolução, valores das métricas, limites, assinaturas e distâncias.

### RF13 — Exportação (TCC 4.6.1; US07)

Exportar os resultados da análise (inclui a matriz). Implementado como JSON completo e CSV (`;`, UTF-8 com BOM, seções `# Nome`) — decisão **D13**.

---

## 3. Requisitos não funcionais (TCC Quadro 11)

| ID | Requisito não funcional | Categoria | Estado no código | DV |
|---|---|---|---|---|
| RNF01 | O protótipo deve ser acessível por navegador web, sem necessidade de instalação local | Portabilidade | ✅ (front React na Vercel + API na EC2, publicados em 2026-10-25; https://sbar-miguelsrs-projects.vercel.app) | — |
| RNF02 | A mesma entrada deve produzir sempre os mesmos resultados | Confiabilidade (reprodutibilidade) | ✅ a detecção de comunidades tem desempate fixo (D9) e é estável com a lista de arestas embaralhada (há teste); a regra só reproduz o TCC se o cadastro seguir a ordem dos Quadros 20/57 | — |
| RNF03 | Valores normalizados com, no mínimo, duas casas decimais; distâncias com três | Exatidão | ✅ (2 e 3 casas na interface) | — |
| RNF04 | A interface deve permitir que um usuário sem conhecimento de teoria dos grafos execute a análise seguindo as etapas do método | Usabilidade | 🟡 há ajuda contextual, glossário e abas na ordem do método, mas não foi avaliada com usuários (é objeto da avaliação do protocolo 5.4) | — |
| RNF05 | Os cálculos de um Mapa com até 50 nós devem ser concluídos em até 2 segundos | Desempenho | ✅ teste de desempenho nas camadas Application e API: mapa sintético de 50 nós e 70 arestas; medido: `GET /metricas` em 38 ms, cálculo em 4 ms | DV-09 fechada |
| RNF06 | Código em repositório versionado, com separação entre o cálculo das métricas e a interface | Manutenibilidade | ✅ (Clean Architecture; `Sbar.Domain`/`Sbar.Application` sem dependência de tela, HTTP ou banco) | — |

---

## 4. Regras de negócio (TCC Quadro 12)

| ID | Regra de negócio | Origem | Estado no código | DV |
|---|---|---|---|---|
| RN01 | Cada nó pertence a exatamente uma banda | 4.3.1 | ✅ | — |
| RN02 | A intensidade de uma aresta assume apenas 1, 2 ou 3 | 4.2.5 | ✅ (`Peso`, 422 na API) | — |
| RN03 | Toda aresta deve possuir justificativa rastreável ao Blueprint | 4.2.3 | ✅ (`TextoValidado`, 422; arestas antigas recebem o texto-padrão "Justificativa pendente…" e a tela as marca) | DV-05 fechada |
| RN04 | Interação assíncrona com restrição de tempo real ⇒ prevalece CRI | 4.2.4 | 🟡 a classificação é escolha do usuário; só há orientação em texto | — |
| RN05 | Nó potencialmente crítico quando `carga(v) > μ + σ` das cargas da banda | 4.2.6 | ✅ | — |
| RN06 | Valores normalizados limitados a `[0, 5]` | 4.5 | ✅ | — |
| RN07 | A soma dos pesos dos eixos deve ser igual a 1 | 4.5.1 | ✅ `ConjuntoDeParametros.ValidarEixos` (edição) e bloqueio no cálculo (`ServicoDeMetricas`, 422) | DV-06 fechada |
| RN08 | Métricas com denominador zero são "não aplicáveis", exceto ESC, que assume 1 | 4.4.2 | ✅ eixo não aplicável sai da distância e os pesos dos demais são renormalizados (`p'_i = p_i ÷ Σ p_j`); `pesosEfetivos`/`eixosOmitidos` na API e aviso | DV-11 fechada (convenção do projeto; o TCC não detalha) |
| RN09 | Resultado apresentado como perfil de referência mais próximo, com aviso de que não substitui o arquiteto | 4.6 | ✅ aba "Perfil mais próximo" (aviso fixo, % do máximo, margem, contribuições) | DV-04 fechada |
| RN10 | Nós sem nenhuma aresta, quando o Mapa já possui arestas, geram aviso que identifica o nó (distorcem ESC, AUT, FLEX e DISP) | 4.4.2 | ✅ aviso por nó em `ServicoDeMetricas` (achado F-01, 2026-11-01); com `m = 0` não há aviso por nó | F-01 fechado |

---

## 5. Histórias de usuário (TCC Quadro 13)

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

## 6. Casos de teste de validação (TCC Quadro 14, seção 5.3.6)

Técnica funcional (caixa-preta), com **classes de equivalência** (entradas válidas e inválidas, ex.: intensidade dentro e fora de {1,2,3}) e **análise do valor limite** (fronteiras, ex.: métricas nos limites de normalização, carga igual a μ+σ). **Oráculo de teste**: os valores calculados manualmente nos Capítulos 6 e 7 (reproduzidos em `estudos-de-caso.md`) — ao receber os nós, arestas e fluxos de cada estudo de caso, o protótipo deve reproduzir cargas, pontos críticos, comunidades, métricas, valores normalizados e distâncias.

| ID | Caso de teste | Entrada | Resultado esperado | Requisitos | Estado |
|---|---|---|---|---|---|
| CT01 | Pontos potencialmente críticos | Mapa do CE1 | P1 e DS | RF04, RN05 | ✅ ver `[Trait("Requisito", "CT01")]` no backend e os comentários `// CT01` no front (mapeamento a fechar após a auditoria dos testes) |
| CT02 | Detecção de comunidades | Mapa do CE2 | Quatro comunidades, Q ≈ 0,34 | RF06 | ✅ ver `[Trait("Requisito", "CT02")]` no backend e os comentários `// CT02` no front (mapeamento a fechar após a auditoria dos testes) |
| CT03 | Métricas e normalização | Mapas dos CE1, CE2 e CE3 | Valores dos Quadros 25, 33 e 42 | RF07, RF08 | ✅ ver `[Trait("Requisito", "CT03")]` no backend e os comentários `// CT03` no front (mapeamento a fechar após a auditoria dos testes) |
| CT04 | Distâncias | Perfis dos CE1, CE2 e CE3 | Valores dos Quadros 27, 35 e 45 | RF10 | ✅ ver `[Trait("Requisito", "CT04")]` no backend e os comentários `// CT04` no front (mapeamento a fechar após a auditoria dos testes) |
| CT05 | Intensidade inválida | Aresta com w(e) = 4 | Cadastro rejeitado com mensagem | RF03, RN02 | ✅ ver `[Trait("Requisito", "CT05")]` no backend e os comentários `// CT05` no front (mapeamento a fechar após a auditoria dos testes) |
| CT06 | Aresta sem justificativa | Justificativa vazia | Cadastro rejeitado | RN03 | ✅ ver `[Trait("Requisito", "CT06")]` no backend e os comentários `// CT06` no front (mapeamento a fechar após a auditoria dos testes) |
| CT07 | Valor limite da normalização | Métrica acima do máximo | Normalizado = 5 | RF08, RN06 | ✅ ver `[Trait("Requisito", "CT07")]` no backend e os comentários `// CT07` no front (mapeamento a fechar após a auditoria dos testes) |
| CT08 | Métrica não aplicável | Mapa sem nós de Frontstage | UX "não aplicável" | RF07, RN08 | ✅ ver `[Trait("Requisito", "CT08")]` no backend e os comentários `// CT08` no front (mapeamento a fechar após a auditoria dos testes) |
| CT09 | Fluxo incompleto | Fluxo com etapa sem aresta | Fluxo sinalizado como inválido | RF05 | ✅ ver `[Trait("Requisito", "CT09")]` no backend e os comentários `// CT09` no front (mapeamento a fechar após a auditoria dos testes) |
| CT10 | Pesos inválidos | Soma dos pesos ≠ 1 | Cálculo bloqueado com mensagem | RF11, RN07 | ✅ ver `[Trait("Requisito", "CT10")]` no backend e os comentários `// CT10` no front (mapeamento a fechar após a auditoria dos testes) |
| CT11 | Nó sem ligação | Dois nós isolados adicionados ao CE1 | Um aviso por nó, com o nome do nó | RN10 | ✅ `[Trait("Requisito", "CT11")]` em `ServicoDeMetricasTests` |
| CT12 | Pesos renormalizados com eixos não aplicáveis | Mapa sem fluxos principais e sem nós de Frontstage | Eixos omitidos da distância; pesos dos demais renormalizados (soma 1); distância no máximo 5; aviso na resposta | RF10, RN07, RN08 | ✅ `[Trait("Requisito", "CT12")]` em `ServicoDeMetricasTests` e `ComparadorDePerfisTests` (DV-11) |

> **Mapeamento CT → teste.** Cada caso é coberto por testes automatizados marcados com `[Trait("Requisito", "CTxx")]` (backend, xUnit) e por comentários `// CTxx` (front, Vitest); os nomes dos testes não são listados aqui porque estão sendo consolidados na auditoria dos testes — o mapeamento definitivo é fechado depois dela.

Validação dos requisitos em duas etapas (TCC 5.3.6): revisão dos requisitos e histórias com o orientador (completos, consistentes e rastreáveis a uma seção do método) e verificação do protótipo pelos testes acima.

---

## 7. Stakeholders (TCC Quadro 9)

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

## 8. Requisitos do projeto que não vêm do TCC (extras)

Decisões de arquitetura e processo do projeto (POP-03/04/09), mantidas por serem necessárias à implementação; não conflitam com o TCC.

- **RX01 — Gestão de Estudos de Caso (POP-09 T1/T2).** Criar, listar, editar e excluir "Estudos de Caso" (cada um é um projeto/sistema modelado) com nome (≤120 caracteres), domínio/descrição (≤1000) e data de criação. O Estudo de Caso é o limite de agregação de Nós, Arestas (e, a partir de RF05, Fluxos); nada é compartilhado entre Estudos de Caso, exceto os Perfis Arquiteturais, que são globais. Excluir um Nó remove as Arestas ligadas; excluir o caso apaga tudo. Estado: ✅.
- **RX02 — Persistência mínima, cálculo sempre ao vivo (POP-09 seção 5).** Persistem apenas as **entradas** (EstudoDeCaso, Nó, Aresta, PerfilArquitetural, a configuração de eixos e, a partir da Etapa 9, Fluxo e versões de parâmetros). **Carga, gargalo, comunidades, Q, as dez métricas, o Radar, as distâncias e a Matriz nunca são gravados** — são recalculados a cada leitura, para jamais haver divergência entre um número desatualizado e a fórmula corrigida (POP-09 seção 5, motivado por erros reais de transcrição do TCC). Estado: ✅. Observação: guardar o **snapshot de parâmetros** (D12) é guardar entrada, não resultado derivado.
- **RX03 — Nomenclatura de domínio (POP-04).** O código usa os termos exatos do método em português (Nó, Aresta, Banda, Carga, Gargalo, Radar, Peso, Comunidade, Fluxo, Perfil Arquitetural, Matriz de Rastreabilidade), nunca termos genéricos ("score", "threshold"). Estado: ✅.
- **RX04 — Auditabilidade das fórmulas (POP-04).** Toda função que implementa fórmula do capítulo 4 cita, em comentário, a seção exata do TCC (numeração do TCC revisado). Estado: ✅ os comentários citam a numeração do TCC revisado (os seis com numeração antiga de Quadros foram corrigidos na Etapa 9, sub-etapa 9.0).

---

## Pontos de dúvida e decisões (D1–D14)

Resumo do estado das decisões (a lista detalhada de opções foi removida em 2026-11-07; está no histórico do git, `docs/dominio/duvidas-pendentes.md`):

| ID | Tema | Estado |
|---|---|---|
| D1 | Valores dos Perfis Arquiteturais | **Resolvido pelo TCC (2026-10-21)** — Quadro 7 publica as assinaturas (provisórias). A reconstrução anterior divergia só em Microsserviços VOL (4,2 × 2,0) |
| D2 | Como definir os fluxos principais | **Superado (2026-10-21)** — o TCC define F como entrada cadastrada (RF05); a derivação automática ("Opção B", 2026-09-17) deixa de valer. Ver D10 |
| D3 | Estudo de Caso 2 com dados duplicados | Resolvido (2026-09-18); dados revalidados contra o TCC revisado |
| D4 | Multiusuário ou sessão única | Sessão única, sem login (2026-09-17); **revista em 2026-10-30**: contas de usuário com vários Estudos de Caso por usuário (`PLANO_CONTAS_DE_USUARIO.md`) |
| D5 | Assistente de Tradução (T0/T0b) | Adiado (2026-09-17) |
| D6 | Validações de aresta não explícitas | Arestas paralelas **permitidas**, self-loop **proibido**, nó pertence a um único Estudo de Caso — mantido (o TCC revisado tem arestas paralelas nos dois casos) |
| D7 | FLEX e AUT constantes (sempre 5,00) | **Resolvido pelo TCC (2026-10-21)** — comunidades por modularidade (CNM), RF06. A proposta anterior de limiar de intensidade foi preterida |
| D8 | Banco e hospedagem em produção | Resolvido (2026-09-21) e publicado (2026-10-25): PostgreSQL + EC2/Docker; ver `DEPLOY.md` |
| D9 | Determinismo e casos-limite da detecção de comunidades | **Resolvida pelo TCC de 2026-10-25** — explora todos os empates e adota a divisão de maior Q (implementada) |
| D10 | Modelagem do Fluxo principal como entrada | **Recomendação implementada por padrão, aguardando confirmação** — entidade `Fluxo`; sem fluxos válidos o DEP é "não aplicável" |
| D11 | Justificativa obrigatória da aresta | **Recomendação implementada por padrão, aguardando confirmação** — obrigatória; backfill com texto-padrão |
| D12 | Versionamento de parâmetros (limites, pesos, assinaturas) | **Recomendação implementada por padrão, aguardando confirmação** — snapshot por versão e reaplicação via `?versao=`; sem comparação automática |
| D13 | Formato da exportação de resultados | **Recomendação implementada por padrão, aguardando confirmação** — JSON e CSV |
| D14 | Vocabulário: "Recomendação" → perfil de referência mais próximo | **Recomendação implementada por padrão, aguardando confirmação** — só texto de interface e docs |

### D1 — Valores dos Perfis Arquiteturais de referência — RESOLVIDO (TCC revisado, Quadro 7)
Histórico: o TCC antigo não publicava os dez valores; em 2026-09-17 foi tratado como débito técnico (placeholders) e em 2026-09-20 reconstruído por engenharia reversa dos Quadros de contribuição (seed "reconstruído"). O TCC revisado publica as assinaturas como **provisórias** (seção 4.6, Quadro 8; tabela em RF09 acima). A reconstrução coincidia em nove eixos; em Microsserviços VOL ela deu 4,2 (a outra raiz da equação quadrática), enquanto o TCC adotou **2,0**. Ação concluída na Etapa 9: seed e nomes ("provisório") atualizados — **DV-03 fechada**. Validação por especialistas (seção 5.4) pode revisar os valores: tratar como parâmetros versionáveis (D12).

### D2 — Como são definidos os "fluxos principais"? — SUPERADO (TCC revisado, RF05)
Histórico: em 2026-09-17 decidiu-se derivar F automaticamente (DFS até "sink de verdade", `DetectorDeFluxosPrincipais`), com DEP = 0,72/3,25 (CE1) e 0,68/3,00 (CE2) no lugar dos valores manuais do TCC. O TCC revisado formaliza F como **entrada cadastrada pelo usuário** com verificação de caminho (RF05, CT09) e usa DEP = 0,6/2,50 (CE1) e 0,8/3,75 (CE2). A derivação automática foi removida do código na Etapa 9 (**DV-02 fechada**); modelagem adotada: **D10**.

### D3 — Estudo de Caso 2 (clínica) — RESOLVIDO (2026-09-18)
O capítulo 7 foi reescrito pelo usuário em 2026-09-18 com domínio próprio; segue como segunda fixture de teste (POP-06). Revalidado em 2026-10-21 contra o TCC revisado: nós, arestas, cargas e pontos críticos (TP2, P2, DS) não mudaram; mudam FLEX, AUT, DEP e as distâncias (ver `estudos-de-caso.md`).

### D4 — Escopo: multiusuário ou sessão única? — RESOLVIDO (2026-09-17)
**Sessão única, sem login.** `EstudoDeCaso` não tem dono; qualquer pessoa que acessa vê/edita todos. Autenticação/multiusuário: fase futura, fora do escopo. (O TCC revisado não exige nenhum dos dois; o stakeholder "arquiteto" é representado pelos avaliadores.)

**Revista em 2026-10-30:** o plano `docs/projeto/PLANO_CONTAS_DE_USUARIO.md` foi aprovado com as recomendações A1–A6 (cookie HttpOnly, cadastro por código de convite, parâmetros e perfis por usuário, legado reivindicado por e-mail, sem compartilhamento, sem recuperação de senha). Contas e posse dos Estudos de Caso passam a fazer parte do protótipo (evolução posterior ao TCC, que segue sem exigir autenticação); a decisão de 2026-09-17 vale até as fases do plano serem publicadas.

### D5 — Assistente de Tradução (T0/T0b) — RESOLVIDO (2026-09-17)
**Deixado para depois.** O MVP cobre o núcleo: cadastro manual → cálculo → Radar → perfil de referência mais próximo → matriz (T1–T8 do POP-09).

### D6 — Regras de validação não explicitadas no TCC — mantido
(a) mais de uma aresta entre o mesmo par de nós: **permitido** — os dois estudos de caso do TCC revisado têm arestas paralelas (CE2: S2→E1 e E1→TP4 em ASY e BAT) e a modularidade soma seus pesos; (b) self-loop: **proibido** (`ArestaAutoReferenciadaException`); (c) cada nó pertence a um único Estudo de Caso.

### D7 — FLEX e AUT matematicamente constantes — RESOLVIDO (TCC revisado, 4.4.1)
Histórico: a auditoria (`analise-critica.md` Achado 1) provou que, com `cluster(v)` por conectividade fraca, FLEX e AUT valem sempre 5,00; em 2026-09-17 decidiu-se implementar literalmente e revisitar, e foi proposto um limiar de intensidade de aresta (≥3). O TCC revisado seguiu **outro caminho**: `cluster(v)` por **detecção de comunidades por modularidade** (Clauset-Newman-Moore), e o ciclo está documentado na DSR (seção 3.2) e nas seções 6.12/7.7. A proposta do limiar foi preterida. Implementado na Etapa 9 (**DV-01 fechada**).

### D8 — Banco de dados e hospedagem — RESOLVIDO (2026-09-21)
PostgreSQL via Npgsql, Postgres local por Docker Compose, hospedagem EC2 + Docker (Terraform, CD por `docker run`), front na Vercel; runbook em `docs/projeto/DEPLOY.md`. Decisão D8.

### D9 — Determinismo e casos-limite da detecção de comunidades — RESOLVIDA (TCC de 2026-10-25)
**Contexto.** RNF02 exige a mesma entrada ⇒ mesmo resultado, mas o TCC anterior (6.12 d) admitia que empates no ganho de modularidade geram divisões diferentes e não dizia como escolher. Casos-limite sem tratamento: `m = 0` (`Q` indefinido: 0/0), nós sem arestas (comunidade própria, saída externa 0 ⇒ "autônomos" no AUT) e comunidade única (FLEX = AUT = 1, degenerado).
**Decisão do TCC (4.4.1).** "O procedimento adotado explora, a cada passo, todas as fusões empatadas e, entre as divisões finais assim obtidas, adota a de maior modularidade", de modo que o resultado não depende da ordem dos nós nem dos rótulos. Nos dois estudos de caso resultam duas divisões (CE1: `Q = 0,4117` com 3 comunidades e `Q = 0,4004` com 4; CE2: `Q = 0,3440` e `Q = 0,3417`, ambas com 4); adotam-se as de maior `Q`. O TCC registra também que o guloso não garante o máximo global (Louvain, 30 sementes: `Q` até 0,412 e 0,364; mesmo estilo mais próximo).
**Implementado (2026-10-25):** `DetectorDeComunidades` (`Sbar.Application/Grafo`) com aritmética **inteira** (`ΔQ·(2m)² = 2·(2m·W_ij − K_i·K_j)`; `Q` das divisões finais comparado por `Σ_c 4m·W_c − K_c²`), exploração de **todos** os empates com memória de estados (cada partição é visitada uma vez; limite de 20.000 estados, acima do qual segue só o primeiro empate e avisa), comunidades numeradas pela menor posição de cadastro dos nós (só a numeração depende da ordem). Se duas divisões finais distintas tiverem o **mesmo** `Q` (o TCC não prevê), adota-se a primeira na ordem de cadastro e o resultado avisa. Casos-limite: `m = 0` ⇒ `Q` nulo e aviso; comunidade única ⇒ aviso de FLEX/AUT degenerados; `Q < 0,3` ⇒ aviso. Testes: partições e `Q` de CE1/CE2, prova de que existem exatamente duas divisões finais e de que a de maior `Q` é a adotada, mesmo resultado com os nós cadastrados em 30 ordens aleatórias, e 60 grafos aleatórios conferidos contra uma implementação de referência por força bruta. **Substitui** a regra provisória de 2026-10-22 (desempate por maior chave de ordem de cadastro), que dependia de cadastrar os nós na ordem dos Quadros 20/57 e não reproduz o TCC atual.

### D10 — Modelagem do Fluxo principal como entrada — IMPLEMENTADA POR PADRÃO (aguarda confirmação)
**Contexto.** RF05 exige cadastrar fluxos e verificar o caminho dirigido; nenhuma entidade "Fluxo" existe. Os fluxos do TCC são **sequências de nós** (podem revisitar nós, começar fora do Ator, terminar em qualquer nó).
**Proposta.** Entidade `Fluxo` no agregado `EstudoDeCaso` (nome + lista ordenada de Nós, com repetição permitida); rotas `POST/PUT/DELETE /api/casos/{id}/fluxos` e validação por par consecutivo (existe aresta origem→destino; senão, 422 indicando a etapa ausente); etapa/tela "Fluxos" entre Arestas e Mapa; remover Nó/Aresta invalida os fluxos afetados (sinalizados, não apagados). Estudos de Caso já gravados ficam com `F = ∅` (DEP "não aplicável") até cadastrarem fluxos; opcionalmente, um botão "sugerir fluxos" reaproveita o antigo `DetectorDeFluxosPrincipais` **só como sugestão**, nunca como valor oficial.
**Implementado (2026-10-22):** entidade `Fluxo` (+ `FluxoPasso`, sem FK de cascata no `NoId`) no agregado, rotas `GET/POST /api/casos/{id}/fluxos` e `PUT/DELETE …/{fluxoId}`, validação por par consecutivo (422 indicando a etapa e os nomes), aba "Fluxos"; excluir Nó/Aresta deixa o fluxo `valido=false` (nunca o apaga); DEP usa só fluxos válidos e fica "não aplicável" sem nenhum. O `DetectorDeFluxosPrincipais` foi **removido**, sem botão "sugerir fluxos". **Custo de mudar:** reintroduzir sugestão automática é um acréscimo isolado; manter a derivação como fallback para casos antigos contraria RF05 e os valores do TCC.

### D11 — Justificativa obrigatória da aresta — IMPLEMENTADA POR PADRÃO (aguarda confirmação)
**Contexto.** RN03/RF03/US02/CT06: toda aresta tem justificativa rastreável ao Blueprint (antes da Etapa 9 não havia o campo).
**Proposta.** Campo `Justificativa` (texto, obrigatório, com `trim`, máx. 500) em `Aresta` (criação e edição, 422 se vazia — CT06) e no formulário. Arestas já gravadas: migration preenche um texto-padrão ("Justificativa não registrada — cadastrada antes de RN03") e a interface as destaca como pendentes.
**Implementado (2026-10-22):** `justificativa` obrigatória (trim, ≤500; 422) em criação e edição; migration com backfill do texto-padrão "Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)" (`Aresta.JustificativaPendente`), que a tela marca como "pendente de revisão". **Custo de mudar:** outro texto-padrão exige nova migration de dados; apagar os dados de desenvolvimento é só limpar o banco local.

### D12 — Versionamento de parâmetros (limites, pesos, assinaturas) — IMPLEMENTADA POR PADRÃO (aguarda confirmação)
**Contexto.** RF11/US06: editar limites, pesos e assinaturas e registrar a versão usada em cada análise. Antes da Etapa 9: `ConfiguracoesDeEixo` (limites e pesos) sem API/tela e Perfis sem histórico.
**Proposta.** Entidade imutável `ConjuntoDeParametros` (versão incremental, data, descrição, snapshot de limites + pesos + assinaturas, com `Σ p_i = 1` validado — RN07/CT10) e um ponteiro para a versão ativa; `GET /metricas` aceita `?versao=` e devolve `versaoDosParametros`. Editar cria nova versão (nunca altera a anterior). É guardar **entrada**, compatível com RX02.
**Implementado (2026-10-22), com desenho diferente do proposto:** em vez de um ponteiro para a "versão ativa", o **estado vigente** continua nas tabelas `ConfiguracoesDeEixo` e `PerfisArquiteturais` e cada alteração grava, na mesma transação (`IUnidadeDeTrabalho`, `pg_advisory_xact_lock`), um **snapshot imutável** em `VersoesDeParametros` (a v1 nasce sob demanda). Rotas: `GET /api/parametros`, `/atual`, `/{numero}` e `PUT /api/parametros/limites-e-pesos`; `GET /metricas?versao=N` reaplica o snapshot; `versaoDosParametros` em cada análise; Σ pesos ≠ 1 ⇒ 422 (CT10). **Não implementado:** comparação automática entre versões (mostrar a diferença dos resultados). **Custo de mudar:** a comparação entre versões é aditiva (duas chamadas a `/metricas?versao=`); migrar para o desenho com ponteiro de versão ativa exige reescrever o repositório de parâmetros e os Perfis.

### D13 — Formato da exportação de resultados — IMPLEMENTADA POR PADRÃO (aguarda confirmação)
**Contexto.** RF13/US07 pedem "exportar os resultados da análise" (e a matriz), sem formato.
**Proposta.** JSON completo (parâmetros e versão, comunidades e Q, fluxos, métricas brutas e normalizadas, distâncias com contribuições, matriz) e CSV da matriz e das métricas, via `GET /api/casos/{id}/exportacao?formato=json|csv`. PDF continua fora do escopo.
**Implementado (2026-10-22):** `GET /api/casos/{id}/exportacao?formato=json` (padrão) ou `formato=csv`, opcional `&versao=N`; JSON com caso, versão e parâmetros, nós, arestas (com justificativa), fluxos, comunidades e Q, avisos, bandas, eixos, recomendação (contribuições e margem) e matriz; CSV com `;`, UTF-8 com BOM, ponto decimal e seções `# Nome`. **Custo de mudar:** PDF é um novo formatador sobre o mesmo `ExportacaoDto`.

### D14 — Vocabulário: "Recomendação" → perfil de referência mais próximo — IMPLEMENTADA POR PADRÃO (aguarda confirmação)
**Contexto.** O TCC revisado troca "recomendação arquitetural preliminar" por "perfil de referência mais próximo (subsídio à decisão)" (4.6–4.8, RN09). Interface, API (`RecomendacaoDto`, `estiloRecomendado`), rotas e documentação ainda dizem "Recomendação"; o `CLAUDE.md` inclui o termo na linguagem ubíqua.
**Proposta.** Trocar já o **texto da interface e dos docs** (aba "Perfil mais próximo", título "Perfil de referência mais próximo", aviso fixo de que não substitui o arquiteto, distância em % do máximo e margem); manter identificadores de API e código (renomear só junto de uma versão de contrato), e registrar a equivalência no glossário. O nome do método (SBAR) não muda.
**Implementado (2026-10-22):** aba "Perfil mais próximo" (rota `recomendacao` mantida), título "Perfil de referência mais próximo", aviso fixo de subsídio, % do máximo e margem; `recomendacao`/`estiloRecomendado` e `RecomendacaoDto` mantidos. **Custo de mudar:** renomear a API quebra o contrato (front, `apiClient`, testes e `API.md` mudam juntos); trocar só rótulos é imediato.

