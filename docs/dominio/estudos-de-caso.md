# Estudos de Caso — Dados de Referência para Testes

> Fonte: `docs/fontes/TCC.pdf` (**versão de 2026-11-06**; os valores numéricos dos capítulos 6, 7 e 8 são os mesmos de 2026-10-25 e 2026-11-05 — só a numeração de Quadros e seções mudou), capítulos 6 e 7 (o capítulo 8, Estudo de Caso 3, está no fim deste arquivo), mais as seções 4.4.1, 4.5 e 4.6 (Quadros 6 e 7). Estes números são a base dos testes exigidos pelo POP-06 e o **oráculo de teste** do protótipo (casos CT01–CT04 do Quadro 14, seção 5.3.6). **Não são medições de um sistema real** — os pesquisadores definiram elementos, pesos e classificações por inferência justificada a partir da descrição do domínio (o próprio TCC trata isso como ameaça à validade e chama a aplicação de demonstrativa).

Todos os valores deste documento foram **recalculados de forma independente** (implementação própria dos dez eixos, do algoritmo de comunidades e das distâncias, a partir dos nós/arestas/fluxos de cada caso) e batem com os quadros do TCC, incluindo as contribuições por eixo, as análises de sensibilidade e as participações percentuais de FLEX+AUT.

## O que mudou em relação à versão anterior do TCC (Entrega 3)

| Item | Antes (Entrega 3) | Agora (TCC revisado) |
|---|---|---|
| Grafo, cargas, pontos críticos | Quadros 7–9 e 14–16 | **Inalterados** (renumerados: Q20/Q22/Q23 e, no CE2, Q57/Q58 no Apêndice C e Q31) |
| `comunidade(v)` (antes `cluster(v)`) | Componente conexo (conectividade fraca) → um único cluster | **Comunidades por modularidade** (CNM, **explorando todos os empates e adotando a divisão de maior Q**), Q = 0,41 com 3 comunidades (CE1) e 0,34 com 4 (CE2) |
| FLEX / AUT | 5,00 / 5,00 nos dois casos | CE1 **3,50 / 1,67**; CE2 **2,76 / 2,50** |
| Fluxos principais F e DEP | O TCC já usava fluxos escolhidos pelos pesquisadores (DEP 2,50 e 3,75); o **projeto** os derivava automaticamente (DEP 3,25 e 3,00) | Fluxos escolhidos **como entrada** do método (RF05) e verificados no grafo (CT09): DEP 2,50 (CE1) e 3,75 (CE2); a derivação automática deixa de valer |
| Assinaturas dos estilos | Implícitas nas distâncias | **Publicadas no Quadro 7** (Microsserviços VOL = 2,0) |
| Distâncias e perfil mais próximo | CE1 Micro 1,368 × Monólito 1,867 (Micro); CE2 Micro 1,221 × Monólito 1,915 (Micro) | CE1 **Monólito 1,051** × Micro 1,605; CE2 **Monólito 1,107** × Micro 1,346 (margem pequena) |
| Quadros | Numeração antiga (Q2–Q20) | Renumerados a cada versão do TCC; ver a tabela de equivalência (Entrega 3 → `TCC_II.pdf` → TCC atual) no fim deste documento. **Os números de Quadro citados neste arquivo, no código e nos testes seguem o TCC atual (2026-11-06).** |
| Saída do método | "Recomendação arquitetural preliminar" | "Perfil de referência mais próximo" como **subsídio** à decisão do arquiteto (RN09) |

---

## Parâmetros comuns aos dois casos (fixtures)

### Limites provisórios de normalização — Quadro 6 do TCC

| Eixo | Métrica bruta | Mínimo | Máximo |
|---|---|---|---|
| CO | N + 2 · bandas ocupadas | 6 | 40 |
| ESC | Proporção de pares independentes | 0 | 1 |
| INT | Proporção de arestas externas | 0 | 1 |
| UX | Grau de entrada médio (Frontstage) | 0 | 3,0\* |
| DISP | 1 − proporção de pontos potencialmente críticos | 0 | 1 |
| FLEX | Isolamento médio dos nós internos | 0 | 1 |
| TR | Proporção volumétrica crítica | 0 | 1 |
| AUT | Proporção de comunidades autônomas | 0 | 1 |
| VOL | Peso médio por aresta | 1 | 3 |
| DEP | Bandas médias por fluxo / 5 | 0,2 | 1 |

\* Limite máximo provisório de UX, adotado como 3,0 nas aplicações demonstrativas e sujeito à validação empírica. `Norm_i = clamp(5 × (M_i − min)/(max − min), 0, 5)` (RN06).

### Assinaturas provisórias de referência — Quadro 7 do TCC

Valores atribuídos pelos pesquisadores com base nas características usualmente associadas a cada estilo; **não resultam de medição empírica** e ainda serão validados por especialistas (protocolo da seção 5.4). Pesos da distância: `p_i = 0,1` para os dez eixos (soma = 1, RN07). Distância máxima possível = 5.

| Eixo | Monólito Modular | Microsserviços | Justificativa (TCC) |
|---|---|---|---|
| CO | 2,0 | 4,0 | Microsserviços envolvem mais unidades implantáveis, integrações e infraestrutura; o Monólito Modular concentra a operação em uma única unidade. |
| ESC | 1,5 | 4,0 | Microsserviços pressupõem poucas ligações diretas entre elementos de uma mesma camada, permitindo escalar partes de forma independente; no Monólito Modular, os módulos compartilham o mesmo processo. |
| INT | 1,0 | 3,5 | Microsserviços comunicam-se por interfaces de rede e integrações com maior frequência; o Monólito Modular resolve a maior parte das interações internamente. |
| UX | 2,5 | 2,5 | A convergência de interações nos pontos de contato não foi considerada característica que diferencia os dois estilos. |
| DISP | 3,0 | 4,0 | O isolamento de falhas em serviços independentes tende a reduzir a concentração de pontos críticos; no Monólito Modular, a unidade de implantação é compartilhada. |
| FLEX | 1,5 | 4,0 | Microsserviços favorecem a evolução independente de partes do sistema; no Monólito Modular, a evolução ocorre dentro da mesma unidade de implantação. |
| TR | 1,5 | 2,5 | Fluxos com resposta imediata distribuídos entre serviços são mais associados a Microsserviços; o valor permanece moderado nos dois estilos. |
| AUT | 1,0 | 4,5 | A autonomia dos módulos é característica central de Microsserviços; no Monólito Modular, os módulos compartilham o processo e, em geral, o banco de dados. |
| VOL | 3,0 | 2,0 | No Monólito Modular, chamadas internas de baixo custo admitem comunicação mais intensa entre módulos; em Microsserviços, a comunicação intensa pela rede tende a ser evitada. |
| DEP | 3,0 | 4,0 | Fluxos em Microsserviços tendem a atravessar mais fronteiras técnicas e organizacionais. |

### Detecção de comunidades — `cluster(v)` (seção 4.4.1)

O TCC revisado define `cluster(v)` como **comunidade estrutural** obtida por detecção de comunidades por modularidade (seção 4.4.1). Especificação para implementação e teste:

- **Grafo de entrada:** versão **não direcionada** do Mapa, com todos os nós (de todas as bandas). O peso da ligação entre dois nós é a **soma das `w(e)`** das arestas existentes entre eles, em qualquer sentido (arestas paralelas e antiparalelas são somadas; self-loop não existe).
- **Modularidade:** `Q = (1/2m) · Σ_ij [A_ij − k_i·k_j/2m] · δ(c_i, c_j)`, com `A_ij` = peso da ligação i–j, `k_i` = soma dos pesos das ligações do nó i, `m` = soma de todos os pesos do grafo e `δ` = 1 se i e j estão na mesma comunidade.
- **Algoritmo:** guloso de Clauset-Newman-Moore (2004): cada nó começa numa comunidade; a cada passo unem-se as duas comunidades cuja fusão dá o maior aumento de Q; para quando nenhuma fusão aumenta Q. Q > ≈ 0,3 indica estrutura de comunidades significativa. **Registrar Q e a partição junto aos resultados** (RF06).
- **Cálculo exato do ganho:** com pesos inteiros, `ΔQ · (2m)² = 2 · (2m · W_ij − K_i · K_j)`, em que `W_ij` é o peso total entre as comunidades i e j e `K_i` a soma dos graus ponderados dos nós de i. Comparar esse **inteiro** (e não o `ΔQ` em ponto flutuante) evita empates falsos ou perdidos por ruído numérico.

**Empates de ganho e a divisão adotada (decisão D9, resolvida pelo TCC de 2026-10-25).** O CNM é guloso e, em alguns passos, vários pares têm exatamente o mesmo ganho; a ordem em que eles são unidos pode levar a divisões finais diferentes. A descrição original do algoritmo não diz como escolher. O TCC (4.4.1) define: **a cada passo, explorar todas as fusões empatadas e, entre as divisões finais assim obtidas, adotar a de maior modularidade** — de modo que o resultado **não depende da ordem em que os nós são listados nem dos rótulos**. Nos dois estudos de caso essa exploração resulta em **duas** divisões possíveis:

| Caso | Divisão adotada (maior Q) | Q | Outra divisão (não adotada) | Q |
|---|---|---|---|---|
| CE1 | {A,E2,P4,S1,TP1,TP4}, {DS,P1,P2,P3,TP2,TP3}, {E1,P5,S2} (3 comunidades) | **0,4117** | {A,E2,P4,TP1,TP4}, {DS,P1,S1,TP2,TP3}, {E1,P5,S2}, {P2,P3} (4 comunidades) | 0,4004 |
| CE2 | {A,E2,P5,TP1}, {DS,P1,S1}, {E1,P4,S2,TP4}, {P2,P3,TP2,TP3} (4 comunidades) | **0,3440** | {A,E2,P5,TP1}, {DS,P1,S1,TP2}, {E1,P4,S2,TP4}, {P2,P3,TP3} (4 comunidades) | 0,3417 |

- O estilo arquitetural mais próximo é o mesmo nas duas divisões de cada caso (Monólito Modular); FLEX, AUT e as distâncias mudam. Os valores deste documento são os da divisão adotada.
- O TCC observa ainda que o guloso **não garante** a divisão de maior modularidade possível: reprocessando os dois casos com o algoritmo de **Louvain** (30 sementes), obtiveram-se divisões de 3 ou 4 comunidades com Q de até 0,412 (CE1, igual à adotada) e 0,364 (CE2); FLEX e AUT variaram entre elas e o estilo mais próximo permaneceu o mesmo em todas as execuções. Recomenda-se registrar em cada aplicação o **algoritmo e o valor de Q**.
- **Para o protótipo:** o `DetectorDeComunidades` implementa a exploração com memória de estados (cada partição é explorada uma vez) e compara o Q das divisões finais em **inteiros exatos**. Os testes comparam a **partição** (conjuntos de nós), provam que existem as duas divisões em cada caso e que a de maior Q é a adotada, e conferem que cadastrar os nós em outra ordem dá o mesmo resultado. Se duas divisões finais distintas tivessem o MESMO Q (caso que o TCC não prevê), adota-se a primeira na ordem de cadastro e o resultado traz um aviso.
- **Para o CT02** ("quatro comunidades, Q ≈ 0,34"): vale para o CE2 em qualquer das duas divisões; o CE1 passa a ter 3 comunidades.


---

## Estudo de Caso 1 — Sistema de Pedidos Digital

Domínio: pedido digital em estabelecimento de alimentação (cardápio via QR Code/app, preparo em cozinha, retirada/mesa/entrega por parceiro, pagamento via gateway externo).

### Nós (15) — Quadro 20 do TCC

| Nó | Tipo | Banda | Descrição |
|---|---|---|---|
| A | Ator/Cliente | Ator | Cliente |
| TP1 | Touchpoint | Frontstage | Cardápio digital |
| TP2 | Touchpoint | Frontstage | Carrinho/Checkout |
| TP3 | Touchpoint | Frontstage | Acompanhamento de status |
| TP4 | Touchpoint | Frontstage | Canal de notificação |
| P1 | Processo | Backstage | Validação do pedido |
| P2 | Processo | Backstage | Envio à cozinha |
| P3 | Processo | Backstage | Preparo do pedido |
| P4 | Processo | Backstage | Expedição/Roteamento |
| P5 | Processo | Backstage | Fechamento de conta |
| S1 | Suporte | Suporte | Gestão de cardápio/estoque |
| S2 | Suporte | Suporte | Processamento de pagamento |
| DS | Dados | Suporte | Repositório de pedidos |
| E1 | Externo | Externo | Gateway de pagamento |
| E2 | Externo | Externo | Plataforma de entrega |

### Arestas (21) — Quadro 22 do TCC

| Origem | Destino | Tipo | Peso |
|---|---|---|---|
| A | TP1 | SYN | 3 |
| TP1 | S1 | SYN | 2 |
| S1 | DS | SYN | 2 |
| TP1 | TP2 | SYN | 3 |
| TP2 | P1 | SYN | 3 |
| P1 | DS | SYN | 3 |
| P1 | P2 | SYN | 3 |
| P2 | DS | SYN | 2 |
| P2 | P3 | **CRI** | 3 |
| P3 | P4 | SYN | 2 |
| P4 | TP4 | ASY | 2 |
| TP4 | A | ASY | 2 |
| P4 | E2 | ASY | 1 |
| E2 | A | ASY | 1 |
| TP2 | TP3 | SYN | 2 |
| TP3 | DS | SYN | 2 |
| P3 | P5 | ASY | 1 |
| P5 | S2 | SYN | 3 |
| S2 | E1 | SYN | 3 |
| E1 | P5 | SYN | 2 |
| P5 | DS | SYN | 2 |

Soma de todos os pesos: **47**. Única aresta CRI: P2→P3 (peso 3). Nenhuma aresta BAT neste caso. Critério de atribuição de `w(e)` (Quadro 21, igual nos dois casos): **3 · Alta** (100% das instâncias, caminho obrigatório e sequencial), **2 · Média** (recorrente, função de apoio), **1 · Baixa** (condicional ou periódica/batch).

### Carga por nó e identificação de gargalo (ponto potencialmente crítico) — Quadro 23 do TCC

| Nó | Banda | Carga | μ da banda | σ da banda | Gargalo (pot. crítico)? |
|---|---|---|---|---|---|
| A | Ator | 6 | 6,00 | 0,00 | Não |
| TP1 | Frontstage | 8 | 6,00 | 2,00 | Não |
| TP2 | Frontstage | 8 | 6,00 | 2,00 | Não |
| TP3 | Frontstage | 4 | 6,00 | 2,00 | Não |
| TP4 | Frontstage | 4 | 6,00 | 2,00 | Não |
| P1 | Backstage | 9 | 7,20 | 1,47 | **Sim** |
| P2 | Backstage | 8 | 7,20 | 1,47 | Não |
| P3 | Backstage | 6 | 7,20 | 1,47 | Não |
| P4 | Backstage | 5 | 7,20 | 1,47 | Não |
| P5 | Backstage | 8 | 7,20 | 1,47 | Não |
| S1 | Suporte | 4 | 7,00 | 2,94 | Não |
| S2 | Suporte | 6 | 7,00 | 2,94 | Não |
| DS | Suporte | 11 | 7,00 | 2,94 | **Sim** |
| E1 | Externo | 5 | 3,50 | 1,50 | Não |
| E2 | Externo | 2 | 3,50 | 1,50 | Não |

Pontos potencialmente críticos (gargalos) identificados: **P1** (validação do pedido) e **DS** (repositório de pedidos) — caso de teste **CT01** do TCC (seção 5.3.6). Critério: `carga(v) > μ_banda + σ_banda`, desvio padrão populacional.

### Fluxos principais F (entrada do método) — seção 6.6

No TCC revisado os fluxos principais são **definidos pelos pesquisadores e fornecidos como entrada** (RF05); o sistema só verifica se cada fluxo possui caminho dirigido no grafo (CT09). Cada fluxo é uma sequência de nós; as bandas contam origem e destino. Todos os fluxos abaixo têm caminho dirigido no grafo (cada par consecutivo é uma aresta existente), logo o teste de fluxo inválido (CT09) não dispara aqui.

| Fluxo | Caminho (entrada) | Bandas distintas | Nº |
|---|---|---|---|
| **F1 – Consulta de cardápio** | A → TP1 → S1 → DS | Ator, Frontstage, Suporte | 3 |
| **F2 – Pedido e preparo** | A → TP1 → TP2 → P1 → P2 → P3 → P4 → TP4 | Ator, Frontstage, Backstage | 3 |
| **F3 – Entrega** | P4 → E2 → A | Backstage, Externo, Ator | 3 |
| **F4 – Pagamento** | P3 → P5 → S2 → E1 → P5 → DS | Backstage, Suporte, Externo | 3 |

Média = 3,0 bandas por fluxo → `DEP_bruto = 3,0/5 = 0,6` → normalizado **2,50** (limites 0,2–1).

Observação do TCC: no fluxo F4 o retorno E1→P5 representa a resposta do gateway ao fechamento de conta, seguida da persistência P5→DS — a representação foi escolhida para ser compatível com as arestas efetivamente registradas no Mapa. O fluxo F3 começa em P4 (não no Ator): fluxos podem começar em qualquer nó, desde que exista caminho dirigido.

### Comunidades estruturais e modularidade (seção 4.4.1; 6.8.1)

Algoritmo guloso de Clauset-Newman-Moore sobre o grafo não direcionado ponderado (ver "Parâmetros comuns"). `m` = soma dos pesos = 47. Três comunidades, **Q = 0,4117** (publicado: 0,41):

| Comunidade | Nós | Arestas de saída para outras comunidades | Nº de saídas |
|---|---|---|---|
| **C1** | A, E2, P4, S1, TP1, TP4 | S1→DS; TP1→TP2 | 2 |
| **C2** | DS, P1, P2, P3, TP2, TP3 | P3→P4; P3→P5 | 2 |
| **C3** | E1, P5, S2 | P5→DS | 1 |

Rótulos (C1 "Entrada, entrega e cardápio", C2 "Pedido, preparo e dados", C3 "Pagamento") e numeração são do TCC; os testes devem comparar a **partição** (conjuntos de nós), não o índice de cada comunidade.

### Isolamento dos nós internos — Quadro 24 do TCC

`isolamento(v) = 1 − arestas_para_outras_comunidades(v) / grau_total(v)`, para os 8 nós internos (Backstage ∪ Suporte).

| Nó | Comunidade | Grau total | Arestas para outras comunidades | `isolamento(v)` |
|---|---|---|---|---|
| DS | C2 | 5 | 2 | 0,600 |
| P1 | C2 | 3 | 0 | 1,000 |
| P2 | C2 | 3 | 0 | 1,000 |
| P3 | C2 | 3 | 2 | 0,333 |
| P4 | C1 | 3 | 1 | 0,667 |
| P5 | C3 | 4 | 2 | 0,500 |
| S1 | C1 | 2 | 1 | 0,500 |
| S2 | C3 | 2 | 0 | 1,000 |

`FLEX = 5,600 / 8 = 0,7000` → normalizado `5 × 0,7000 = 3,500` = **3,50**. `AUT = 1/3 = 0,3333` (só a comunidade C3 tem no máximo uma saída) → normalizado `5 × 0,3333 = 1,667` ≈ **1,67**.

### Resultado esperado das dez métricas — Quadro 25 do TCC

| Eixo | Métrica bruta | Mín | Máx | Normalizado (0–5) |
|---|---|---|---|---|
| CO | 25 | 6 | 40 | **2,79** |
| ESC | 0,65 | 0 | 1 | **3,25** |
| INT | 0,1905 | 0 | 1 | **0,95** |
| UX | 1,00 | 0 | 3\* | **1,67** |
| DISP | 0,75 | 0 | 1 | **3,75** |
| FLEX | 0,700 | 0 | 1 | **3,50** |
| TR | 0,0638 | 0 | 1 | **0,32** |
| AUT | 0,3333 | 0 | 1 | **1,67** (≈ 1,667) |
| VOL | 2,2381 | 1 | 3 | **3,10** (≈ 3,095) |
| DEP | 0,6 | 0,2 | 1 | **2,50** |

\* limite máximo provisório de UX (3,0), igual nos dois estudos de caso e sujeito à validação empírica.

**Demonstração resumida de cada eixo** (conferir com o código quando implementado):

- **CO** = N + 2·|bandas ocupadas| = 15 + 2·5 = 25 → `5 × (25 − 6)/(40 − 6)` = 2,794.
- **ESC**: 20 pares possíveis nas cinco bandas (0 + 6 + 10 + 3 + 1) e 7 pares com aresta direta dentro da banda (DS–S1, P1–P2, P2–P3, P3–P4, P3–P5, TP1–TP2, TP2–TP3) → 13/20 = 0,65 → 3,25.
- **INT**: 4 arestas ligadas à banda Externo (P4→E2, E2→A, S2→E1, E1→P5) → 4/21 = 0,1905 → 0,95.
- **UX**: graus de entrada dos Touchpoints (TP1=1, TP2=1, TP3=1, TP4=1) → média 1 → `5 × 1/3` = 1,67.
- **DISP**: 8 nós internos (Backstage 5 + Suporte 3), 2 críticos (P1, DS) → 1 − 2/8 = 0,75 → 3,75.
- **FLEX** e **AUT**: ver as seções de comunidades e isolamento acima.
- **TR**: única aresta CRI P2→P3 (w = 3); Σw = 47 → 3/47 = 0,0638 → 0,32.
- **VOL**: 47/21 = 2,2381 → `5 × (2,2381 − 1)/(3 − 1)` = 3,095 ≈ 3,10.
- **DEP**: ver a seção de fluxos principais (média 3,0 → 0,6 → `5 × (0,6 − 0,2)/0,8` = 2,50).

### Distâncias até os perfis de referência — Quadros 26 e 27 do TCC

Pesos `p_i = 0,1`; perfis do Quadro 7 (ver "Parâmetros comuns"). A contribuição de cada eixo é `p_i × (Norm_i^Serviço − Norm_i^Estilo)²`, calculada com os **normalizados sem arredondar** (usar os já arredondados a 2 casas muda a 4ª casa). Reproduz as tabelas do TCC termo a termo, com uma única diferença de 0,0001: contribuição de CO até o Monólito = 0,0631 recalculada contra 0,0630 publicada (o TCC parte do normalizado intermediário 2,794).

| Eixo | Serviço (norm.) | Monólito Modular | Contribuição | Microsserviços | Contribuição |
|---|---|---|---|---|---|
| CO | 2,79 | 2,0 | 0,0631 | 4,0 | 0,1454 |
| ESC | 3,25 | 1,5 | 0,3063 | 4,0 | 0,0563 |
| INT | 0,95 | 1,0 | 0,0002 | 3,5 | 0,6490 |
| UX | 1,67 | 2,5 | 0,0694 | 2,5 | 0,0694 |
| DISP | 3,75 | 3,0 | 0,0563 | 4,0 | 0,0063 |
| FLEX | 3,50 | 1,5 | 0,4000 | 4,0 | 0,0250 |
| TR | 0,32 | 1,5 | 0,1394 | 2,5 | 0,4756 |
| AUT | 1,67 | 1,0 | 0,0444 | 4,5 | 0,8028 |
| VOL | 3,10 | 3,0 | 0,0009 | 2,0 | 0,1200 |
| DEP | 2,50 | 3,0 | 0,0250 | 4,0 | 0,2250 |
| **Soma** | | | **1,1050** | | **2,5747** |

`d(Serviço, Monólito Modular) = √1,1050 = 1,051` · `d(Serviço, Microsserviços) = √2,5747 = 1,605`.

| Estilo de referência | Distância `d(Servico, Estilo)` | % do máximo possível (5) |
|---|---|---|
| Monólito Modular (provisório) | **1,051** | 21,0% |
| Microsserviços (provisório) | **1,605** | 32,1% |

**Perfil de referência mais próximo (subsídio à decisão do arquiteto, RN09): Monólito Modular**; diferença entre as distâncias = 0,553 (≈ 11% da escala). Não é uma decisão: o arquiteto deve considerar custos, competências da equipe, previsão de crescimento e demais restrições que o Mapa não captura.

**Leitura do TCC (6.10.1/6.10.2):** a distância até Microsserviços é formada principalmente por AUT (0,803), INT (0,649) e TR (0,476), cerca de 75% da soma (poucas integrações externas, poucas interações de tempo real e comunidades com baixa autonomia — só uma das três depende de uma única saída); a distância até o Monólito vem de FLEX (0,400) e ESC (0,306) — o serviço tem mais isolamento entre as comunidades e mais independência entre elementos da mesma banda que o previsto para esse estilo. Independentemente do perfil, P1 e DS merecem atenção por concentrarem carga; são candidatos a táticas de disponibilidade, a avaliar pelo arquiteto junto aos requisitos não funcionais do Quadro 18.

### Análise de sensibilidade (seção 6.10.1, documentada no próprio TCC)

| Configuração | Monólito Modular | Microsserviços | Perfil mais próximo |
|---|---|---|---|
| **Versão inicial** do método (cluster por conectividade fraca → FLEX = AUT = 5,00), 10 eixos | 1,867 | 1,368 | Microsserviços |
| Versão inicial, 8 eixos (sem FLEX/AUT), `p_i = 1/8` | 0,909 | 1,478 | Monólito Modular (a relação se invertia) |
| **Versão revisada** (comunidades), 10 eixos | 1,051 | 1,605 | Monólito Modular |
| Versão revisada, 8 eixos, `p_i = 1/8` | 0,909 | 1,478 | Monólito Modular |

Participação de FLEX+AUT na soma dos termos da distância: versão inicial 81,0% (Monólito); versão revisada 40,2% (Monólito) e 32,1% (Microsserviços). Na versão revisada o resultado deixou de depender de FLEX/AUT (mesmo perfil mais próximo com 10 e com 8 eixos).

**Sensibilidade adicional do TCC de 2026-10-25 (simulações; não são parte do oráculo dos testes):** somando a cada valor das duas assinaturas uma perturbação uniforme de até ±0,5 (20.000 simulações) o Monólito Modular permaneceu o mais próximo em todas; com ±1,0 a indicação mudou em 2,0% delas. Nenhuma das 80 alterações isoladas de ±0,5 ou ±1,0 em um único valor de assinatura inverteu o resultado, nem a variação da intensidade de cada uma das 21 arestas. O TCC ressalta que a suposição (perturbações uniformes e independentes) é uma das possíveis e não substitui a validação por especialistas.

### Matriz de rastreabilidade — Quadro 28 do TCC

| Elemento do Blueprint (etapa) | Nó/Aresta | Métrica | Eixo(s) | Contribuição |
|---|---|---|---|---|
| Validação (etapa 2) | P1 (gargalo) | `carga(v)`, `gargalo(v)` | DISP, ESC | Concentra interações e é identificado como ponto potencialmente crítico. |
| Repositório (etapas 1, 2, 3 e 5) | DS (crítico) | `carga(v)`, `gargalo(v)` | DISP | Concentração de operações de leitura e escrita. |
| Envio à cozinha → preparo (etapa 3) | P2→P3 (CRI) | `tipo(e)`, `w(e)` | TR | Única interação classificada como crítica. |
| Notificação e entrega (etapa 4) | P4→TP4 (ASY); P4→E2→A (ASY) | `tipo(e)`, `w(e)` | VOL, INT, FLEX | Representa comunicação desacoplada e integração externa. |
| Pagamento (etapa 5) | P5→S2→E1→P5 (SYN) | `E_Externo/M` | INT, ESC | Representa a fronteira de integração externa e contribui para os eixos INT e ESC. |
| Consulta de cardápio/estoque (etapa 1) | TP1→S1→DS (SYN) | `carga(v)` | UX, DEP | Representa interação frequente do fluxo inicial e contribui para a carga de DS. |
| Pagamento (etapa 5) | Comunidade C3 (P5, S2, E1) | `isolamento(v)`; `saídas_externas(c)` | FLEX, AUT | Comunidade com uma única saída (P5→DS), que contribui para a autonomia dos módulos. |

Este é o formato de dado que a funcionalidade "Matriz de Rastreabilidade" (tela T8 do POP-09) precisa produzir a partir de qualquer mapa — não é uma tabela fixa, é derivada dos nós/arestas/comunidades relevantes. O TCC revisado inclui a coluna do **elemento do Blueprint (com a etapa)**, vários eixos por linha, gargalos de qualquer banda e linhas de **comunidade** (FLEX/AUT); a implementação atual cobre menos que isso (DV-07).

### Observações e limitações (seção 6.12)

A versão inicial de `cluster(v)` (conectividade fraca) gerava um único componente com os 15 nós, FLEX e AUT máximos e FLEX+AUT respondendo por 81,0% da distância até o Monólito; foi tratada com a detecção de comunidades por modularidade (seção 4.4.1), que identificou três comunidades coerentes com as etapas do serviço (entrada, entrega e cardápio; pedido, preparo e dados; pagamento). Permanecem as limitações: (a) elementos, requisitos e pesos `w(e)` foram definidos por inferência dos pesquisadores, não por observação de um sistema real; (b) o critério de pontos potencialmente críticos e os limites de normalização (em especial o máximo de UX) são provisórios; (c) as assinaturas de Monólito Modular e Microsserviços foram definidas para o experimento e ainda serão validadas por especialistas (seção 5.4); (d) o algoritmo guloso admite empates no ganho de modularidade que levam a divisões diferentes; nos dois estudos de caso há duas divisões possíveis e foi adotada a de maior Q (seção 4.4.1) — por isso o algoritmo, a divisão obtida e o valor de Q devem ser sempre registrados.

---

## Estudo de Caso 2 — Sistema de Agendamento de Clínica

Domínio: agendamento de consultas em clínica médica — acesso ao portal, consulta de horários por especialidade, confirmação, lembrete, check-in no dia do atendimento e sincronização com prontuário/convênio externo.

### Nós (15) — Quadro 57 do TCC

| Nó | Tipo | Banda | Descrição |
|---|---|---|---|
| A | Ator/Cliente | Ator | Paciente |
| TP1 | Touchpoint | Frontstage | Portal de Agendamento |
| TP2 | Touchpoint | Frontstage | Tela de Seleção de Horário |
| TP3 | Touchpoint | Frontstage | Tela de Confirmação |
| TP4 | Touchpoint | Frontstage | Canal de Notificação |
| P1 | Processo | Backstage | Verificação de Disponibilidade |
| P2 | Processo | Backstage | Registro do Agendamento |
| P3 | Processo | Backstage | Geração da Confirmação |
| P4 | Processo | Backstage | Job de Lembrete |
| P5 | Processo | Backstage | Check-in da Consulta |
| S1 | Suporte | Suporte | Autenticação/Cadastro |
| S2 | Suporte | Suporte | Serviço de Notificações |
| DS | Dados | Suporte | Repositório de Agendas |
| E1 | Externo | Externo | Gateway SMS/E-mail |
| E2 | Externo | Externo | Prontuário/Convênio |

### Arestas (21) — Quadro 58 do TCC

| Origem | Destino | Tipo | Peso |
|---|---|---|---|
| A | TP1 | SYN | 3 |
| TP1 | S1 | SYN | 2 |
| S1 | DS | SYN | 2 |
| TP1 | TP2 | SYN | 3 |
| TP2 | P1 | SYN | 3 |
| P1 | DS | SYN | 3 |
| TP2 | TP3 | SYN | 3 |
| TP3 | P2 | SYN | 3 |
| P2 | DS | SYN | 3 |
| P2 | P3 | ASY | 2 |
| P3 | S2 | ASY | 2 |
| S2 | E1 | ASY | 2 |
| E1 | TP4 | ASY | 2 |
| P2 | P4 | BAT | 1 |
| P4 | S2 | BAT | 1 |
| S2 | E1 | BAT | 1 |
| E1 | TP4 | BAT | 1 |
| TP4 | A | ASY | 2 |
| A | P5 | **CRI** | 3 |
| P5 | DS | SYN | 2 |
| P5 | E2 | BAT | 1 |

Soma de todos os pesos: **45**. Única aresta CRI: A→P5 (check-in, peso 3). Há **duas** arestas paralelas entre S2→E1 (uma ASY peso 2, uma BAT peso 1) e entre E1→TP4 (mesma dualidade) — o fluxo de confirmação e o de lembrete compartilham o mesmo canal técnico (S2, E1, TP4), mas são interações distintas. Confirma a decisão D6 (arestas paralelas entre o mesmo par de nós são permitidas); na modularidade elas são **somadas** (peso do par S2–E1 = 3; E1–TP4 = 3). Conferido por reconstrução independente: somando os pesos das arestas incidentes a cada nó, a carga bate nó a nó com o Quadro 31.

### Carga por nó e identificação de gargalo (ponto potencialmente crítico) — Quadro 31 do TCC

| Nó | Banda | Carga | μ da banda | σ da banda | Gargalo (pot. crítico)? |
|---|---|---|---|---|---|
| A | Ator | 8 | 8,00 | 0,00 | Não |
| TP1 | Frontstage | 8 | 7,00 | 1,58 | Não |
| TP2 | Frontstage | 9 | 7,00 | 1,58 | **Sim** |
| TP3 | Frontstage | 6 | 7,00 | 1,58 | Não |
| TP4 | Frontstage | 5 | 7,00 | 1,58 | Não |
| P1 | Backstage | 6 | 5,40 | 2,33 | Não |
| P2 | Backstage | 9 | 5,40 | 2,33 | **Sim** |
| P3 | Backstage | 4 | 5,40 | 2,33 | Não |
| P4 | Backstage | 2 | 5,40 | 2,33 | Não |
| P5 | Backstage | 6 | 5,40 | 2,33 | Não |
| S1 | Suporte | 4 | 6,67 | 2,49 | Não |
| S2 | Suporte | 6 | 6,67 | 2,49 | Não |
| DS | Suporte | 10 | 6,67 | 2,49 | **Sim** |
| E1 | Externo | 6 | 3,50 | 2,50 | Não |
| E2 | Externo | 1 | 3,50 | 2,50 | Não |

Pontos potencialmente críticos (gargalos): **TP2** (seleção de horário), **P2** (registro do agendamento) e **DS** (repositório de agendas) — três nós; TP2 é Frontstage e por isso **não entra em DISP** (que só considera Backstage ∪ Suporte), mas é destacado no Mapa (halo) e aparece na matriz (DISP, UX).

### Fluxos principais F (entrada do método) — seção 7.2

No TCC revisado os fluxos principais são **definidos pelos pesquisadores e fornecidos como entrada** (RF05); o sistema só verifica se cada fluxo possui caminho dirigido no grafo (CT09). Cada fluxo é uma sequência de nós; as bandas contam origem e destino. Todos os fluxos abaixo têm caminho dirigido no grafo (cada par consecutivo é uma aresta existente), logo o teste de fluxo inválido (CT09) não dispara aqui.

| Fluxo | Caminho (entrada) | Bandas distintas | Nº |
|---|---|---|---|
| **F1 – Agendamento** | A → TP1 → TP2 → TP3 → P2 → DS | Ator, Frontstage, Backstage, Suporte | 4 |
| **F2 – Confirmação** | P2 → P3 → S2 → E1 → TP4 → A | Backstage, Suporte, Externo, Frontstage, Ator | 5 |
| **F3 – Lembrete** | P2 → P4 → S2 → E1 → TP4 → A | Backstage, Suporte, Externo, Frontstage, Ator | 5 |
| **F4a – Check-in e atualização interna** | A → P5 → DS | Ator, Backstage, Suporte | 3 |
| **F4b – Sincronização externa** | A → P5 → E2 | Ator, Backstage, Externo | 3 |

Média = 4,0 bandas por fluxo → `DEP_bruto = 4,0/5 = 0,8` → normalizado **3,75** (limites 0,2–1).

Os fluxos F4a e F4b são duas interações distintas a partir do check-in: atualização do estado no repositório interno e sincronização com o prontuário/convênio. F2 e F3 não começam no Ator (começam no processo P2). Nenhum dos fluxos é derivado automaticamente: se o usuário não cadastrar fluxos, `F = ∅` e DEP é "não aplicável".

### Comunidades estruturais e modularidade (seção 4.4.1; 7.4)

Algoritmo guloso de Clauset-Newman-Moore sobre o grafo não direcionado ponderado (ver "Parâmetros comuns"). `m` = soma dos pesos = 45. Quatro comunidades, **Q = 0,3440** (publicado: 0,34):

| Comunidade | Nós | Arestas de saída para outras comunidades | Nº de saídas |
|---|---|---|---|
| **C1** | A, E2, P5, TP1 | P5→DS; TP1→S1; TP1→TP2 | 3 |
| **C2** | DS, P1, S1 | (nenhuma) | 0 |
| **C3** | E1, P4, S2, TP4 | TP4→A | 1 |
| **C4** | P2, P3, TP2, TP3 | TP2→P1; P2→DS; P2→P4; P3→S2 | 4 |

Rótulos do TCC: C1 "Acesso e atendimento", C2 "Disponibilidade e dados", C3 "Notificações", C4 "Seleção, registro e confirmação". Caso de teste **CT02** do TCC: quatro comunidades, Q ≈ 0,34.

### Isolamento dos nós internos — Quadro 32 do TCC

`isolamento(v) = 1 − arestas_para_outras_comunidades(v) / grau_total(v)`, para os 8 nós internos (Backstage ∪ Suporte).

| Nó | Comunidade | Grau total | Arestas para outras comunidades | `isolamento(v)` |
|---|---|---|---|---|
| DS | C2 | 4 | 2 | 0,500 |
| P1 | C2 | 2 | 1 | 0,500 |
| P2 | C4 | 4 | 2 | 0,500 |
| P3 | C4 | 2 | 1 | 0,500 |
| P4 | C3 | 2 | 1 | 0,500 |
| P5 | C1 | 3 | 1 | 0,667 |
| S1 | C2 | 2 | 1 | 0,500 |
| S2 | C3 | 4 | 1 | 0,750 |

`FLEX = 4,417 / 8 = 0,5521` → normalizado `5 × 0,5521 = 2,760` ≈ **2,76**. `AUT = 2/4 = 0,50` (comunidades com no máximo uma saída: C2, C3) → **2,50**.

### Resultado esperado das dez métricas — Quadro 33 do TCC

| Eixo | Métrica bruta | Mín | Máx | Normalizado (0–5) |
|---|---|---|---|---|
| CO | 25 | 6 | 40 | **2,79** |
| ESC | 0,75 | 0 | 1 | **3,75** |
| INT | 0,2381 | 0 | 1 | **1,19** |
| UX | 1,25 | 0 | 3\* | **2,08** |
| DISP | 0,75 | 0 | 1 | **3,75** |
| FLEX | 0,552 | 0 | 1 | **2,76** (≈ 2,760) |
| TR | 0,0667 | 0 | 1 | **0,33** |
| AUT | 0,50 | 0 | 1 | **2,50** |
| VOL | 2,1429 | 1 | 3 | **2,86** (≈ 2,857) |
| DEP | 0,8 | 0,2 | 1 | **3,75** |

\* limite máximo provisório de UX (3,0), igual nos dois estudos de caso e sujeito à validação empírica.

**Demonstração resumida de cada eixo** (conferir com o código quando implementado):

- **CO** = 15 + 2·5 = 25 → 2,79 (igual ao Estudo de Caso 1: mesmo N e mesmas bandas).
- **ESC**: 20 pares possíveis e 5 com aresta direta dentro da banda (DS–S1, P2–P3, P2–P4, TP1–TP2, TP2–TP3) → 15/20 = 0,75 → 3,75. As arestas paralelas S2→E1 e E1→TP4 cruzam bandas (Suporte/Externo/Frontstage) e não entram aqui.
- **INT**: 5 arestas ligadas à banda Externo (S2→E1, E1→TP4, S2→E1, E1→TP4, P5→E2) → 5/21 = 0,2381 → 1,19.
- **UX**: graus de entrada (TP1=1, TP2=1, TP3=1, TP4=2; TP4 recebe confirmação e lembrete) → 5/4 = 1,25 → `5 × 1,25/3` = 2,083 ≈ 2,08.
- **DISP**: 8 nós internos; dos três críticos só P2 e DS são Backstage/Suporte (TP2 é Frontstage e não entra) → 1 − 2/8 = 0,75 → 3,75.
- **FLEX** e **AUT**: ver as seções de comunidades e isolamento acima.
- **TR**: única aresta CRI A→P5 (w = 3); Σw = 45 → 3/45 = 0,0667 → 0,33.
- **VOL**: 45/21 = 2,1429 → `5 × (2,1429 − 1)/2` = 2,857 ≈ 2,86.
- **DEP**: ver a seção de fluxos principais (média 4,0 → 0,8 → `5 × (0,8 − 0,2)/0,8` = 3,75).

### Distâncias até os perfis de referência — Quadros 34 e 35 do TCC

Pesos `p_i = 0,1`; perfis do Quadro 7 (ver "Parâmetros comuns"). A contribuição de cada eixo é `p_i × (Norm_i^Serviço − Norm_i^Estilo)²`, calculada com os **normalizados sem arredondar** (usar os já arredondados a 2 casas muda a 4ª casa). Reproduz as tabelas do TCC termo a termo, com uma única diferença de 0,0001: contribuição de CO até o Monólito = 0,0631 recalculada contra 0,0630 publicada (o TCC parte do normalizado intermediário 2,794).

| Eixo | Serviço (norm.) | Monólito Modular | Contribuição | Microsserviços | Contribuição |
|---|---|---|---|---|---|
| CO | 2,79 | 2,0 | 0,0631 | 4,0 | 0,1454 |
| ESC | 3,75 | 1,5 | 0,5063 | 4,0 | 0,0063 |
| INT | 1,19 | 1,0 | 0,0036 | 3,5 | 0,5334 |
| UX | 2,08 | 2,5 | 0,0174 | 2,5 | 0,0174 |
| DISP | 3,75 | 3,0 | 0,0563 | 4,0 | 0,0063 |
| FLEX | 2,76 | 1,5 | 0,1589 | 4,0 | 0,1537 |
| TR | 0,33 | 1,5 | 0,1361 | 2,5 | 0,4694 |
| AUT | 2,50 | 1,0 | 0,2250 | 4,5 | 0,4000 |
| VOL | 2,86 | 3,0 | 0,0020 | 2,0 | 0,0735 |
| DEP | 3,75 | 3,0 | 0,0563 | 4,0 | 0,0063 |
| **Soma** | | | **1,2248** | | **1,8115** |

`d(Serviço, Monólito Modular) = √1,2248 = 1,107` · `d(Serviço, Microsserviços) = √1,8115 = 1,346`.

| Estilo de referência | Distância `d(Servico, Estilo)` | % do máximo possível (5) |
|---|---|---|
| Monólito Modular (provisório) | **1,107** | 22,1% |
| Microsserviços (provisório) | **1,346** | 26,9% |

**Perfil de referência mais próximo (subsídio à decisão do arquiteto, RN09): Monólito Modular**; diferença entre as distâncias = 0,239 (≈ 4,8% da escala). Não é uma decisão: o arquiteto deve considerar custos, competências da equipe, previsão de crescimento e demais restrições que o Mapa não captura.

**Leitura do TCC (7.5):** o serviço tem perfil intermediário entre os dois estilos e a comparação, isoladamente, pouco os diferencia (margem de ≈ 4,8% da escala, contra ≈ 11% no Estudo de Caso 1) — situação em que a análise do arquiteto é mais importante. A distância até Microsserviços vem de INT (0,533), TR (0,469) e AUT (0,400), cerca de 77% da soma; a distância até o Monólito vem de ESC (0,506), AUT (0,225) e FLEX (0,159), cerca de 73%. A comunidade de notificações (C3) tem isolamento elevado e pode justificar tratamento separado. TP2, P2 e DS merecem atenção por concentrarem carga, considerando os requisitos não funcionais do Quadro 29 (em especial o controle de concorrência na reserva de horários).

### Análise de sensibilidade (seção 7.5, documentada no próprio TCC)

| Configuração | Monólito Modular | Microsserviços | Perfil mais próximo |
|---|---|---|---|
| **Versão inicial** do método (cluster por conectividade fraca → FLEX = AUT = 5,00), 10 eixos | 1,915 | 1,176 | Microsserviços |
| Versão inicial, 8 eixos (sem FLEX/AUT), `p_i = 1/8` | 1,025 | 1,254 | Monólito Modular (a relação se invertia) |
| **Versão revisada** (comunidades), 10 eixos | 1,107 | 1,346 | Monólito Modular |
| Versão revisada, 8 eixos, `p_i = 1/8` | 1,025 | 1,254 | Monólito Modular |

Participação de FLEX+AUT na soma dos termos da distância: versão inicial 77,1% (Monólito); versão revisada 31,3% (Monólito) e 30,6% (Microsserviços). Na versão revisada o resultado deixou de depender de FLEX/AUT (mesmo perfil mais próximo com 10 e com 8 eixos).

**Sensibilidade adicional do TCC de 2026-10-25 (simulações; não são parte do oráculo dos testes):** como a margem (0,239) é menor que a do CE1, a mesma verificação foi aplicada: com perturbações uniformes de até ±0,25, ±0,5 e ±1,0 nas assinaturas (20.000 simulações cada) a indicação mudou em 0,0%, 3,2% e 19,2% dos casos; nenhuma das 80 alterações isoladas de um valor de assinatura inverteu o resultado; retirando cada eixo individualmente o resultado também não se inverteu, mas a margem caiu para 0,027 sem INT; nenhuma das 42 variações de intensidade de uma única aresta (comunidades recalculadas) alterou a indicação.

### Matriz de rastreabilidade — Quadro 36 do TCC

| Elemento do Blueprint (etapa) | Nó/Aresta | Métrica | Eixo(s) | Contribuição |
|---|---|---|---|---|
| Seleção de especialidade e horário | TP2 (gargalo) | `carga(v)`, `gargalo(v)` | DISP, UX | Concentra interações do Frontstage e é identificado como ponto potencialmente crítico. |
| Registro do agendamento | P2 (gargalo) | `carga(v)`, `gargalo(v)` | DISP, ESC | Concentra interações relacionadas ao registro e aos processos posteriores do agendamento. |
| Repositório de agendas | DS (crítico) | `carga(v)`, `gargalo(v)` | DISP | Concentra operações de leitura e escrita do estado das agendas. |
| Confirmação do agendamento | P2→P3→S2→E1→TP4 (ASY) | `tipo(e)`, `w(e)` | TR, VOL, INT | Representa comunicação desacoplada e integração com serviço externo de notificações. |
| Lembrete da consulta | P2→P4→S2→E1→TP4 (BAT) | `tipo(e)`, `w(e)` | VOL, DEP | Representa uma rotina programada tolerante a atraso operacional. |
| Check-in | A→P5 (CRI) | `tipo(e)`, `w(e)` | TR | Representa uma interação com impacto imediato sobre a continuidade do atendimento. |
| Sincronização externa | P5→E2 (BAT) | `tipo(e)`, `w(e)` | INT, DEP | Representa a comunicação com o sistema externo de prontuário ou convênio. |
| Portal e seleção de horário | TP1→TP2 | `carga(v)` | UX, DEP | Representa a interação inicial do paciente e contribui para a estrutura dos fluxos de agendamento. |
| Confirmação e lembrete (etapas 4 e 5) | Comunidade C3 (TP4, P4, S2, E1) | `isolamento(v)`; `saídas_externas(c)` | FLEX, AUT | Agrupamento de notificações com uma única saída (TP4→A), que contribui para a autonomia dos módulos. |

Este é o formato de dado que a funcionalidade "Matriz de Rastreabilidade" (tela T8 do POP-09) precisa produzir a partir de qualquer mapa — não é uma tabela fixa, é derivada dos nós/arestas/comunidades relevantes. O TCC revisado inclui a coluna do **elemento do Blueprint (com a etapa)**, vários eixos por linha, gargalos de qualquer banda e linhas de **comunidade** (FLEX/AUT); a implementação atual cobre menos que isso (DV-07).

### Observações e limitações (seção 7.7)

A definição inicial de `cluster(v)` também resultou em um único componente e em FLEX/AUT máximos; a repetição em outro domínio confirmou que era limitação da definição de agrupamento, não do sistema de pedidos. Com as comunidades por modularidade o Mapa divide-se em quatro comunidades e as métricas passam a diferenciar os dois serviços. Permanecem as limitações da seção 6.12 (inferência dos pesquisadores, critérios e limites provisórios, assinaturas a validar por especialistas) e, como a diferença entre as distâncias é pequena, o resultado é mais sensível a ajustes nas assinaturas. Os resultados são uma aplicação demonstrativa do SBAR, base para o protótipo e para a validação do método.

---

## Comparação entre os Estudos de Caso 1 e 2 — Quadro 39 do `TCC_II.pdf` (no TCC atual, o Quadro 48 reúne os três casos)

| Indicador | Estudo de Caso 1 | Estudo de Caso 2 |
|---|---|---|
| Nós | 15 | 15 |
| Arestas | 21 | 21 |
| Bandas utilizadas | 5 | 5 |
| CO | 2,79 | 2,79 |
| ESC | 3,25 | 3,75 |
| INT | 0,95 | 1,19 |
| UX | 1,67 | 2,08 |
| DISP | 3,75 | 3,75 |
| FLEX | 3,50 | 2,76 |
| TR | 0,32 | 0,33 |
| AUT | 1,67 | 2,50 |
| VOL | 3,10 | 2,86 |
| DEP | 2,50 | 3,75 |
| Modularidade Q | 0,41 | 0,34 |
| Distância — Monólito Modular | 1,051 | 1,107 |
| Distância — Microsserviços | 1,605 | 1,346 |
| Diferença entre as distâncias | 0,553 | 0,239 |
| Perfil de referência mais próximo | Monólito Modular | Monólito Modular (margem pequena) |
| Pontos potencialmente críticos internos | P1 e DS | P2 e DS |

Por que os dois casos coincidem em CO e divergem nos demais eixos (seção 7.8 do TCC):

- **CO idêntico** — mesmo número de nós (15) e as mesmas 5 bandas; `CO = N + 2×bandas` não depende das arestas (limitação registrada no Achado 6 de `analise-critica.md`).
- **ESC maior no CE2** — menor proporção de conexões diretas entre nós da mesma banda (5 contra 7 pares ligados).
- **INT maior no CE2** — cinco arestas ligadas a integrações externas (contra quatro).
- **UX maior no CE2** — o canal de notificação TP4 recebe duas interações de entrada (confirmação e lembrete).
- **DEP maior no CE2** — os fluxos principais atravessam em média 4 bandas, contra 3 no CE1.
- **FLEX e AUT passam a diferenciar os serviços** — o sistema de pedidos tem FLEX superior (3,50 contra 2,76), associado a comunidades maiores, que concentram mais interações internamente; o de agendamento tem AUT superior (2,50 contra 1,67), porque duas de suas quatro comunidades dependem de no máximo uma interação de saída, contra uma de três no sistema de pedidos. Na versão inicial ambos saturavam em 5,00 nos dois casos, o que confirmou tratar-se de limitação do método e não de um domínio.
- **Margens diferentes:** o Monólito Modular é o mais próximo nos dois casos, com margem de 0,553 no CE1 e de apenas 0,239 no CE2. Isso é coerente com as características: o CE2 tem mais integrações externas, mais comunicação assíncrona e fluxos que atravessam mais bandas. O método indica o perfil mais próximo **e** o grau de segurança dessa indicação.
- **Pontos críticos internos:** P1 e DS no CE1; P2 e DS no CE2 — o repositório de dados `DS` é o único elemento que se repete como ponto crítico nos dois domínios.
- Os resultados demonstram a aplicação do método e apontam pontos de evolução; **não** validam as assinaturas nem determinam uma arquitetura. A avaliação com especialistas (seção 5.4) verificará a coerência das indicações e permitirá revisar as assinaturas.

---

## Histórico: versão inicial do método (cluster por conectividade fraca)

O TCC revisado registra os resultados das duas versões (DSR, seção 3.2). Valores da **versão inicial**, mantidos só como referência histórica — **não são mais o golden value**:

| | CE1 | CE2 |
|---|---|---|
| `cluster(v)` | 1 componente com os 15 nós | 1 componente com os 15 nós |
| FLEX / AUT (norm.) | 5,00 / 5,00 | 5,00 / 5,00 |
| Distâncias (assinaturas do Quadro 7) | Monólito 1,867, Microsserviços 1,368 → Microsserviços | Monólito 1,915, Microsserviços 1,176 → Microsserviços |
| FLEX+AUT na soma de Monólito | 81,0% | 77,1% |
| Sem FLEX/AUT (8 eixos, p = 1/8) | Monólito 0,909 × Micro 1,478 (inversão) | Monólito 1,025 × Micro 1,254 (inversão) |

Notas históricas: (1) na Entrega 3 o CE2 listava Microsserviços = 1,221, calculado com a assinatura implícita de VOL = 4,2 (a mesma que o projeto havia reconstruído por engenharia reversa); o Quadro 8 publica VOL = 2,0 e o "inicial" do TCC revisado passa a ser 1,176. (2) O Quadro 20 da Entrega 3 listava `1,859` para a distância do CE1 ao Monólito, divergente do 1,867 do Quadro 11 daquela versão (a soma do quadro dá 1,867); o TCC revisado não traz mais essa divergência (Quadro 39: 1,051 e 1,605). (3) O projeto havia adotado, antes do TCC revisado, derivação automática de fluxos (DEP 0,72/3,25 no CE1 e 0,68/3,00 no CE2) e propôs um limiar de peso de aresta para `cluster(v)`; ambas as decisões foram **superadas** pelo TCC revisado (fluxos como entrada; comunidades por modularidade).

---

## Histórico: o que mudou nos testes automatizados (Etapa 9, 2026-10-22)

Quando este documento foi reescrito (2026-10-21; atualizado em 2026-10-25 para o TCC que explora os empates do CNM), o código ainda seguia o TCC antigo e a tabela abaixo listava o que passaria a divergir. **A Etapa 9 aplicou essas mudanças**: os testes de backend e de front passaram a usar os valores deste documento como oráculo (fixture única de referência no backend), e o quadro fica como **registro histórico** do que foi trocado (IDs `DV-xx` = divergências código × TCC revisado, `docs/projeto/FLUXO_E_STATUS.md` seção 5). Os testes de ponta a ponta no navegador (Playwright) são atualizados em seguida.

| O que testava | Valor antes da Etapa 9 | Valor do TCC revisado (hoje) | DV |
|---|---|---|---|
| FLEX e AUT (calculadoras, métricas e pipeline) | FLEX = 1,00 e AUT = 1,00 (norm. 5,00) nos dois casos | CE1: FLEX 0,700 / AUT 0,3333 (3,50 / 1,67) · CE2: FLEX 0,5521 / AUT 0,50 (2,76 / 2,50) | DV-01 |
| Agrupamento (detector) | componentes conexos (1 cluster) | CNM explorando os empates e adotando a divisão de maior Q: 3 comunidades, Q = 0,4117 (CE1) e 4, Q = 0,3440 (CE2); casos-limite (m = 0, comunidade única, empates) | DV-01 |
| DEP (calculadora, métricas e pipeline) | derivado automaticamente: CE1 0,72 / 3,25; CE2 0,68 / 3,00 | com fluxos de entrada: CE1 0,60 / 2,50; CE2 0,80 / 3,75 | DV-02 |
| Seed de Perfis | "(reconstruído)"; Microsserviços VOL = 4,2 | "(provisório)" (Quadro 7); Microsserviços VOL = 2,0 | DV-03 |
| Distâncias (comparador e pipeline) | CE1 1,862 / 1,306; CE2 1,900 / 1,258 (Microsserviços mais próximo) | CE1 **1,051 / 1,605**; CE2 **1,107 / 1,346** (Monólito Modular mais próximo nos dois) | DV-01/02/03 |
| E2E do front (fluxo da Clínica) | FLEX 5,00, AUT 5,00, DEP 3,00; Microsserviços 1,258 × Monólito 1,900 | FLEX 2,76, AUT 2,50, DEP 3,75; Monólito 1,107 × Microsserviços 1,346 | DV-01/02/03/04, DV-10 |
| Comentários que citam Quadros | Q5, Q7–Q11, Q14–Q18 (numeração antiga) | Q7, Q20–Q28, Q31–Q38 (ver tabela de renumeração) | DV-10 |

Permaneceram válidos, sem mudança: CO, ESC, INT, UX, DISP, TR e VOL de ambos os casos; cargas, médias/desvios por banda e pontos críticos; e a normalização (Quadro 6).

**Atualização de 2026-10-25 (D9 resolvida):** o TCC passou a explorar todos os empates do CNM e a adotar a divisão de maior Q. Os valores de CE1 (3 comunidades, Q = 0,4117, FLEX 3,50, AUT 1,67, distâncias 1,051 / 1,605, margem 0,553) e de CE2 (FLEX 2,76, distâncias 1,107 / 1,346, margem 0,239) deste documento são os novos; os da tabela acima (coluna "hoje") descrevem o estado intermediário da Etapa 9, com o desempate por ordem de cadastro, que **não vale mais**.

---

## Casos de teste do TCC (Quadro 14) que usam estes dados

| ID | Caso de teste | Entrada | Resultado esperado | Requisitos |
|---|---|---|---|---|
| CT01 | Pontos potencialmente críticos | Mapa do CE1 | P1 e DS | RF04, RN05 |
| CT02 | Detecção de comunidades | Mapa do CE2 | 4 comunidades, Q ≈ 0,34 | RF06 |
| CT03 | Métricas e normalização | Mapas dos CE1, CE2 e CE3 | Valores dos Quadros 25, 33 e 42 | RF07, RF08 |
| CT04 | Distâncias | Perfis dos CE1, CE2 e CE3 | Valores dos Quadros 27, 35 e 45 | RF10 |
| CT09 | Fluxo incompleto | Fluxo com etapa sem aresta correspondente | Fluxo sinalizado como inválido | RF05 |
| CT11 | Nó sem ligação | Dois nós isolados adicionados ao CE1 | Um aviso por nó, com o nome do nó | RN10 |
| CT12 | Pesos renormalizados | Mapa sem fluxos principais e sem nós de Frontstage | Eixos omitidos da distância; pesos dos demais somam 1; distância ≤ 5 | RF10, RN07, RN08 |

Os demais (CT05–CT08, CT10) não dependem destes casos: intensidade inválida, aresta sem justificativa, valor-limite da normalização, métrica não aplicável (mapa sem Frontstage) e pesos que não somam 1. Técnica: caixa-preta, com classes de equivalência e análise do valor-limite; o oráculo são os valores calculados nos capítulos 6 e 7.

---

## Equivalência de numeração dos Quadros (Entrega 3 → `TCC_II.pdf` → TCC atual)

A numeração mudou a cada versão do TCC (a de 2026-11-06 removeu o Quadro "Resumo da metodologia", o cronograma e a seção 3.3, e levou as tabelas completas do CE2 e do CE3 para o Apêndice C). Citações a Quadros (em código, testes e documentos) devem usar a **última coluna**. Seções do CE2: o TCC atual reuniu as antigas 7.2–7.13 em 7.1–7.8 (ver o mapa abaixo).

| Conteúdo | Entrega 3 | `TCC_II.pdf` | TCC atual |
|---|---|---|---|
| Taxonomia das arestas / dos nós | Q2 / Q3 | Q4 / Q5 | Q3 / Q4 |
| Eixos e métricas observadas | Q4 | Q6 | Q5 |
| Limites de normalização | Q5 | Q7 | Q6 |
| Assinaturas de referência | — | Q8 | Q7 |
| Fluxo operacional do SBAR | Q6 | Q9 | Q8 |
| Estudo de Caso 1: Service Blueprint · nós · arestas · pontos críticos | Q6 · Q7 · Q8 · Q9 | Q19 · Q20 · Q22 · Q23 | Q19 · Q20 · Q22 · Q23 |
| Estudo de Caso 1: isolamento · métricas · contribuições · distâncias · matriz | — · Q10 · — · Q11 · Q12 | Q24 · Q25 · Q26 · Q27 · Q28 | Q24 · Q25 · Q26 · Q27 · Q28 |
| Estudo de Caso 2: Service Blueprint · nós · arestas · pontos críticos | Q13 · Q14 · Q15 · Q16 | Q30 · Q31 · Q32 · Q33 | Q30 · **Q57** · **Q58** · Q31 (nós e arestas no Apêndice C.1/C.2) |
| Estudo de Caso 2: isolamento · métricas · contribuições · distâncias · matriz | — · Q17 · — · Q18 · Q19 | Q34 · Q35 · Q36 · Q37 · Q38 | Q32 · Q33 · Q34 · Q35 · Q36 (matriz completa: Q59) |
| Comparação entre os estudos de caso | Q20 | Q39 (casos 1 e 2) | Q48 (casos 1, 2 e 3) |
| Critério de `w(e)` · RNFs/RNs do CE1 · RNFs/RNs do CE2 | — | Q21 · Q18 · Q29 | Q21 · Q18 · Q29 |
| Capítulo 5 | — | Q10 stakeholders · Q11 RFs · Q12 RNFs · Q13 RNs · Q14 histórias · Q15 casos de teste · Q16 instrumento · Q17 cronograma | Q9 stakeholders · Q10 RFs · Q11 RNFs · Q12 RNs · Q13 histórias · Q14 casos de teste **e resultado da execução** · Q15 tecnologias · Q16 suíte por camada · Q17 instrumento (cronograma removido) |
| Estudo de Caso 3 (cap. 8) | — | (`2026-11-05`: Q44 RNFs/RNs · Q45 Blueprint · Q46 nós · Q47 arestas · Q48 pontos críticos · Q49 comunidades · Q50 isolamento · Q51 métricas · Q52 contribuições · Q53 sensibilidade · Q54 distâncias · Q55 matriz) | Q37 RNFs/RNs · Q38 Blueprint · Q60 nós · Q61 arestas (Apêndice C.1/C.2) · Q39 pontos críticos · Q40 comunidades · Q41 isolamento · Q42 métricas · Q43 contribuições · Q44 sensibilidade · Q45 distâncias · Q46 matriz (completa: Q62) · Q47 cinco repositórios × um |
| Assinaturas provisórias × revisadas (9.7) | — | (`2026-11-05`: Q63 · Q64) | Q53 · Q54 |

Seções do Estudo de Caso 2 (`TCC_II.pdf` → atual): 7.1/7.2 → 7.1 · 7.3/7.4/7.6 → 7.2 · 7.5/7.7 → 7.3 · 7.8/7.9 → 7.4 (7.8.1 → 7.4) · 7.10 (7.10.1/7.10.2) → 7.5 · 7.11 → 7.6 · 7.12 → 7.7 · 7.13 → 7.8. Capítulos 6 e 8 mantêm a numeração.

---

## Estudo de Caso 3 — Plataforma de Marketplace

> Fonte: capítulo 8 do TCC (valores idênticos nas versões de 2026-11-05 e 2026-11-06; Quadros 37 a 47, mais 60 e 61 do Apêndice C; fluxos principais da seção 8.7; assinaturas revisadas e Quadros 53 e 54 da seção 9.7). **Cenário construído pelos autores** como teste de sanidade da capacidade de discriminar estilos (seção 8.1) — não é um sistema real nem evidência de validação independente. Os valores foram conferidos contra o backend (`Sbar.Application`, parâmetros dos Quadros 6 e 7) e contra uma implementação independente em Python, com diferença máxima de 4e-16. Teste automatizado: `backend/src/Sbar.Application.Tests/CasosDeUso/ServicoDeMetricasEstudoDeCaso3Tests.cs` (fixture `EstudosDeCasoDeReferencia.CriarEstudoDeCaso3`, CT03/CT04).

Domínio: marketplace com vendedores (publicação de anúncios), clientes (busca, carrinho, checkout, pagamento, entrega e acompanhamento), repasse ao vendedor, notificações e nota fiscal. Cinco repositórios de dados (DS1 a DS5), um por contexto de negócio, e sete sistemas externos.

### Nós (33) — Quadro 60 do TCC

| Id | Nó | Tipo | Banda |
|---|---|---|---|
| A1 | Cliente | Ator/Cliente | Ator |
| A2 | Vendedor | Ator/Cliente | Ator |
| TP1 | Busca e vitrine de produtos | Touchpoint | Frontstage |
| TP2 | Carrinho e checkout | Touchpoint | Frontstage |
| TP3 | Acompanhamento do pedido | Touchpoint | Frontstage |
| TP4 | Painel de anúncios | Touchpoint | Frontstage |
| TP5 | Canal de notificações | Touchpoint | Frontstage |
| TP6 | Painel de pedidos e repasses | Touchpoint | Frontstage |
| P1 | Publicação de anúncios | Processo | Backstage |
| P2 | Busca e indexação | Processo | Backstage |
| P3 | Reserva de estoque | Processo | Backstage |
| P4 | Gestão do pedido | Processo | Backstage |
| P5 | Autorização de pagamento | Processo | Backstage |
| P6 | Análise antifraude | Processo | Backstage |
| P7 | Separação e despacho | Processo | Backstage |
| P8 | Cotação de frete e etiquetas | Processo | Backstage |
| P9 | Atualização de rastreio | Processo | Backstage |
| P10 | Repasse ao vendedor | Processo | Backstage |
| P11 | Emissão de nota fiscal | Processo | Backstage |
| S1 | Identidade e autenticação | Suporte | Suporte |
| S2 | Serviço de notificações | Suporte | Suporte |
| DS1 | Catálogo de produtos | Dados | Suporte |
| DS2 | Estoque | Dados | Suporte |
| DS3 | Pedidos | Dados | Suporte |
| DS4 | Pagamentos e repasses | Dados | Suporte |
| DS5 | Entregas e rastreio | Dados | Suporte |
| E1 | Gateway de pagamento | Externo | Externo |
| E2 | Serviço antifraude | Externo | Externo |
| E3 | Transportadoras | Externo | Externo |
| E4 | Provedor de mensagens | Externo | Externo |
| E5 | ERP do vendedor | Externo | Externo |
| E6 | Emissor de nota fiscal | Externo | Externo |
| E7 | Moderação de anúncios | Externo | Externo |

### Arestas (49) — Quadro 61 do TCC

Soma das intensidades `Σ w(e)` = 108. Todas as arestas têm justificativa no TCC (RN03); os testes usam o texto padrão `Interação X → Y (TCC revisado, Quadro 61)`.

| # | Origem → Destino | Tipo | w |
|---|---|---|---|
| 1 | A2 → TP4 | SYN | 2 |
| 2 | TP4 → S1 | SYN | 2 |
| 3 | TP4 → P1 | SYN | 3 |
| 4 | P1 → DS1 | SYN | 3 |
| 5 | E5 → P1 | BAT | 1 |
| 6 | P1 → P2 | ASY | 2 |
| 7 | E5 → P3 | BAT | 1 |
| 8 | A1 → TP1 | SYN | 3 |
| 9 | TP1 → S1 | SYN | 2 |
| 10 | TP1 → P2 | SYN | 3 |
| 11 | P2 → DS1 | SYN | 3 |
| 12 | TP1 → TP2 | SYN | 3 |
| 13 | TP2 → P3 | CRI | 3 |
| 14 | P3 → DS2 | SYN | 3 |
| 15 | TP2 → P8 | SYN | 2 |
| 16 | P8 → E3 | SYN | 2 |
| 17 | TP2 → P4 | SYN | 3 |
| 18 | P4 → DS3 | SYN | 3 |
| 19 | P4 → P5 | CRI | 3 |
| 20 | P5 → P6 | CRI | 3 |
| 21 | P6 → E2 | SYN | 3 |
| 22 | P5 → E1 | SYN | 3 |
| 23 | E1 → P5 | SYN | 2 |
| 24 | P5 → DS4 | SYN | 2 |
| 25 | P5 → P4 | ASY | 2 |
| 26 | P4 → P7 | ASY | 3 |
| 27 | TP6 → P7 | SYN | 2 |
| 28 | P7 → P8 | SYN | 3 |
| 29 | P8 → DS5 | SYN | 2 |
| 30 | E3 → P9 | ASY | 2 |
| 31 | P9 → DS5 | SYN | 2 |
| 32 | P9 → S2 | ASY | 2 |
| 33 | S2 → E4 | ASY | 2 |
| 34 | E4 → TP5 | ASY | 2 |
| 35 | TP5 → A1 | ASY | 2 |
| 36 | A1 → TP3 | SYN | 2 |
| 37 | TP3 → DS5 | SYN | 2 |
| 38 | P4 → S2 | ASY | 2 |
| 39 | P9 → P10 | BAT | 1 |
| 40 | P10 → DS4 | BAT | 1 |
| 41 | P10 → E1 | BAT | 1 |
| 42 | P10 → TP6 | ASY | 1 |
| 43 | A2 → TP6 | SYN | 2 |
| 44 | P4 → P11 | ASY | 2 |
| 45 | P11 → E6 | SYN | 2 |
| 46 | E6 → P11 | ASY | 2 |
| 47 | P11 → DS3 | SYN | 2 |
| 48 | P1 → E7 | ASY | 2 |
| 49 | E7 → P1 | ASY | 2 |

### Carga por nó e identificação de gargalo (ponto potencialmente crítico) — Quadro 39 do TCC

Desvio padrão **populacional**; gargalo se `carga(v) > μ + σ` da própria banda. Bandas: Ator μ = 5,50 σ = 1,50; Frontstage μ = 7,00 σ = 3,00; Backstage μ = 9,36 σ = 4,01; Suporte μ = 4,71 σ = 1,28; Externo μ = 3,86 σ = 1,12.

| Nó | Banda | Carga | Gargalo |
|---|---|---|---|
| A1 | Ator | 7 | Não |
| A2 | Ator | 4 | Não |
| TP1 | Frontstage | 11 | **Sim** |
| TP2 | Frontstage | 11 | **Sim** |
| TP3 | Frontstage | 4 | Não |
| TP4 | Frontstage | 7 | Não |
| TP5 | Frontstage | 4 | Não |
| TP6 | Frontstage | 5 | Não |
| P1 | Backstage | 13 | Não |
| P2 | Backstage | 8 | Não |
| P3 | Backstage | 7 | Não |
| P4 | Backstage | 18 | **Sim** |
| P5 | Backstage | 15 | **Sim** |
| P6 | Backstage | 6 | Não |
| P7 | Backstage | 8 | Não |
| P8 | Backstage | 9 | Não |
| P9 | Backstage | 7 | Não |
| P10 | Backstage | 4 | Não |
| P11 | Backstage | 8 | Não |
| S1 | Suporte | 4 | Não |
| S2 | Suporte | 6 | **Sim** |
| DS1 | Suporte | 6 | **Sim** |
| DS2 | Suporte | 3 | Não |
| DS3 | Suporte | 5 | Não |
| DS4 | Suporte | 3 | Não |
| DS5 | Suporte | 6 | **Sim** |
| E1 | Externo | 6 | **Sim** |
| E2 | Externo | 3 | Não |
| E3 | Externo | 4 | Não |
| E4 | Externo | 4 | Não |
| E5 | Externo | 2 | Não |
| E6 | Externo | 4 | Não |
| E7 | Externo | 4 | Não |

Pontos potencialmente críticos: **TP1, TP2, P4, P5, S2, DS1, DS5, E1** (oito; só P4, P5, S2, DS1 e DS5 entram em DISP, que considera Backstage e Suporte).

### Fluxos principais F (entrada do método) — seção 8.7

| Fluxo | Caminho | Bandas percorridas |
|---|---|---|
| F1 – Publicação de anúncio | A2 → TP4 → P1 → DS1 | 4 |
| F2 – Busca de produtos | A1 → TP1 → P2 → DS1 | 4 |
| F3 – Reserva de estoque | TP2 → P3 → DS2 | 3 |
| F4 – Compra e pagamento | A1 → TP1 → TP2 → P4 → P5 → E1 → P5 → DS4 | 5 |
| F5 – Despacho e notificação | P4 → P7 → P8 → E3 → P9 → S2 → E4 → TP5 → A1 | 5 |
| F6 – Acompanhamento | A1 → TP3 → DS5 | 3 |
| F7 – Repasse ao vendedor | P9 → P10 → E1 | 2 |

DEP bruto = (4+4+3+5+5+3+2)/7/5 = 26/35 = 0,7429. O fluxo F4 visita P5 duas vezes (retorno do gateway antes do registro).

### Comunidades estruturais e modularidade (seções 4.4.1; 8.9) — Quadro 40 do TCC

`Q` = **0,5266**, cinco comunidades, divisão final única (sem empate entre divisões). Numeração pela menor posição de cadastro.

| Comunidade | Nós | Saídas externas |
|---|---|---|
| C1 – catálogo, busca e moderação | A1, DS1, E7, P1, P2, S1, TP1, TP4 | 2 |
| C2 – entrega, rastreio e pedidos do vendedor | A2, DS5, E3, P7, P8, P9, TP3, TP6 | 3 |
| C3 – checkout e estoque | DS2, E5, P3, TP2 | 3 |
| C4 – notificações | E4, S2, TP5 | 1 |
| C5 – pedido, pagamento, repasse e nota fiscal | DS3, DS4, E1, E2, E6, P10, P11, P4, P5, P6 | 3 |

Apenas C4 tem no máximo uma saída externa (TP5 → A1): AUT = 1/5 = 0,2000.

### Resultado esperado das dez métricas — Quadro 42 do TCC

| Eixo | Métrica bruta | Mín. | Máx. | Normalizado (0–5) |
|---|---|---|---|---|
| CO | 43 | 6 | 40 | 5,00 (limitado ao máximo, RN06) |
| ESC | 0,9292 | 0 | 1 | 4,65 |
| INT | 0,2857 | 0 | 1 | 1,43 |
| UX | 1,1667 | 0 | 3 | 1,94 |
| DISP | 0,7222 | 0 | 1 | 3,61 |
| FLEX | 0,8419 | 0 | 1 | 4,21 |
| TR | 0,0833 | 0 | 1 | 0,42 |
| AUT | 0,2000 | 0 | 1 | 1,00 |
| VOL | 2,2041 | 1 | 3 | 3,01 |
| DEP | 0,7429 | 0,2 | 1 | 3,39 |

### Distâncias até os perfis de referência — Quadros 43 e 45 do TCC e Quadro 54 (seção 9.7)

Pesos `p_i = 0,1`; os dez eixos são aplicáveis (sem renormalização, DV-11).

| Assinaturas | Monólito Modular | Microsserviços | Margem | Perfil mais próximo |
|---|---|---|---|---|
| Provisórias (Quadro 7) | 1,686 (33,7% do máximo) | 1,555 (31,1% do máximo) | 0,131 | **Microsserviços** |
| Revisadas pelos especialistas (TCC 9.7) | 1,540 | 1,628 | 0,088 | **Monólito Modular** |

Contribuições por eixo com as assinaturas provisórias (Quadro 43): Monólito Modular — CO 0,9000, ESC 0,9897, FLEX 0,7342; Microsserviços — INT 0,4291, TR 0,4340, AUT 1,2250. A soma das contribuições é a distância ao quadrado.

Assinaturas revisadas (TCC 9.7): iguais às provisórias, exceto Monólito Modular ESC 2,0, FLEX 2,0 e DEP 2,5, e Microsserviços INT 4,0.

### Observações

- Cenário **construído pelos autores**, já conhecendo as métricas, como serviço em que Microsserviços é plausível; uma modelagem anterior do mesmo domínio, com mais acoplamento entre contextos, resultou em Monólito Modular com margem mínima (1,548 contra 1,630). Não serve como validação independente (seção 8.1).
- Margem pequena (0,131) e indicação sensível: com os cinco repositórios consolidados em um único DS o perfil mais próximo passa a ser o Monólito Modular, e com as assinaturas revisadas também (TCC 8.12 e 9.7).
- CO satura em 5,00 a partir de cerca de 30 nós (limite máximo 40, Quadro 6).
