# Passo a Passo: Do Service Blueprint ao Perfil de Referência Mais Próximo

> Guia único, do início ao fim, unindo os 7 passos de "Blueprint → Mapa" (`metodo-sbar.md` seção 4) com os passos de "Mapa → Radar → perfil de referência" (`metodo-sbar.md` seção 5), conforme o **TCC revisado** (`docs/fontes/TCC.pdf`). Usa um **exemplo ilustrativo próprio** (não é um Estudo de Caso do TCC), pequeno o bastante para ser acompanhado à mão. Para os exemplos reais e completos (15 nós, 21 arestas), ver [`estudos-de-caso.md`](./estudos-de-caso.md). Todos os números abaixo foram calculados por script e conferidos; as regras (comunidades, fluxos como entrada, assinaturas do Quadro 7, saída como subsídio) são as do TCC revisado.

## O exemplo: cadastro em uma newsletter

Um cliente se inscreve numa newsletter por um formulário; o sistema valida o e-mail, salva o cadastro e dispara um e-mail de confirmação via um provedor externo.

### Passo 1 — Inventariar os elementos do Service Blueprint

| Jornada do cliente | Frontstage | Backstage | Suporte/Dados | Externo |
|---|---|---|---|---|
| Acessa o formulário | Formulário de inscrição (TP1) | | | |
| (aguarda) | | Validação de e-mail (P1) | | |
| (aguarda) | | | Repositório de assinantes (DS) | |
| (aguarda) | | | | Serviço de e-mail, ex. SendGrid (E1) |
| Recebe confirmação | E-mail de confirmação (TP2) | | | |

### Passo 2 — Posicionar os nós em bandas

| Nó | Tipo | Banda |
|---|---|---|
| A | Ator | Ator |
| TP1 | Touchpoint | Frontstage |
| TP2 | Touchpoint | Frontstage |
| P1 | Processo | Backstage |
| DS | Dados | Suporte |
| E1 | Externo | Externo |

Todas as 5 bandas ficam ocupadas (`N = 6`). A banda é sempre derivada do tipo (RN01: cada nó pertence a exatamente uma banda).

### Passos 3 a 5 — Traçar arestas, classificar e atribuir peso

Toda aresta precisa de uma **justificativa rastreável ao Blueprint** (RN03). Tipos pela natureza da comunicação (Quadro 3); se uma interação for assíncrona **e** tiver restrição de tempo real, prevalece CRI (RN04). Peso pelo critério do Quadro 21: 3 = ocorre em 100% das instâncias e no caminho obrigatório, 2 = recorrente de apoio, 1 = condicional ou periódica.

| Origem | Destino | Tipo | Peso `w(e)` | Justificativa (RN03) |
|---|---|---|---|---|
| A | TP1 | SYN | 3 | Cliente sempre acessa o formulário — obrigatório. |
| TP1 | P1 | SYN | 3 | Toda submissão aciona a validação — obrigatório. |
| P1 | DS | **CRI** | 3 | Perder o cadastro aqui seria grave (não dá para "tentar de novo" silenciosamente) — crítica. |
| P1 | E1 | ASY | 2 | Dispara o envio do e-mail, não bloqueia o cliente — assíncrona, recorrente. |
| E1 | TP2 | ASY | 1 | O e-mail chega com atraso variável — assíncrona, apoio. |
| TP2 | A | ASY | 1 | Cliente vê a confirmação quando ela chegar. |

`M = 6` arestas. Soma dos pesos = 3+3+3+2+1+1 = **13**.

### Passo 6 — Carga por nó e identificação de gargalo (ponto potencialmente crítico)

`carga(v)` = soma dos pesos de todas as arestas que tocam `v` (entrada + saída). Um nó é crítico quando `carga(v) > μ_banda + σ_banda` (desvio padrão populacional, RN05):

| Nó | Banda | Carga | μ da banda | σ da banda | `μ+σ` | Gargalo? |
|---|---|---|---|---|---|---|
| A | Ator | 3+1=4 | 4,00 | 0,00 | 4,00 | Não (n=1, nunca é) |
| TP1 | Frontstage | 3+3=6 | 4,00 | 2,00 | 6,00 | **Não** — 6 não é *maior* que 6 |
| TP2 | Frontstage | 1+1=2 | 4,00 | 2,00 | 6,00 | Não |
| P1 | Backstage | 3+3+2=8 | 8,00 | 0,00 | 8,00 | Não (n=1) |
| DS | Suporte | 3 | 3,00 | 0,00 | 3,00 | Não (n=1) |
| E1 | Externo | 2+1=3 | 3,00 | 0,00 | 3,00 | Não (n=1) |

Nenhum nó vira gargalo — e a banda Frontstage (TP1=6, TP2=2) é um exemplo ao vivo do **Achado 2** de `analise-critica.md` (continua válido no TCC revisado): mesmo com TP1 carregando **3× mais carga** que TP2, o critério não consegue sinalizar, porque a banda tem exatamente 2 nós (`μ+σ` é sempre igual ao maior valor).

### Passo 7 — Validar a malha e cadastrar os fluxos principais

No TCC revisado os **fluxos principais `F` são entrada** (RF05), escolhidos por quem modela o Blueprint — não são derivados do grafo. Para cada fluxo o sistema verifica se existe caminho dirigido no Mapa (CT09: fluxo com etapa sem aresta é sinalizado como inválido). Neste exemplo:

| Fluxo | Caminho (entrada) | Arestas conferidas | Bandas distintas |
|---|---|---|---|
| F1 – Cadastro | A → TP1 → P1 → DS | A→TP1 ✓, TP1→P1 ✓, P1→DS ✓ | Ator, Frontstage, Backstage, Suporte = 4 |
| F2 – Confirmação | P1 → E1 → TP2 → A | P1→E1 ✓, E1→TP2 ✓, TP2→A ✓ | Backstage, Externo, Frontstage, Ator = 4 |

Malha válida: todas as etapas dos dois fluxos têm aresta rastreável ao Blueprint. (F2 não começa no Ator: um fluxo pode começar em qualquer nó, desde que exista o caminho.)

### Passo 8 — Detectar as comunidades e calcular as dez métricas

**8a. Comunidades (seção 4.4.1).** FLEX e AUT dependem de `cluster(v)`, que no TCC revisado é a **comunidade estrutural** obtida pelo algoritmo guloso de Clauset-Newman-Moore sobre a versão não direcionada do Mapa, com peso = soma das `w(e)` entre cada par de nós. Aqui `m = 13` (soma dos pesos) e os graus ponderados são `K` = A:4, TP1:6, P1:8, DS:3, E1:3, TP2:2. Cada nó começa sozinho; a cada passo une-se o par de comunidades com maior ganho `ΔQ`, medido de forma exata pelo inteiro `2m·W_ij − K_i·K_j` (`ΔQ = 2·(2m·W_ij − K_i·K_j)/(2m)²`). Ganhos do primeiro passo:

| Par | `W_ij` | `K_i` | `K_j` | `2m·W_ij − K_i·K_j` |
|---|---|---|---|---|
| A–TP1 | 3 | 4 | 6 | 26·3 − 4·6 = **54** |
| A–TP2 | 1 | 4 | 2 | 26·1 − 4·2 = **18** |
| DS–P1 | 3 | 3 | 8 | 26·3 − 3·8 = **54** |
| E1–P1 | 2 | 3 | 8 | 26·2 − 3·8 = **28** |
| E1–TP2 | 1 | 3 | 2 | 26·1 − 3·2 = **20** |
| P1–TP1 | 3 | 8 | 6 | 26·3 − 8·6 = **30** |

Há um **empate** no maior ganho (A–TP1 e P1–DS, ambos 54). Neste exemplo ele não muda o resultado (as duas fusões acontecem de qualquer forma; testado com 300 sorteios de desempate), mas em grafos maiores empates podem levar a divisões finais diferentes: o TCC resolve isso explorando todas as fusões empatadas e adotando a divisão de maior Q (seção 4.4.1; decisão D9 em `regras-de-negocio.md` e a seção de comunidades de `estudos-de-caso.md`). Sequência completa de fusões (o algoritmo para quando nenhuma fusão aumenta Q; o Q "todos separados" é negativo e cresce a cada fusão boa):

| # | Fusão escolhida | `2m·W_ij − K_i·K_j` | Pares empatados nesse ganho | Q após a fusão |
|---|---|---|---|---|
| 0 | (cada nó numa comunidade) | — | — | -0,2041 |
| 1 | {A} + {TP1} | 54 | 2 | -0,0444 |
| 2 | {DS} + {P1} | 54 | 1 | 0,1154 |
| 3 | {E1} + {TP2} | 20 | 1 | 0,1746 |

Resultado: **3 comunidades** — `{A, TP1}`, `{P1, DS}`, `{TP2, E1}` — com **Q = 0,1746**. O TCC pede para **registrar o Q**: valores acima de ≈ 0,3 indicam estrutura de comunidades significativa; **aqui Q é bem menor**, o que é esperado num grafo de 6 nós em anel e serve de alerta para ler FLEX e AUT com cautela neste exemplo.

Saídas para outras comunidades: `{A, TP1}` → TP1→P1 (1); `{P1, DS}` → P1→E1 (1); `{TP2, E1}` → TP2→A (1). Isolamento dos nós internos (Backstage ∪ Suporte): `P1` tem 3 arestas, 2 delas para outras comunidades (TP1→P1 e P1→E1) → `1 − 2/3 = 0,333`; `DS` tem 1 aresta (P1→DS), dentro da própria comunidade → `1,000`.

**8b. As dez métricas e a normalização** (`Norm = clamp(5 × (bruto − mín)/(máx − mín), 0, 5)`, limites do Quadro 6):

| Eixo | Cálculo | Bruto | Normalizado (0–5) |
|---|---|---|---|
| CO | `6 + 2×5` | 16 | 1,47 |
| ESC | só Frontstage tem 2+ nós: `C(2,2) = 1` par, sem aresta direta TP1–TP2 → `1/1` | 1,00 | 5,00 |
| INT | arestas tocando E1 (`P1→E1`, `E1→TP2`) / M = `2/6` | 0,33 | 1,67 |
| UX | grau de entrada médio de {TP1, TP2} = `(1+1)/2` | 1,00 | 1,67 (limite máx. provisório 3,0) |
| DISP | nenhum gargalo entre {P1, DS}: `1 − 0/2` | 1,00 | 5,00 |
| FLEX | média do isolamento de {P1, DS} = `(0,333 + 1,000)/2` | 0,667 | 3,33 |
| TR | peso da única aresta CRI (`P1→DS` = 3) / soma total (13) | 0,2308 | 1,15 |
| AUT | 3 comunidades, todas com ≤ 1 saída → `3/3` | 1,00 | 5,00 |
| VOL | soma dos pesos / M = `13/6` | 2,1667 | 2,92 |
| DEP | 2 fluxos de entrada, 4 bandas cada, média 4 → `4/5` | 0,80 | 3,75 |

Observações: (1) **FLEX deixou de ser constante** (no TCC antigo, com `cluster(v)` por conectividade fraca, valia sempre 5,00 — Achado 1 de `analise-critica.md`, resolvido pela revisão). (2) **AUT continua em 1,00 aqui**, mas por um motivo estrutural e não pela tautologia antiga: as três comunidades formam um ciclo e cada uma tem exatamente uma saída; o `Q` baixo é o sinal de que a partição, neste grafo minúsculo, pouco informa. (3) ESC e DISP também batem no teto por falta de dados (banda com poucos nós, nenhum gargalo detectável) — Achados 2 e 6 de `analise-critica.md`.

### Passo 9 — Montar o perfil e o Radar Arquitetural

```
P_servico = (CO=1,47; ESC=5,00; INT=1,67; UX=1,67; DISP=5,00; FLEX=3,33; TR=1,15; AUT=5,00; VOL=2,92; DEP=3,75)
```

### Passo 10 — Comparar com os perfis de referência e interpretar

Os perfis são as **assinaturas provisórias do Quadro 7** do TCC (atribuídas pelos pesquisadores, ainda a validar por especialistas — não são medição). Com `p_i = 0,1` para todos os eixos (soma = 1, RN07) e `d = √(Σ p_i × (Norm_i^Serviço − Norm_i^Estilo)²)`:

| Eixo | Serviço | Monólito Modular | Contribuição | Microsserviços | Contribuição |
|---|---|---|---|---|---|
| CO | 1,47 | 2,0 | 0,0280 | 4,0 | 0,6398 |
| ESC | 5,00 | 1,5 | 1,2250 | 4,0 | 0,1000 |
| INT | 1,67 | 1,0 | 0,0444 | 3,5 | 0,3361 |
| UX | 1,67 | 2,5 | 0,0694 | 2,5 | 0,0694 |
| DISP | 5,00 | 3,0 | 0,4000 | 4,0 | 0,1000 |
| FLEX | 3,33 | 1,5 | 0,3361 | 4,0 | 0,0444 |
| TR | 1,15 | 1,5 | 0,0120 | 2,5 | 0,1812 |
| AUT | 5,00 | 1,0 | 1,6000 | 4,5 | 0,0250 |
| VOL | 2,92 | 3,0 | 0,0007 | 2,0 | 0,0840 |
| DEP | 3,75 | 3,0 | 0,0563 | 4,0 | 0,0063 |
| **Soma** | | | **3,7720** | | **1,5863** |

```
d(Servico, Monólito Modular) = √3,7720 = 1,942   (38,8% do máximo possível, que é 5)
d(Servico, Microsserviços)   = √1,5863 = 1,259   (25,2% do máximo possível)
```

**Resultado (subsídio, RN09): o perfil de referência mais próximo é Microsserviços**, com margem de 0,683 (13,7% da escala). Isto **não é uma decisão arquitetural**: indica de qual assinatura o serviço mais se aproxima, e a margem diz o quão forte é a indicação (nos Estudos de Caso 1 e 2 as margens são 0,553 e 0,239). Neste exemplo a distância até o Monólito é dominada por AUT (1,6000), ESC (1,2250) e DISP (0,4000), que juntos respondem por 85,5% da soma — justamente os três eixos que estão no teto (5,00) por falta de dados num grafo de 6 nós (Q baixo, banda com poucos nós, nenhum gargalo detectável). Por isso a indicação deve ser lida como ilustração do mecanismo, não como conclusão sobre uma newsletter real; além disso as assinaturas ainda são provisórias e o arquiteto deve considerar custos, equipe e demais restrições que o Mapa não captura.

### Linhas da Matriz de Rastreabilidade para este exemplo

| Elemento do Blueprint (etapa) | Nó/Aresta | Métrica | Eixo(s) | Contribuição |
|---|---|---|---|---|
| Validação salva o cadastro (etapa 2) | P1→DS (CRI) | `tipo(e)`, `w(e)` | TR | Única aresta crítica do fluxo; eleva TR. |
| Confirmação por e-mail via provedor externo (etapa 4) | P1→E1→TP2 (ASY) | `tipo(e)`, `E_Externo/M` | INT, VOL | Traz a única integração externa do fluxo. |
| Notificação ao cliente (etapas 4 e 5) | Comunidade {TP2, E1} | `isolamento(v)`; `saídas_externas(c)` | FLEX, AUT | Agrupamento com uma única saída (TP2→A), que contribui para a autonomia dos módulos. |

---

Este guia é deliberadamente pequeno para ser seguido à mão. Para conferir a mesma mecânica com grafos maiores (15 nós, 21 arestas, 4 comunidades) e números publicados no TCC, veja `estudos-de-caso.md`.
