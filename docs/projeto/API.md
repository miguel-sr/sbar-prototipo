# Guia de Integração da API — para o Front-end

> Referência completa de como consumir a API do SBAR: convenções gerais, o fluxo de telas recomendado (POP-09) e o contrato exato (request/response/erros) de cada rota. Para entender **por dentro** o que cada rota faz (camadas, classes, fórmulas), veja [`ARQUITETURA.md`](./ARQUITETURA.md) — este documento aqui é só o **contrato externo**, escrito para quem vai chamar a API, não para quem vai mexer no backend.
>
> **Este documento descreve a API como ela está hoje:** login por cookie de sessão, justificativa da aresta, fluxos principais, comunidades e `Q`, contribuição por eixo, parâmetros versionados, matriz derivada das fórmulas e exportação. Os nomes `recomendacao`/`estiloRecomendado` foram **mantidos** (D14), embora a saída seja o "perfil de referência mais próximo" (um subsídio à decisão).
>
> A mesma informação também está disponível interativa, com "Try it out", em `/swagger` (Swagger UI) sempre que a API estiver rodando — inclusive em produção, não só em desenvolvimento (decisão registrada em `ARQUITETURA.md`).

## 1. Convenções gerais

- **Exige login.** A sessão é um cookie `HttpOnly` (`sbar.sessao`) emitido por `POST /api/account/login` ou `register`; o navegador o reenvia sozinho (o front e a API parecem a mesma origem pelo proxy da Vercel). Sem sessão, toda rota fora de `/api/account/*` responde **401**. Cada conta enxerga só os seus Estudos de Caso, parâmetros e perfis: o recurso de outra conta responde **404**, nunca 403 (decisão D4; seção 2.1).
- **Tudo é síncrono.** Cada chamada calcula e responde na hora — não existe fila, job em background nem estado "processando".
- **`Content-Type: application/json`** em todo `POST`/`PUT` com corpo.
- **Nomes de campo em `camelCase`** no JSON (tanto request quanto response), mesmo as classes C# usando PascalCase internamente — é a convenção padrão do ASP.NET Core. Ex.: a classe `CriarNoRequest(string Nome, TipoDeNo Tipo)` vira `{"nome": "...", "tipo": "..."}` no JSON.
- **Enums trafegam como string, nunca como número.** Ex.: `"tipo": "Touchpoint"`, nunca `"tipo": 1`. Os valores possíveis de cada enum estão na seção 2.
- **Datas em ISO 8601 UTC** (ex.: `"2026-09-20T14:01:41.4324958Z"`).
- **`Guid` como string** no formato padrão (`"9d8bed89-2996-41d7-b36c-e46ce6674a5d"`).

### 1.1 Formato de erro

Todo erro (4xx/5xx) devolve o mesmo formato ([RFC 7807 `ProblemDetails`](https://www.rfc-editor.org/rfc/rfc7807)):

```json
{
  "status": 404,
  "title": "EstudoDeCaso '9d8bed89-...' não encontrado.",
  "traceId": "0HNON3SIK9NCL:00000001"
}
```

`title` é uma mensagem pronta para debug (não necessariamente para mostrar ao usuário final tal como está). Códigos usados pela API:

| Código | Quando acontece |
|---|---|
| `400 Bad Request` | Corpo do request malformado (JSON inválido, tipo errado). |
| `404 Not Found` | O `Guid` da rota (Estudo de Caso, Nó, Aresta, Fluxo, Perfil) ou o número da versão de parâmetros não existe. |
| `422 Unprocessable Entity` | Corpo bem-formado, mas viola uma regra de negócio: peso de Aresta fora de `{1,2,3}`, self-loop, nome vazio/só espaços/acima do limite, **justificativa da Aresta vazia**, **fluxo com etapa sem Aresta**, **soma dos pesos dos eixos ≠ 1** (edição de parâmetros ou cálculo de `/metricas`). |
| `500 Internal Server Error` | Erro não esperado — reportar como bug. |

### 1.2 Valores possíveis de cada enum

| Enum | Valores | Usado em |
|---|---|---|
| `TipoDeNo` | `Ator`, `Touchpoint`, `Processo`, `Suporte`, `Dados`, `Externo` | `CriarNoRequest.tipo` |
| `Banda` | `Ator`, `Frontstage`, `Backstage`, `Suporte`, `Externo` | `NoDto.banda` (sempre derivada do `TipoDeNo` — nunca enviada pelo cliente) |
| `TipoDeAresta` | `Sincrona`, `Assincrona`, `Critica`, `Batch` | `CriarArestaRequest.tipo` |
| `Eixo` | `CO`, `ESC`, `INT`, `UX`, `DISP`, `FLEX`, `TR`, `AUT`, `VOL`, `DEP` | `EixoResultadoDto.eixo`, `LinhaDeRastreabilidadeDto.eixos` |

## 2. Fluxo de telas recomendado (POP-09)

```
T1 ListaDeCasos      → GET  /api/casos
T2 CasoForm          → POST /api/casos
T3 MapaNos           → POST /api/casos/{id}/nos      (repetir por Nó)
                      → GET  /api/casos/{id}/nos      (e PUT/DELETE em .../nos/{noId})
T4 MapaArestas       → POST /api/casos/{id}/arestas  (repetir por Aresta, com justificativa)
                      → GET  /api/casos/{id}/arestas  (e PUT/DELETE em .../arestas/{arestaId})
T4b Fluxos           → GET/POST /api/casos/{id}/fluxos (e PUT/DELETE em .../fluxos/{fluxoId})
T5 MapaVisual        → GET  /api/casos/{id}          (nós+arestas juntos, pra desenhar o grafo)
T6 MetricasRadar     → GET  /api/casos/{id}/metricas (eixos, comunidades, Q, bandas, avisos)
T7 Perfil mais próximo → GET /api/casos/{id}/metricas (mesma chamada de T6, campo "recomendacao"; ?versao=N reaplica uma versão)
T8 MatrizRastreabilidade → GET /api/casos/{id}/matriz-rastreabilidade (e GET /api/casos/{id}/exportacao)

Fora do fluxo linear, acessíveis a qualquer momento:
  T9 PerfisArquiteturais → GET/POST /api/perfis-arquiteturais, GET/PUT/DELETE /api/perfis-arquiteturais/{id}
  Parâmetros             → GET /api/parametros, /atual, /{numero}; PUT /api/parametros/limites-e-pesos
```

T3 a T8 (incluindo os fluxos) não são um wizard bloqueante: o usuário pode voltar e editar um Nó depois de já ter visto o Radar. **Sempre busque o estado atual via API antes de mostrar T6/T7/T8** — nada é cacheado no backend, então um Nó/Aresta editado muda o resultado na próxima leitura.

## 2.1 Conta e sessão — `/api/account`

| Rota | Corpo | Respostas |
|---|---|---|
| `POST /api/account/register` | `{ "email", "senha", "codigoDeConvite" }` | **201** + cookie e `{ id, email }`; **403** convite inválido ou cadastro fechado (sem `Sbar__CodigoDeConvite`); **409** e-mail já usado; **422** e-mail/senha fora das regras (mínimo de 8 caracteres, com letra e número); **429** limite de tentativas |
| `POST /api/account/login` | `{ "email", "senha" }` | **200** + cookie e `{ id, email }`; **401** e-mail ou senha incorretos (5 erros bloqueiam a conta por 5 minutos); **429** |
| `POST /api/account/logout` | — | **204** |
| `GET /api/account/me` | — | **200** `{ id, email }` ou **401** (o front consulta ao abrir) |

O cadastro exige o **código de convite** configurado em `Sbar__CodigoDeConvite`. Quem se cadastra com o e-mail de `Sbar__EmailDoDonoDoLegado` recebe os Estudos de Caso, parâmetros, perfis e versões publicados antes das contas (sem dono). Conta nova recebe uma cópia dos padrões do Quadro 7 e do Quadro 8 na primeira leitura de parâmetros ou perfis. Cada conta pode ter até **20** Estudos de Caso (**422** ao exceder). Não há recuperação de senha nem confirmação de e-mail nesta versão.

## 3. Estudos de Caso — `/api/casos`

### `POST /api/casos` — criar um Estudo de Caso

Request:
```json
{ "nome": "Sistema de Pedidos Digital", "dominioDescricao": "Pedidos de restaurante feitos pelo app do cliente" }
```
`dominioDescricao` é opcional (pode vir `null` ou ser omitido).
`nome` é obrigatório (máx. 120 caracteres, sem só espaços) e `dominioDescricao` tem no máximo 1000; ambos recebem `trim` no servidor. Violação devolve `422`.

Response `201 Created`:
```json
{
  "id": "9d8bed89-2996-41d7-b36c-e46ce6674a5d",
  "nome": "Sistema de Pedidos Digital",
  "dominioDescricao": "Pedidos de restaurante feitos pelo app do cliente",
  "dataCriacao": "2026-09-20T14:01:41.4324958Z",
  "nos": [],
  "arestas": []
}
```

### `GET /api/casos` — listar todos

Response `200 OK`: array de `EstudoDeCasoDto` (mesmo formato acima, com `nos`/`arestas` preenchidos — a tela inicial usa isso para mostrar "N nós · M arestas" em cada card). `nos` e `arestas` saem sempre **na ordem de cadastro**.

### `GET /api/casos/{id}` — obter um, com Nós e Arestas

Response `200 OK`: um `EstudoDeCasoDto` completo. `404` se `id` não existir.

### `PUT /api/casos/{id}` — editar nome e domínio

Request: `{ "nome": "...", "dominioDescricao": "..." }` (mesmas regras de validação do `POST`). Response `200 OK`: o `EstudoDeCasoDto` atualizado. `404` se `id` não existir; `422` se o nome for inválido.

### `DELETE /api/casos/{id}` — excluir

Response `204 No Content`. **Apaga também todos os Nós e Arestas do caso** (cascata). `404` se `id` não existir.

## 4. Nós — `/api/casos/{casoId}/nos`

### `POST /api/casos/{casoId}/nos` — criar um Nó

Request:
```json
{ "nome": "Carrinho/Checkout", "tipo": "Touchpoint" }
```

Response `201 Created`:
```json
{ "id": "b1c2...", "nome": "Carrinho/Checkout", "tipo": "Touchpoint", "banda": "Frontstage" }
```

`banda` é sempre calculada pelo backend a partir de `tipo` — a tela **não** deve deixar o usuário escolher a Banda diretamente:

| `tipo` enviado | `banda` devolvida |
|---|---|
| `Ator` | `Ator` |
| `Touchpoint` | `Frontstage` |
| `Processo` | `Backstage` |
| `Suporte` | `Suporte` |
| `Dados` | `Suporte` |
| `Externo` | `Externo` |

`404` se `casoId` não existir; `422` se `nome` for vazio, só espaços ou tiver mais de 100 caracteres (o servidor também aplica `trim`).

### `GET /api/casos/{casoId}/nos` — listar os Nós do caso

Response `200 OK`: array de `NoDto`, em ordem de cadastro. `404` se `casoId` não existir.

### `PUT /api/casos/{casoId}/nos/{noId}` — editar um Nó

Request: `{ "nome": "...", "tipo": "Processo" }`. A `banda` é **recalculada** a partir do novo `tipo` (mesma tabela do `POST`). Response `200 OK`: o `NoDto` atualizado. `404` se o caso ou o Nó não existirem; `422` se o nome for inválido.

### `DELETE /api/casos/{casoId}/nos/{noId}` — excluir um Nó

Response `204 No Content`. **Remove também todas as Arestas que entram ou saem do Nó**; os fluxos que passavam por ele **não são apagados** — ficam `valido: false` (seção 6). `404` se o caso ou o Nó não existirem.

## 5. Arestas — `/api/casos/{casoId}/arestas`

### `POST /api/casos/{casoId}/arestas` — criar uma Aresta

Request:
```json
{ "noOrigemId": "b1c2...", "noDestinoId": "d3e4...", "tipo": "Sincrona", "peso": 3, "justificativa": "O cliente sempre acessa o formulário — obrigatório no fluxo." }
```
`noOrigemId`/`noDestinoId` precisam ser Nós já criados **no mesmo** `casoId`. `peso` só aceita `1`, `2` ou `3`. **`justificativa` é obrigatória** (RN03: rastreabilidade até o Service Blueprint): `trim` no servidor, até 500 caracteres; vazia, só com espaços ou maior que o limite devolve `422`. Um corpo sem o campo também é rejeitado.

Response `201 Created`:
```json
{ "id": "f5a6...", "noOrigemId": "b1c2...", "noDestinoId": "d3e4...", "tipo": "Sincrona", "peso": 3, "justificativa": "O cliente sempre acessa o formulário — obrigatório no fluxo." }
```

Arestas cadastradas **antes** da exigência de justificativa (migration `AdicionaJustificativaDaAresta`) trazem o texto-padrão **"Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)"**, que o front marca como "pendente de revisão".

Erros: `404` se `casoId`/`noOrigemId`/`noDestinoId` não existirem; `422` se `noOrigemId == noDestinoId` (self-loop proibido), `peso` fora de `{1,2,3}` ou `justificativa` vazia/acima do limite.

### `GET /api/casos/{casoId}/arestas` — listar as Arestas do caso

Response `200 OK`: array de `ArestaDto`, em ordem de cadastro. `404` se `casoId` não existir.

### `PUT /api/casos/{casoId}/arestas/{arestaId}` — editar uma Aresta

Request: `{ "noOrigemId": "...", "noDestinoId": "...", "tipo": "Assincrona", "peso": 2, "justificativa": "..." }` (mesmas regras do `POST`, inclusive a `justificativa` obrigatória). Response `200 OK`: o `ArestaDto` atualizado. `404` se o caso ou a Aresta não existirem; `422` para self-loop, `peso` fora de `{1,2,3}`, justificativa inválida ou Nó de outro caso. Alterar ou excluir uma Aresta pode tornar **inválido** um fluxo que passava por ela (seção 6).

### `DELETE /api/casos/{casoId}/arestas/{arestaId}` — excluir uma Aresta

Response `204 No Content`. `404` se o caso ou a Aresta não existirem.

## 6. Fluxos principais — `/api/casos/{casoId}/fluxos`

Os **fluxos principais (F)** são **entrada do usuário** (TCC 4.2.7, RF05): cada fluxo é uma **sequência ordenada de Nós** que representa uma jornada do Blueprint. Só os fluxos **válidos** entram no cálculo do eixo DEP; sem nenhum fluxo válido, o DEP é "não aplicável".

### `POST /api/casos/{casoId}/fluxos` — cadastrar um fluxo

Request:
```json
{ "nome": "Pedido e preparo", "nosIds": ["a1…", "b2…", "c3…", "d4…"] }
```
`nosIds` pode **repetir** Nós (ex.: o CE1 F4 revisita P5), **não precisa começar no Ator** nem terminar num sink. **Cada par consecutivo** de Nós precisa ter uma Aresta `origem → destino` já cadastrada (a direção importa; arestas paralelas satisfazem o mesmo passo).

Response `201 Created`:
```json
{
  "id": "f1…",
  "nome": "Pedido e preparo",
  "nosIds": ["a1…", "b2…", "c3…", "d4…"],
  "valido": true,
  "etapaAusente": null,
  "bandasPercorridas": 3
}
```
- `bandasPercorridas`: número de Bandas **distintas** dos Nós do caminho, incluindo origem e destino (é o que alimenta o DEP).
- `valido`/`etapaAusente` são **calculados na leitura**, contra o Mapa atual. `etapaAusente` = `{ "posicao": 2, "origemId": "…", "destinoId": "…", "motivo": "…" }` (a etapa N liga o N-ésimo Nó ao seguinte; `posicao` começa em 1) quando falta a Aresta ou o Nó foi removido.

Erros: `404` se `casoId` não existir; `422` se o nome for vazio ou tiver mais de 100 caracteres, se houver menos de 2 Nós (ou mais de 100), se algum Nó for de outro Estudo de Caso **ou se uma etapa não tiver Aresta** (a mensagem indica a etapa e os nomes — CT09).

### `GET /api/casos/{casoId}/fluxos` — listar

Response `200 OK`: array de `FluxoDto`, com a validade recalculada. `404` se `casoId` não existir.

### `PUT /api/casos/{casoId}/fluxos/{fluxoId}` — editar

Mesmo corpo e mesmas validações do `POST`. `200 OK` com o fluxo atualizado; `404` se o caso ou o fluxo não existirem.

### `DELETE /api/casos/{casoId}/fluxos/{fluxoId}` — excluir

`204 No Content`; `404` se o caso ou o fluxo não existirem.

> **Excluir um Nó ou uma Aresta nunca apaga nem altera um fluxo:** ele passa a `valido: false`, com `etapaAusente` explicando o motivo, e deixa de contar no DEP (a resposta de `/metricas` ganha um aviso).

## 7. Métricas e perfil mais próximo — `/api/casos/{casoId}/metricas`

### `GET /api/casos/{casoId}/metricas[?versao=N]`

Recalcula tudo a partir do Mapa atual — não precisa (nem deve) ser cacheado pelo front entre edições do Mapa. Sem `versao`, usa os parâmetros (limites, pesos e assinaturas) **vigentes**; com `versao=N`, **reaplica o snapshot** da versão N (protocolo de avaliação, TCC 5.4).

Response `200 OK` (valores do Estudo de Caso 1 do TCC revisado; listas abreviadas):
```json
{
  "eixos": [
    { "eixo": "CO", "aplicavel": true, "valorBruto": 25.0, "valorNormalizado": 2.794, "motivo": null },
    { "eixo": "FLEX", "aplicavel": true, "valorBruto": 0.575, "valorNormalizado": 2.875, "motivo": null }
  ],
  "nosGargalo": ["…(P1)", "…(DS)"],
  "comunidades": [
    { "indice": 1, "nosIds": ["…(A)", "…(TP1)", "…(TP4)", "…(P4)", "…(E2)"], "saidasExternas": 2 }
  ],
  "modularidadeQ": 0.4004,
  "avisos": [],
  "bandas": [
    { "banda": "Backstage", "quantidadeDeNos": 5, "media": 7.2, "desvioPadrao": 1.47, "limiar": 8.67 }
  ],
  "fluxos": [
    { "id": "f1…", "nome": "Consulta de cardápio", "valido": true, "bandasPercorridas": 3 }
  ],
  "versaoDosParametros": { "numero": 1, "criadaEm": "2026-10-22T03:10:46Z", "descricao": "Versão inicial (estado vigente dos parâmetros)" },
  "recomendacao": {
    "estiloRecomendado": "Monólito Modular (provisório)",
    "margem": 0.4712,
    "distanciaMaxima": 5.0,
    "distancias": [
      {
        "perfilId": "00000000-0000-0000-0000-000000000001", "perfilNome": "Monólito Modular (provisório)",
        "distancia": 1.0366, "percentualDoMaximo": 20.73,
        "contribuicoes": [ { "eixo": "CO", "contribuicao": 0.0631 }, { "eixo": "ESC", "contribuicao": 0.3063 } ]
      },
      {
        "perfilId": "00000000-0000-0000-0000-000000000002", "perfilNome": "Microsserviços (provisório)",
        "distancia": 1.5078, "percentualDoMaximo": 30.16,
        "contribuicoes": [ { "eixo": "CO", "contribuicao": 0.1454 }, { "eixo": "ESC", "contribuicao": 0.0563 } ]
      }
    ]
  }
}
```

- `eixos` sempre tem **10 itens**, um por valor de `Eixo` — mesmo quando `aplicavel: false` (mostre `motivo` em vez de um número). O DEP é "não aplicável" quando nenhum fluxo válido está cadastrado.
- `nosGargalo`: ids de **todos** os Nós gargalo (o "ponto potencialmente crítico" do TCC), de qualquer Banda (`carga(v) > μ + σ` da própria Banda).
- `comunidades` / `modularidadeQ`: partição em **comunidades estruturais** (TCC 4.4.1, CNM guloso; base de FLEX e AUT). `indice` começa em 1, na ordem da menor posição de cadastro dos Nós; `saidasExternas` conta as arestas que partem da comunidade para outra (autônoma quando ≤ 1). `modularidadeQ` é `null` quando o Mapa não tem arestas; `Q > ~0,3` indica estrutura significativa.
- `avisos`: textos prontos para mostrar — comunidade única (FLEX e AUT degenerados), Mapa sem arestas, `Q < 0,3` e **fluxos inválidos** (ficam fora do DEP).
- `bandas`: μ, σ (populacional) e limiar `μ + σ` de cada banda ocupada (critério de gargalo, US04).
- `fluxos`: resumo dos fluxos cadastrados (os válidos alimentam o DEP).
- `versaoDosParametros`: versão dos limites, pesos e assinaturas usada (RF11, US06).
- `recomendacao` vem `null` quando não há nenhum Perfil Arquitetural cadastrado. É o **perfil de referência mais próximo** — um **subsídio** à decisão do arquiteto, nunca uma decisão (TCC 4.6, RN09); a tela deve trazer o aviso de que não substitui a análise do arquiteto. Os nomes `recomendacao`/`estiloRecomendado` foram **mantidos** por compatibilidade (decisão D14, opção 1).
  - `distancias[].percentualDoMaximo` = distância ÷ 5 × 100 (a distância máxima possível é 5); `distancias[].contribuicoes[]` = `p_i·(Norm_i do serviço − Norm_i do perfil)²` por eixo aplicável, **sem arredondar** (Σ = distância², Quadros 26/36); `margem` = segunda menor distância − menor (`null` com menos de 2 perfis; margem pequena indica perfil intermediário); `distanciaMaxima` = 5.

Erros: `404` se `casoId` ou a versão `N` não existirem; **`422`** se a soma dos pesos usada ≠ 1 (cálculo bloqueado — RN07, CT10).

## 8. Matriz de Rastreabilidade — `/api/casos/{casoId}/matriz-rastreabilidade`

### `GET /api/casos/{casoId}/matriz-rastreabilidade`

Derivada a cada leitura **das mesmas definições das calculadoras de eixo** (nada é persistido). Response `200 OK`:
```json
[
  {
    "elemento": "Validação do pedido (Backstage)",
    "noOuAresta": "Validação do pedido (gargalo)",
    "metrica": "carga(v), gargalo(v)",
    "eixos": ["DISP"],
    "contribuicao": "Carga 9 acima do limiar da Banda Backstage (μ+σ = 8,67); reduz a Disponibilidade calculada.",
    "categoria": "gargalo",
    "nosIds": ["…"],
    "arestasIds": []
  }
]
```

- `categoria`: `gargalo` | `aresta-critica` | `aresta-externa` | `comunidade` | `fluxo` | `no-frontstage`.
- **Gargalos de qualquer banda:** Backstage/Suporte → `eixos: ["DISP"]`; Frontstage → `["UX"]` (a contribuição explica que não entra no DISP); Ator/Externo → `[]` ("fora do DISP").
- Arestas CRI → `TR`; arestas ligadas à Banda Externo → `INT` (a contribuição traz tipo e peso, o que distingue arestas paralelas entre o mesmo par de Nós); **comunidades** → uma linha por comunidade (`FLEX`, `AUT`: nós, saídas externas, autônoma ou não, isolamento médio dos Nós internos); **fluxos válidos** → `DEP` (bandas percorridas); Touchpoints de grau de entrada acima da média → `UX`.
- **Sem campo de etapa do Blueprint:** a coluna "Elemento" é "Nome (Banda)". CO gera uma linha por Banda ocupada (`banda-ocupada`), ESC uma por Banda com ao menos dois nós (`pares-da-banda`) e VOL uma por aresta (`volume-da-aresta`).
- Pode vir uma lista vazia (`[]`) se o Mapa não tiver nenhum elemento relevante — não é erro. `404` se `casoId` não existir.

## 9. Perfis Arquiteturais — `/api/perfis-arquiteturais`

CRUD completo, não vinculado a nenhum Estudo de Caso — são as **assinaturas de referência** usadas por `/metricas`. Já vem semeado com as duas assinaturas **provisórias** do TCC revisado (Quadro 8: "Monólito Modular (provisório)" e "Microsserviços (provisório)"; Microsserviços com VOL = 2,0), editáveis livremente. **Toda criação, atualização ou remoção de perfil gera uma nova versão dos parâmetros** (seção 10).

### `POST /api/perfis-arquiteturais` — criar

Request (os dez valores já na escala normalizada 0–5, mesma escala do `valorNormalizado` de `/metricas`):
```json
{
  "nome": "Monólito Modular (provisório)",
  "valorCO": 2.0, "valorESC": 1.5, "valorINT": 1.0, "valorUX": 2.5, "valorDISP": 3.0,
  "valorFLEX": 1.5, "valorTR": 1.5, "valorAUT": 1.0, "valorVOL": 3.0, "valorDEP": 3.0
}
```

Response `201 Created`: o mesmo corpo, com `id` adicionado.

### `GET /api/perfis-arquiteturais` — listar todos

Response `200 OK`: array de `PerfilArquiteturalDto`.

### `GET /api/perfis-arquiteturais/{id}` — obter um

Response `200 OK`. `404` se não existir.

### `PUT /api/perfis-arquiteturais/{id}` — substituir todos os valores

Mesmo corpo do `POST`. Response `200 OK` com o perfil atualizado. `404` se não existir.

### `DELETE /api/perfis-arquiteturais/{id}` — remover

Response `204 No Content`. `404` se não existir. Depois de removido, esse perfil some da lista `distancias` de qualquer `/metricas` futura (versões antigas continuam a tê-lo no snapshot).

## 10. Parâmetros de análise e versões — `/api/parametros`

Os **parâmetros** são os limites de normalização (Quadro 7), os pesos `p_i` e as assinaturas de referência (Quadro 8). O **estado vigente** fica nas tabelas de configuração e dos Perfis; **cada alteração grava um snapshot imutável** (numeração 1, 2, 3…, sequencial mesmo com chamadas concorrentes), e cada análise informa a versão usada (`versaoDosParametros` em `/metricas` e na exportação). A versão 1 é criada sob demanda, a partir do estado vigente. Valores do cálculo reaplicáveis com `GET /metricas?versao=N`.

### `GET /api/parametros` — listar as versões

Response `200 OK`, da mais recente para a mais antiga: `[ { "numero": 2, "criadaEm": "…", "descricao": "Limites e pesos atualizados" }, … ]`.

### `GET /api/parametros/atual` e `GET /api/parametros/{numero}`

Response `200 OK`:
```json
{
  "versao": { "numero": 2, "criadaEm": "…", "descricao": "Limites e pesos atualizados" },
  "eixos": [ { "eixo": "CO", "minimo": 6, "maximo": 40, "peso": 0.1 } ],
  "assinaturas": [ { "id": "…", "nome": "Monólito Modular (provisório)", "valores": { "CO": 2.0, "ESC": 1.5, "INT": 1.0, "UX": 2.5, "DISP": 3.0, "FLEX": 1.5, "TR": 1.5, "AUT": 1.0, "VOL": 3.0, "DEP": 3.0 } } ]
}
```
`eixos` tem um item por eixo (os dez). `404` em `/{numero}` se a versão não existir.

### `PUT /api/parametros/limites-e-pesos` — editar limites e pesos (cria uma nova versão)

Request:
```json
{ "descricao": "Pesos 1/8 sem FLEX e AUT", "eixos": [ { "eixo": "CO", "minimo": 6, "maximo": 40, "peso": 0.125 } ] }
```
`descricao` é opcional (até 200 caracteres; vazio vira "Limites e pesos atualizados"). Response `200 OK`: o conjunto da **nova** versão. `422` se faltar ou repetir algum eixo, se `minimo >= maximo`, se algum peso for negativo ou se a **soma dos pesos for diferente de 1** (mensagem "Soma dos pesos igual a X; deve ser 1." — RN07, CT10); nada é gravado nesse caso. As assinaturas são editadas pelos Perfis (seção 9).

## 11. Exportação — `/api/casos/{casoId}/exportacao`

### `GET /api/casos/{casoId}/exportacao[?formato=json|csv][&versao=N]`

Exporta o **registro completo da análise** (RF13; o que o TCC 4.6.1 manda registrar). `formato` padrão = `json` (sem distinguir maiúsculas); `versao` padrão = parâmetros vigentes. Devolve arquivo com `Content-Disposition: attachment; filename="sbar-{slug-do-caso}-v{n}.{json|csv}"`.

- **JSON** (`application/json`): `{ geradoEm, caso, versaoDosParametros, parametros, nos, arestas (com justificativa), fluxos, comunidades, modularidadeQ, avisos, bandas, eixos, recomendacao (com contribuições e margem), matriz }`.
- **CSV** (`text/csv; charset=utf-8`): UTF-8 com BOM, separador `;`, **ponto decimal** (reimportável) e uma seção por bloco, cada uma iniciada por `# Nome` e separada por linha em branco: Caso, Versão dos parâmetros, Limites e pesos, Assinaturas de referência, Nós, Arestas, Fluxos principais, Comunidades, Modularidade, Avisos, Bandas, Eixos, Distâncias, Contribuições por eixo e Matriz de rastreabilidade.

Erros: `400` para `formato` diferente de `json`/`csv`; `404` se o caso ou a versão não existirem; `422` se a soma dos pesos da versão usada ≠ 1.
