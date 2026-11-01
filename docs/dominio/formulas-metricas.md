# Fórmulas das Métricas Arquiteturais (os Dez Eixos)

> Fonte: o TCC II revisado (TCC revisado, 2026-10-21), seções 4.4 a 4.6 e Quadros 6, 7 e 8. Estas são as fórmulas **exatas** a implementar — cada uma deve virar uma classe/função pura testável isoladamente (POP-06), com a fórmula citada em comentário no código (POP-04).
>
> **O que mudou em relação ao TCC anterior (`TCC_II_Entrega_3.pdf`)**: (1) `cluster(v)` deixou de ser o componente conexo do grafo e passou a ser a **comunidade estrutural** obtida por **detecção de comunidades por modularidade** (seção 4.4.1) — isso afeta FLEX e AUT; (2) os fluxos principais `F` são **entrada** (cadastrados e validados, RF05), não derivados automaticamente; (3) os perfis arquiteturais de referência foram **publicados** (Quadro 8). As fórmulas dos outros eixos não mudaram. Os valores esperados para cada estudo de caso estão em [`estudos-de-caso.md`](./estudos-de-caso.md).
>
> **Estado do código:** o back-end implementa o método **como este documento o descreve** — comunidades por modularidade (`DetectorDeComunidades`), fluxos como entrada e assinaturas do Quadro 8. O desempate do CNM (D9) segue a seção 4.4.1 do TCC.

## Notação geral

| Símbolo | Significado |
|---|---|
| `N = |V|` | Número total de nós |
| `M = |E|` | Número total de arestas |
| `V_b` | Nós pertencentes à banda `b`; `n_b = |V_b|` |
| `E_b` | Arestas com pelo menos uma extremidade na banda `b` |
| `grau⁺(v)`, `grau⁻(v)` | Graus de saída e de entrada do nó `v` |
| `grau_total(v)` | `grau⁺(v) + grau⁻(v)` — **não é definido explicitamente no TCC**, mas é a única leitura consistente com o uso em FLEX (ver Eixo 6); tratamos como convenção fixada aqui. Arestas paralelas contam uma a uma. |
| `w(e)` | Peso de intensidade da aresta `e`, `∈ {1,2,3}` |
| `carga(v)` | `Σ w(e)` para todas as arestas `e` incidentes a `v` |
| `cluster(v)` | **Comunidade estrutural** à qual o nó `v` pertence, obtida por detecção de comunidades por modularidade (seção "Detecção de comunidades" abaixo; TCC 4.4.1). O TCC usa `cluster(v)`, "agrupamento" e "comunidade" como sinônimos; neste projeto o termo canônico é **Comunidade**. |
| `F = {f1, ..., fk}` | Conjunto de fluxos principais validados no Mapa. **É entrada do usuário** (RF05; seção "Fluxos principais" abaixo). |

## Detecção de comunidades (TCC 4.4.1)

### Por que existe

Na versão inicial do método, `cluster(v)` era o componente conexo do grafo. Como todo o Mapa acaba formando um único componente, `FLEX` e `AUT` valiam sempre o máximo para qualquer estrutura (o que tornava essas métricas constantes). A solução adotada pelo TCC revisado é agrupar os nós por **comunidades**: grupos de nós mais densamente conectados entre si do que com o restante do grafo.

### Definição formal

A qualidade de uma divisão do grafo em comunidades é medida pela **modularidade** `Q` (Clauset, Newman e Moore, 2004), calculada sobre a versão **não direcionada** do Mapa:

```
Q = (1 / 2m) · Σ_{i,j} [ A_ij − (k_i · k_j) / 2m ] · δ(c_i, c_j)
```

- `A_ij` = peso da ligação entre os nós `i` e `j`: a **soma das `w(e)`** de todas as arestas existentes entre eles, **em qualquer direção** (arestas paralelas e antiparalelas são somadas; self-loop é proibido — D6).
- `k_i` = soma dos pesos das ligações do nó `i` (`k_i = Σ_j A_ij`; "força" do nó, não o grau).
- `m` = soma de todos os pesos do grafo (cada aresta contada uma vez; é a soma de `w(e)` de todas as arestas — 47 no Estudo de Caso 1, 45 no Estudo de Caso 2).
- `δ(c_i, c_j) = 1` se `i` e `j` estão na mesma comunidade e `0` caso contrário.

Forma equivalente, por comunidade `c` (útil para implementar e conferir): `Q = Σ_c [ L_c / m − (K_c / 2m)² ]`, com `L_c` = soma dos pesos das ligações internas a `c` (cada aresta uma vez) e `K_c = Σ_{i∈c} k_i`.

Segundo os autores, `Q` acima de aproximadamente **0,3** indica estrutura de comunidades significativa. **O valor de `Q` obtido deve ser registrado junto aos resultados de cada aplicação** (RF06), assim como a partição.

### Algoritmo (guloso de Clauset-Newman-Moore) — especificação implementável

Entrada: nós `V` e arestas `E` do Mapa. Saída: a partição de `V` em comunidades e o `Q` final.

1. **Construir o grafo não direcionado ponderado**: para cada par `{u, v}` com `u ≠ v`, `A_uv = Σ w(e)` sobre as arestas entre `u` e `v` (qualquer direção). Calcular `k_i` de cada nó e `m = Σ w(e)`.
2. **Inicializar**: cada nó é uma comunidade. Para cada comunidade `c`: `a_c = K_c / 2m`; para cada par de comunidades `c ≠ d` ligadas por pelo menos uma aresta: `e_cd = A_cd / 2m` (soma dos `A` entre os nós das duas comunidades).
3. **Passo**: para cada par `(c, d)` ligado por aresta, calcular o ganho de modularidade da fusão:
   ```
   ΔQ(c, d) = 2 · ( e_cd − a_c · a_d )
   ```
   Escolher o par com o **maior** `ΔQ`.
4. **Parada**: se o maior `ΔQ` for `≤ 0`, terminar. Caso contrário, fundir as duas comunidades (`a_novo = a_c + a_d`; `e_(c∪d),x = e_cx + e_dx` para toda outra comunidade `x`) e voltar ao passo 3.
5. **Saída**: partição final; `Q = Σ_c (e_cc − a_c²)` (equivale à fórmula acima).

Só é preciso testar pares **ligados por aresta**: a fusão de duas comunidades sem ligação tem `ΔQ = −2·a_c·a_d < 0` e nunca é escolhida. Não há parâmetro de resolução nem número de comunidades pré-definido — o número de comunidades é consequência do algoritmo.

**Empates (TCC de 2026-10-25, seção 4.4.1)**: a descrição original do algoritmo não define como escolher entre fusões de mesmo `ΔQ`. O procedimento do TCC é **explorar, a cada passo, todas as fusões empatadas e, entre as divisões finais assim obtidas, adotar a de maior `Q`** — o resultado não depende da ordem dos nós nem dos rótulos. Nos dois estudos de caso há duas divisões finais: Estudo de Caso 1 → 3 comunidades, `Q = 0,4117` (adotada) ou 4 comunidades, `Q = 0,4004`; Estudo de Caso 2 → 4 comunidades, `Q = 0,3440` (adotada) ou `Q = 0,3417` (ver `estudos-de-caso.md`). O guloso também não garante a divisão de maior modularidade possível (o TCC reprocessou os casos com Louvain, 30 sementes: `Q` de até 0,412 e 0,364, mesmo estilo mais próximo); por isso se registra o algoritmo e o `Q` em cada aplicação.

### Casos-limite (D9)

| Situação | O que acontece | Convenção proposta |
|---|---|---|
| **Empate de `ΔQ`** entre pares candidatos | Nos dois estudos de caso há empates exatos, e desempates diferentes geram divisões finais diferentes (CE1: 3 comunidades, `Q = 0,4117` **ou** 4 comunidades, `Q = 0,4004`; CE2: `Q = 0,3440` **ou** `Q = 0,3417`). | **Resolvido pelo TCC**: explorar todas as fusões empatadas e adotar a divisão final de maior `Q` (independe da ordem dos nós). O protótipo memoriza as partições já exploradas (cada uma é visitada uma vez) e compara o `Q` das divisões finais em **inteiros exatos** (`Σ_c 4m·W_c − K_c²`). Se duas divisões finais distintas tiverem o **mesmo** `Q` (o TCC não prevê), adota-se a primeira na ordem de cadastro e o resultado avisa. Há um limite de 20.000 estados explorados, acima do qual segue-se só o primeiro empate de cada passo e o resultado avisa. Sempre registrar a partição usada. |
| **Grafo sem arestas** (`m = 0`) | `Q` é indefinido (divisão por `2m = 0`). Cada nó fica isolado numa comunidade; todos os nós internos têm grau 0 (isolamento 1) e todas as comunidades têm 0 saídas → FLEX = AUT = 1. | Reportar `Q` como **não aplicável** (não como 0). INT, TR e VOL já são "não aplicáveis" com `M = 0`. |
| **Comunidade única** | Pode acontecer em grafos muito centralizados. Exemplo: 7 nós em que 6 se ligam apenas a um repositório `DS` (estrela, `w = 3` em todas as arestas) → o CNM funde tudo, `Q = 0`; FLEX = AUT = 1 (a saturação original reaparece nesse caso). | Informar o `Q` e deixar visível que há uma comunidade só; não é erro, é uma propriedade do Mapa. |
| **Nó sem arestas** (com `m > 0`) | Fica numa comunidade só dele (0 saídas → conta como "autônoma" em AUT). Um Mapa validado (passo 7) não deve ter nós soltos. | Tratar como aviso de validação. |
| **Limite de resolução** | A modularidade pode preferir fundir comunidades pequenas e densas: num anel de 10 triângulos ligados por uma aresta (30 nós, dentro do limite de 50 do RNF05), o CNM devolve 5 comunidades de 6 nós (`Q = 0,675`) em vez das 10 "naturais" (`Q = 0,650`). | Documentar como limitação do método; não há parâmetro de ajuste. |

## Os dez eixos

### Eixo 1 — Complexidade Operacional (CO)

```
CO = N + 2 × |bandas ocupadas|
```
`|bandas ocupadas|` = número de bandas (das 5) que têm pelo menos um nó.

### Eixo 2 — Escalabilidade (ESC)

```
ESC = Σ_b [C(n_b, 2) − arestas_intra(b)] / Σ_b C(n_b, 2)
```
`C(n_b, 2)` = combinação de pares possíveis dentro da banda `b` = `n_b × (n_b − 1) / 2`. `arestas_intra(b)` = conexões diretas entre nós da mesma banda `b` (sem distinção de direção).

O somatório `Σ_b` percorre **todas as 5 bandas** (Ator, Frontstage, Backstage, Suporte, Externo) — inclusive as que normalmente têm poucos nós (Ator, Externo). O próprio TCC não deixa isso explícito no texto da fórmula, mas foi **confirmado por conferência aritmética** com os números do Estudo de Caso 1: somando `C(n_b,2)` das 5 bandas (0+6+10+3+1) dá exatamente o denominador 20 publicado (seção 6.8.1: "20 pares possíveis e 7 pares com interação direta"). Se a soma rodasse só sobre bandas "internas" (Backstage+Suporte), o denominador daria 13, não 20 — não bateria.

`arestas_intra(b)` deve ser contado como **número de pares distintos de nós da banda com pelo menos uma aresta entre eles** (não o número bruto de arestas) — se um dia existirem duas arestas entre o mesmo par (ex.: A→B e B→A), isso não deve contar duas vezes. O TCC não cobre esse caso-limite explicitamente; é uma convenção nossa para a fórmula não dar resultado negativo.

**Caso especial (RN08)**: se o denominador for 0 (nenhuma banda tem ≥ 2 nós), `ESC = 1`, "pois não existem pares de nós disponíveis para caracterizar acoplamento interno". É a **única exceção** à regra "denominador zero ⇒ métrica não aplicável".

Representa a proporção de pares de nós **sem** conexão direta dentro da mesma banda — indicador estrutural de independência e potencial de paralelismo, não escalabilidade real de produção.

### Eixo 3 — Integração (INT)

```
INT = |E_Externo| / M
```
`E_Externo` = arestas relacionadas à banda de Integrações Externas (têm origem ou destino em nó da banda Externo).

**Caso especial**: se `M = 0`, métrica não aplicável.

### Eixo 4 — Experiência do Usuário (UX)

```
UX = Σ_{v ∈ V_Frontstage} grau⁻(v) / n_Frontstage
```
Grau de entrada médio dos nós Frontstage — característica estrutural das interações com o Frontstage, não medida direta da qualidade da experiência.

**Caso especial**: se `n_Frontstage = 0`, métrica não aplicável (CT08).

### Eixo 5 — Disponibilidade (DISP)

```
DISP = 1 − [ Σ_{v ∈ V_Backstage ∪ V_Suporte} gargalo(v) ] / (n_Backstage + n_Suporte)
```
Complemento da proporção de nós internos (Backstage + Suporte) marcados como gargalo (ponto potencialmente crítico: `carga(v) > μ_b + σ_b`, RN05). Um nó crítico fora dessas bandas **não entra**: no Estudo de Caso 2, TP2 (Frontstage) é crítico mas não afeta DISP (seção 7.8.1).

### Eixo 6 — Flexibilidade / Evolução (FLEX)

Para cada nó interno `v` (Backstage ∪ Suporte):
```
isolamento(v) = 1 − arestas_externas(v) / grau_total(v)
```
`arestas_externas(v)` = arestas que conectam `v` a nós pertencentes a **comunidades diferentes** da comunidade de `v` (`cluster(v)`).

**Caso especial**: se `grau_total(v) = 0`, `isolamento(v) = 1`.

```
FLEX = Σ_{v ∈ V_Backstage ∪ V_Suporte} isolamento(v) / (n_Backstage + n_Suporte)
```

Grau médio de isolamento estrutural dos elementos internos em relação a outras comunidades — indicador de potencial de evolução independente. As comunidades são calculadas sobre **todos** os nós do Mapa (inclusive Ator, Frontstage e Externo); só o somatório usa os nós internos.

### Eixo 7 — Tempo Real (TR)

```
TR = Σ_{e: tipo(e)=CRI} w(e) / Σ_{e ∈ E} w(e)
```
Proporção ponderada de comunicação classificada como crítica/tempo real. (O TCC não prevê caso especial para `M = 0`, em que o denominador é 0; a convenção do projeto é tratar como "não aplicável".)

### Eixo 8 — Autonomia dos Módulos (AUT)

Seja `C` o conjunto de comunidades estruturais (seção 4.4.1). Para cada `c ∈ C`, `saidas_externas(c)` = arestas que partem de nós de `c` para nós de outra comunidade (**só as de saída**, não as de entrada).

```
AUT = |{c ∈ C : saidas_externas(c) ≤ 1}| / |C|
```

**Caso especial**: se `C = ∅`, métrica não aplicável.

Proporção de comunidades que dependem de no máximo uma interação de saída para outras comunidades — indicador de potencial de autonomia dos módulos. Com `|C|` pequeno (4 nos dois estudos), `AUT` só assume valores em múltiplos de `1/|C|`.

### Eixo 9 — Volume de Comunicação (VOL)

```
VOL = Σ_{e ∈ E} w(e) / M
```
Intensidade média por aresta. (Sem caso especial no TCC para `M = 0`; convenção do projeto: não aplicável.)

### Eixo 10 — Dependência Organizacional (DEP)

Para cada fluxo principal `f ∈ F`, `bandas_percorridas(f)` = número de bandas distintas visitadas pelo caminho do fluxo (incluindo as bandas de origem e de destino).

```
DEP = [ Σ_{f ∈ F} bandas_percorridas(f) / |F| ] / 5
```
O `5` é o número total de bandas do SBAR.

**Caso especial**: se `F = ∅`, métrica não aplicável.

#### Fluxos principais (`F`) — entrada do usuário (RF05)

No TCC revisado os fluxos principais **não são derivados do grafo**: são definidos pelos pesquisadores (seções 4.2.7, 6.6 e 7.6) e o protótipo deve **cadastrá-los e validá-los** (RF05, US03, CT09). Isso **supera** a decisão anterior do projeto (D2: derivar `F` automaticamente por DFS até "sinks" — "Opção B").

Regras para o cadastro:
1. Um fluxo é uma **sequência ordenada de nós** do Mapa (ex.: CE1 F2: `A → TP1 → TP2 → P1 → P2 → P3 → P4 → TP4`).
2. **Validação (CT09)**: cada par de nós consecutivos precisa de uma **aresta dirigida** correspondente no grafo. Fluxo com etapa sem aresta é sinalizado como inválido, com indicação da etapa ausente. Conferido contra os 9 fluxos dos dois estudos de caso: todos passam.
3. O fluxo é um **passeio**, não um caminho simples: pode repetir nó. O CE1 F4 (`P3 → P5 → S2 → E1 → P5 → DS`) visita `P5` duas vezes (o retorno `E1 → P5` é a resposta do gateway, seção 6.6).
4. **Não é obrigatório começar no Ator** na prática: 4 dos 9 fluxos dos estudos começam em outro nó (CE1 F3 `P4 → E2 → A`, F4 `P3 → …`; CE2 F2 e F3 começam em `P2`), embora a seção 4.2.7 fale em caminho "desde o ator até o elemento terminal". Convenção proposta (D10): validar só a existência das arestas, sem exigir início no Ator.
5. `bandas_percorridas(f)` conta as bandas distintas dos nós do fluxo.
6. Sem nenhum fluxo cadastrado, `F = ∅` e DEP é "não aplicável".

Valores de referência: CE1 → 4 fluxos × 3 bandas → `DEP = 0,6` (norm. 2,50); CE2 → fluxos com 4, 5, 5, 3 e 3 bandas → média 4,0 → `DEP = 0,8` (norm. 3,75).

## Normalização (0 a 5)

```
Norm_i = 5 × (M_i − M_i_min) / (M_i_max − M_i_min)     — resultado limitado a [0, 5] (RN06)
```

A escala 0–5 expressa a **intensidade relativa** da característica no serviço, não "melhor" ou "pior" desempenho.

**Caso especial não coberto pelo TCC**: se `M_i_max == M_i_min` (configuração inválida de limites, divisão por zero), o comportamento correto não está definido na fonte. Convenção a adotar: tratar como erro de configuração explícito (lançar exceção ao salvar limites onde `max <= min`), nunca retornar um valor calculado silenciosamente.

**Precisão/arredondamento (RNF03)**: o TCC exibe os valores normalizados com **no mínimo duas casas** e as distâncias com **três**. Os quadros publicados calculam as contribuições por eixo (Quadros 26 e 36) e as distâncias com os **normalizados sem arredondar** (conferido: CE1, Monólito, CO → `0,1 × (2,794 − 2)² = 0,0630`, e não `0,0624` do valor arredondado 2,79). Convenção: calcular em `double`/`decimal` de precisão total, arredondar só na apresentação, e nos testes comparar com tolerância (ex.: `precisao: 3`), nunca por igualdade exata de ponto flutuante.

### Limites provisórios (Quadro 7 do TCC — ⚠️ ainda não calibrados/validados por especialistas)

| Eixo | Métrica bruta | Mínimo | Máximo |
|---|---|---|---|
| CO | N + 2·bandas ocupadas | 6 | 40 |
| ESC | Proporção de pares independentes | 0 | 1 |
| INT | Proporção de arestas externas | 0 | 1 |
| UX | Grau de entrada médio (Frontstage) | 0 | **3,0** (provisório, "sujeito à validação empírica"; adotado nas aplicações demonstrativas) |
| DISP | 1 − proporção de pontos potencialmente críticos | 0 | 1 |
| FLEX | Isolamento médio dos nós internos | 0 | 1 |
| TR | Proporção volumétrica crítica | 0 | 1 |
| AUT | Proporção de comunidades autônomas (o Quadro 7 ainda escreve "clusters") | 0 | 1 |
| VOL | Peso médio por aresta | 1 | 3 |
| DEP | Bandas médias por fluxo / 5 | 0,2 | 1 |

Estes limites **devem ficar configuráveis** (arquivo de configuração central, não hardcoded — decisão explícita do POP-03), pois já estão marcados como provisórios pelos próprios autores e sujeitos a recalibração. O TCC pede que eventuais alterações sejam **registradas** para preservar a rastreabilidade entre versões do método (RF11, D12).

## Pesos do vetor de comparação

Para a distância entre perfis, cada eixo tem um peso `p_i`, com `Σ p_i = 1` (RN07 — soma diferente de 1 bloqueia o cálculo, CT10). Configuração inicial (sem calibração por domínio): `p_i = 0,1` para todos os dez eixos — também deve ser configurável. Pesos diferenciados por domínio permanecem em aberto no TCC (seção 4.5.1). A representação visual do Radar usa os dez eixos com peso equivalente.

## Distância entre o perfil do serviço e uma assinatura de referência

```
d(Servico, Estilo) = √( Σ_i p_i × (Norm_i^Servico − Norm_i^Estilo)² )
```

**Como ler a distância (TCC 4.6)**:
- Como os eixos vão de 0 a 5 e `Σ p_i = 1`, a **distância máxima possível é 5**; qualquer distância pode ser lida como percentual desse máximo (CE1: 1,051 ≈ 21%, 1,605 ≈ 32%).
- Além do valor absoluto, importa a **diferença entre as distâncias** dos estilos comparados (a "margem"): diferença pequena indica perfil intermediário e que a comparação, isoladamente, pouco diferencia os estilos (CE1: 0,553 ≈ 11% da escala; CE2: 0,239 ≈ 4,8%).
- O estilo com menor distância é o **perfil de referência mais próximo** e constitui um **subsídio** à análise, não uma escolha: o resultado deve ser lido junto com contexto, requisitos, restrições organizacionais, custos e competências, que o Mapa não captura. O SBAR **não determina** uma arquitetura definitiva; "recommendation" no nome do método é uma indicação fundamentada e rastreável ao arquiteto (RN09: a tela deve apresentar o "perfil de referência mais próximo" com aviso de que não substitui a análise do arquiteto).
- Cada eixo contribui `p_i × (diferença)²` para a soma; a **contribuição por eixo** mostra de onde vem a distância (RF10; Quadros 26 e 36).

## Assinaturas arquiteturais de referência (Quadro 8 do TCC)

Cada estilo é um vetor de valores 0–5 para os dez eixos. O TCC revisado **publica** os valores de dois estilos — **assinaturas provisórias**, atribuídas pelos pesquisadores para o experimento, com base nas características usualmente associadas a cada estilo, **sem medição empírica**, e que ainda precisam ser validadas por especialistas (protocolo da seção 5.4; revisão pela mediana das respostas). No produto continuam sendo editáveis (tela "Perfis Arquiteturais"), sem hardcode.

| Eixo | Monólito Modular | Microsserviços | Justificativa (resumo do Quadro 8) |
|---|---|---|---|
| CO | 2,0 | 4,0 | Microsserviços envolvem mais unidades implantáveis, integrações e infraestrutura; o Monólito concentra a operação numa unidade. |
| ESC | 1,5 | 4,0 | Microsserviços pressupõem poucas ligações diretas entre elementos de uma mesma camada, permitindo escalar partes de forma independente; no Monólito os módulos compartilham o processo. |
| INT | 1,0 | 3,5 | Microsserviços se comunicam por interfaces de rede e integrações com mais frequência; o Monólito resolve a maior parte das interações internamente. |
| UX | 2,5 | 2,5 | A convergência de interações nos pontos de contato não foi considerada característica diferenciadora dos dois estilos. |
| DISP | 3,0 | 4,0 | O isolamento de falhas em serviços independentes tende a reduzir a concentração de pontos críticos; no Monólito a unidade de implantação é compartilhada. |
| FLEX | 1,5 | 4,0 | Microsserviços favorecem a evolução independente; no Monólito a evolução ocorre na mesma unidade de implantação. |
| TR | 1,5 | 2,5 | Fluxos de resposta imediata distribuídos entre serviços são mais associados a Microsserviços; o valor permanece moderado nos dois. |
| AUT | 1,0 | 4,5 | A autonomia dos módulos é central em Microsserviços; no Monólito os módulos compartilham processo e, em geral, o banco. |
| VOL | 3,0 | **2,0** | No Monólito, chamadas internas baratas admitem comunicação mais intensa; em Microsserviços a comunicação intensa pela rede tende a ser evitada. |
| DEP | 3,0 | 4,0 | Fluxos em Microsserviços tendem a atravessar mais fronteiras técnicas e organizacionais. |

