# O Método SBAR — Visão Conceitual

> Fonte: o TCC II revisado (TCC revisado, 2026-10-21), capítulos 3, 4 e 5. Este documento explica o **raciocínio** do método; as fórmulas exatas estão em [`formulas-metricas.md`](./formulas-metricas.md) e os números de referência (para testes) em [`estudos-de-caso.md`](./estudos-de-caso.md).
>
> **Mudanças em relação ao TCC anterior**: o enquadramento passou de "recomendação arquitetural preliminar" para **subsídio estruturado à decisão do arquiteto**; `cluster(v)` agora é **comunidade estrutural por modularidade** (a limitação antes registrada na seção 7 está resolvida); os **fluxos principais** são entrada (cadastrados e validados); os **perfis de referência** foram publicados (Quadro 8); e há uma Engenharia de Requisitos do protótipo (seção 5.3) e um protocolo de avaliação com especialistas (seção 5.4).

## 1. O que é o SBAR

SBAR = **Service Blueprint Architectural Recommendation**. É um método (não é só o nome do software) que transforma um **Service Blueprint** de um sistema em um **subsídio estruturado para a decisão arquitetural** — o **perfil de referência mais próximo** entre estilos como Monólito Modular e Microsserviços —, passando por uma representação intermediária formal e por métricas quantitativas. O termo *recommendation* do nome deve ser entendido como uma **indicação fundamentada e rastreável** oferecida ao arquiteto, e não como uma decisão tomada pelo método (TCC 4.6).

Cadeia de transformação:

```
Service Blueprint → Mapa de Interações Arquiteturais → Formalização (grafo) →
Métricas arquiteturais (10 eixos) → Radar Arquitetural → Comparação com perfis
de referência → Perfil de referência mais próximo (subsídio à decisão)
```

O SBAR **não** substitui o julgamento do arquiteto, **não** gera código-fonte, **não** avalia segurança de infraestrutura e **não** resolve dívida técnica preexistente (TCC seção 5.1). Ele produz um subsídio estruturado e **rastreável**.

O método é construído segundo a **Design Science Research** (Peffers et al., 2007; TCC 3.2, Quadro 3). Sob a ótica da Engenharia de Requisitos, o próprio SBAR é um procedimento de elicitação e análise de **requisitos arquiteturalmente significativos** a partir do Blueprint (TCC 1 e 2.2, Quadro 1): a necessidade de resposta imediata em uma etapa, a dependência de sistemas externos, a concentração de operações num repositório ou a existência de processos que toleram atraso.

## 2. Visão de produto (o que o usuário faz no sistema)

O usuário informa os dados do fluxo de negócio do próprio sistema/serviço que está desenhando — em telas equivalentes a montar um Service Blueprint simplificado (atores, pontos de contato, processos, dados, integrações e as ligações entre eles, **cada ligação com sua justificativa**) e **cadastra os fluxos principais** do serviço — e o software executa **todo o resto do pipeline automaticamente**: monta o Mapa de Interações Arquiteturais, identifica as comunidades (e informa o `Q`), calcula as 10 métricas, plota o Radar, compara com os perfis de referência e apresenta o perfil mais próximo com a contribuição de cada eixo, sempre mantendo a rastreabilidade (matriz elemento do Blueprint → nó/aresta → métrica → eixo → comparação). Esse é o produto fim-a-fim que o backend precisa sustentar. O protótipo existe para automatizar cálculos que, manualmente, envolvem dezenas de contas por estudo de caso e já geraram inconsistências entre cálculo detalhado e quadro-resumo (TCC 5.3.1).

## 3. O Mapa de Interações Arquiteturais

É a estrutura central do método: um **grafo direcionado e ponderado** `G = (V, E)` que reorganiza o Service Blueprint em nós e arestas analisáveis quantitativamente.

### 3.1 As cinco bandas

| Banda | O que representa |
|---|---|
| **Ator** | O usuário/ator que inicia ou participa do fluxo. |
| **Frontstage** | Pontos de contato percebidos diretamente pelo usuário. |
| **Backstage** | Processos de negócio que executam o serviço, fora da percepção direta do usuário. |
| **Suporte** | Processos/mecanismos transversais (reutilizados por múltiplos fluxos) + repositórios de estado/dados. |
| **Externo** | Sistemas, serviços, fornecedores ou parceiros de terceiros. |

### 3.2 Taxonomia formal dos nós (Quadro 5 do TCC)

Cada nó é a tupla `v = (id, tipo, banda, carga, gargalo)`:

| Tipo de nó | Banda | Papel típico na arquitetura-alvo (não é regra determinística) |
|---|---|---|
| Ator/Cliente (A) | Ator | Delimita entrada/saída do sistema; não é implementado. |
| Touchpoint (TP) | Frontstage | Candidato a interface, API pública ou controller. |
| Processo (P) | Backstage | Candidato a caso de uso, serviço de aplicação ou regra de domínio. |
| Suporte (S) | Suporte | Candidato a serviço transversal (autenticação, filas, cache, log). |
| Dados (DS) | Suporte | Candidato a repositório, banco de dados ou índice de busca. |
| Externo (E) | Externo | Candidato a integração via API, webhook ou gateway externo. |

Cada nó pertence a **exatamente uma banda** (RN01).

### 3.3 Arestas (Quadro 4 do TCC)

Cada aresta é a tupla `e = (u, v, tipo, w)`, com `tipo ∈ {SYN, ASY, CRI, BAT}` e `w ∈ {1, 2, 3}` (RN02), mais uma **justificativa rastreável ao Blueprint** (RN03 — campo `justificativa` obrigatório no protótipo).

### 3.4 Atributos derivados

- **Carga do nó**: soma dos pesos das arestas incidentes.
- **Tamanho do nó** (só visual): pequeno/médio/grande por tercis da carga no próprio mapa.
- **Gargalo** (ponto potencialmente crítico): `{0,1}`, calculado por banda (ver glossário).
- **Comunidade** (`cluster(v)`): agrupamento obtido por **detecção de comunidades por modularidade** sobre o grafo não direcionado (peso = soma das `w(e)` entre dois nós), usando o algoritmo guloso de Clauset-Newman-Moore — usada nos eixos FLEX e AUT. A partição e o valor de `Q` devem ser registrados (TCC 4.4.1; detalhes em `formulas-metricas.md`).
- **Isolamento do nó** (`isolamento(v)`): fração das interações de um nó interno que ficam dentro da própria comunidade (eixo FLEX).

## 4. Passo a passo: de um Service Blueprint para o Mapa (7 passos)

Esta é a transformação que precisa virar lógica de validação/inferência no backend (hoje feita manualmente pelos pesquisadores no TCC; no produto, o próprio usuário faz isso ao preencher as telas, mas as mesmas regras de decisão se aplicam):

1. **Inventariar os elementos**: listar atores, pontos de contato, processos de backstage, processos de suporte, repositórios de dados, integrações externas. Um elemento só vira nó se está associado a uma ação explícita do Blueprint (não converter menções contextuais).
2. **Posicionar os nós em bandas**: banda decidida pelo tipo (regra fixa, nunca escolha manual). Em caso de ambiguidade Backstage vs. Suporte: específico deste fluxo → Backstage; reutilizado por múltiplos fluxos → Suporte.
3. **Traçar as arestas**: toda aresta deve ser rastreável a uma interação descrita no Blueprint (ou inferência devidamente justificada pelos pesquisadores — no produto, a **justificativa** é um campo obrigatório). Não criar conexões só para "fechar" o grafo visualmente.
4. **Classificar as arestas**: aplicar a taxonomia SYN/ASY/CRI/BAT (ver glossário). Interação que seja ao mesmo tempo assíncrona e com restrição explícita de tempo real: prevalece **CRI**, por ser a condição mais restritiva (RN04). A classificação segue o comportamento descrito no serviço, não uma tecnologia presumida.
5. **Codificar a intensidade**: peso 1–3 (ver glossário). Quando não há dado quantitativo, é uma atribuição qualitativa (no produto: decisão do usuário; no TCC: inferência justificada dos pesquisadores).
6. **Marcar pontos potencialmente críticos**: aplicar `carga(v) > μ_banda + σ_banda` por banda (RN05).
7. **Validar a malha**: **cada fluxo principal cadastrado precisa ter um caminho dirigido no grafo** que represente a sequência de interações (RF05, CT09); fluxo com etapa sem aresta volta ao passo 3 para correção. Fluxos de exceção (falhas, cancelamentos, reprocessamentos) também devem ser representados quando presentes no Blueprint. Os fluxos principais **são definidos pelo usuário/pesquisador**, não derivados do grafo.

## 5. Das métricas ao perfil de referência mais próximo

O Quadro 9 do TCC consolida o fluxo operacional em 10 etapas (1–7 são a construção do Mapa acima):

| Etapa | Entrada | Processamento | Saída |
|---|---|---|---|
| 8 | Mapa validado | Cálculo das métricas | Vetor de métricas |
| 9 | Métricas | Normalização | Perfil comportamental |
| 10 | Perfil comportamental | Comparação com referências | Perfil de referência mais próximo (subsídio à decisão) |

Em detalhe:

1. Identificar as comunidades (`Q` registrado) e calcular as 10 métricas brutas (fórmulas exatas em `formulas-metricas.md`), indicando as que forem "não aplicáveis".
2. Normalizar cada uma para escala 0–5 usando os limites min/max do **Quadro 7** (também em `formulas-metricas.md`).
3. Montar o vetor `P_servico = (Norm_1, ..., Norm_10)` — o perfil comportamental do serviço (Radar).
4. Comparar `P_servico` com cada assinatura de referência (Quadro 8: Monólito Modular, Microsserviços) via distância euclidiana ponderada (`p_i = 0,1`), exibindo a **contribuição de cada eixo**.
5. O estilo com **menor distância** é o perfil de referência mais próximo. Ler também a **margem** entre as distâncias e a distância como percentual do máximo (5): margem pequena (CE2: 0,239, ≈ 4,8% da escala) indica perfil intermediário, em que a comparação pouco diferencia os estilos e a análise do arquiteto pesa mais. O resultado é sempre apresentado como subsídio **preliminar/demonstrativo**, com aviso de que não substitui a análise do arquiteto (RN09), nunca como resposta definitiva.

## 6. Rastreabilidade (requisito não funcional obrigatório)

Cadeia obrigatória a preservar em todo o pipeline:

```
elemento do Service Blueprint → nó/aresta do Mapa → métrica → eixo do Radar →
comparação arquitetural → perfil de referência mais próximo
```

Para isso, o sistema precisa **registrar** (não apenas calcular e descartar): elementos identificados, classificação de nós e arestas, pesos atribuídos, divergências/ajustes e critérios de resolução, valores de métricas, limites de normalização usados, assinaturas arquiteturais empregadas, distâncias calculadas — e, agora, a **partição em comunidades e o `Q`**. Isso é o que sustenta a Matriz de Rastreabilidade (Quadros 28 e 38 do TCC: elemento do Blueprint com a etapa, nó/aresta, métrica, eixos, contribuição) e a reprodutibilidade da aplicação (RNF02), permitindo que analistas diferentes comparem resultados a partir dos mesmos dados e critérios.

## 7. Limitação conhecida do método — RESOLVIDA no TCC revisado (histórico e riscos remanescentes)

**O que era**: a primeira versão definia `cluster(v)` pela conectividade fraca do grafo. Isso não era só uma limitação "observada" nos estudos de caso — era uma **tautologia matemática**: como qualquer aresta entre dois nós já os coloca no mesmo componente, é logicamente impossível existir aresta "para fora" de um cluster; logo `FLEX` e `AUT` valiam **sempre** 5,00 para qualquer grafo, e uma análise de sensibilidade mostrou que retirar os dois eixos **invertia** o perfil mais próximo (de Microsserviços para Monólito Modular).

**O que o TCC fez**: o ciclo da DSR (TCC 3.2) levou a demonstração de volta ao projeto, e a definição foi **substituída pela detecção de comunidades por modularidade** (seção 4.4.1). Os resultados das **duas versões** ficam registrados nos Capítulos 6 e 7 (CE1: FLEX/AUT de 5,00 → 3,50/1,67; CE2: 5,00 → 2,76/2,50). Após a revisão, FLEX e AUT respondem por 40,2% (CE1) e 31,3% (CE2) da soma dos termos da distância até o Monólito Modular — antes 81,0% e 77,1% — e o perfil mais próximo é o mesmo com os dez eixos e com os oito restantes. Débito **D7 resolvido** pelo TCC (a proposta anterior do projeto, limiar de intensidade de aresta, não foi a adotada).

**Riscos que permanecem** (detalhes em `formulas-metricas.md`, "Casos-limite"):
- o CNM guloso **depende do desempate** quando há empate de ganho de modularidade, e a partição pode mudar (D9); por isso partição e `Q` precisam ser registrados;
- grafos muito centralizados podem resultar em **uma comunidade só** (FLEX = AUT = 1 de novo);
- a modularidade tem **limite de resolução**, pois funde comunidades pequenas e densas;
- `Q` abaixo de ~0,3 indica estrutura de comunidades fraca (o exemplo pequeno da newsletter, com 6 nós, dá ≈ 0,17).

## 8. Engenharia de Requisitos do protótipo (TCC 5.3)

O protótipo web que operacionaliza o método tem requisitos especificados e validados segundo as atividades de Engenharia de Requisitos (especificação do problema, stakeholders, elicitação, especificação, validação): 13 requisitos funcionais, 6 não funcionais, 9 regras de negócio, 7 histórias de usuário e 10 casos de teste caixa-preta (classes de equivalência e valor-limite) cujo **oráculo** são os valores calculados à mão nos Capítulos 6 e 7. A consolidação para implementação, com o estado atual do código, está em [`regras-de-negocio.md`](./regras-de-negocio.md).

## 9. Avaliação com especialistas (TCC 5.4) — resumo

Três objetivos: verificar clareza e utilidade do método; comparar as saídas do SBAR com a análise independente de profissionais; validar as assinaturas de referência. Participantes: no mínimo 5 profissionais de desenvolvimento/arquitetura com pelo menos 2 anos de experiência. Três etapas: (a) análise independente de um estudo de caso, sem ver o SBAR, indicando o estilo mais adequado; (b) avaliação do método (Mapa, Radar, distâncias, matriz) em escala Likert de 5 pontos — clareza, consistência, rastreabilidade, pontos críticos, utilidade; (c) atribuição de valores 0–5 aos dez eixos de cada estilo. Análise: mediana e proporção de respostas 4–5; **concordância** (% de participantes cuja indicação coincide com o perfil mais próximo do SBAR); assinaturas revisadas pela mediana, com eixos de grande dispersão registrados como incerteza; a versão revisada é reaplicada aos dois estudos e as diferenças são registradas (rastreabilidade 4.6.1). O cronograma do TCC (Quadro 17) prevê a aplicação em 10/10/2026 e a análise dos resultados em 24/10/2026; o andamento real não está registrado neste repositório.
