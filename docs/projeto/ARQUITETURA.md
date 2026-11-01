# Arquitetura

Visão da estrutura do SBAR: como o back-end está dividido, como uma requisição percorre as camadas e quais decisões de desenho sustentam o método. Para o *o quê* (requisitos), ver [`regras-de-negocio.md`](../dominio/regras-de-negocio.md); para as fórmulas, [`formulas-metricas.md`](../dominio/formulas-metricas.md); para o contrato HTTP, [`API.md`](./API.md).

## 1. Visão geral

```
Sbar.Api  →  Sbar.Application  →  Sbar.Domain
                    ↑
            Sbar.Infrastructure
```

Clean Architecture em quatro projetos (`backend/src/`), cada um com um projeto de testes irmão.

| Camada | Papel | Depende de |
|---|---|---|
| `Sbar.Domain` | Entidades, enums, regras puras, exceções e as **interfaces** (repositórios e serviços) | nada |
| `Sbar.Application` | Casos de uso, grafo, calculadoras dos dez eixos, normalização, comparação e matriz | Domain |
| `Sbar.Infrastructure` | EF Core/PostgreSQL, Identity, repositórios e unidade de trabalho | Domain |
| `Sbar.Api` | Controllers, DTOs, exportação, autenticação, Swagger e composição (DI) | todas |

**Regra da dependência:** `Sbar.Domain` não importa EF Core, ASP.NET nem banco, e por isso é testável isoladamente. `Sbar.Application` só enxerga as interfaces que a Infrastructure implementa. A composição acontece num único ponto, `Sbar.Api/Extensions`.

Princípios que valem para todo o código:

- **Só as entradas são persistidas** (Estudos de Caso, Nós, Arestas com justificativa, Fluxos, parâmetros versionados). Carga, gargalo, comunidades, Q, as dez métricas, o Radar, as distâncias e a Matriz de Rastreabilidade são **recalculados a cada leitura**.
- **A Banda é sempre derivada** do Tipo de Nó, em um único ponto (`RegraDeBanda`).
- **O front não calcula nada do método**: tudo vem da API.
- **Nomes do domínio em português**, iguais aos termos do método (Nó, Aresta, Banda, Carga, Gargalo, Comunidade, Fluxo, Perfil Arquitetural, Matriz de Rastreabilidade).

## 2. Fluxo do produto e modelo de acesso

Cada pessoa cria uma **conta** (e-mail e senha, com cadastro protegido por um código de convite) e modela os seus próprios Estudos de Caso. A sessão é um cookie `HttpOnly` emitido pela API; o front não guarda token. Cada conta enxerga apenas os seus Estudos de Caso, parâmetros e perfis: o recurso de outra conta responde **404**, nunca 403, para não revelar que ele existe.

Telas e rotas, na ordem em que costumam ser percorridas:

```
T1  Lista de Estudos de Caso     GET  /api/casos
T2  Novo Estudo de Caso          POST /api/casos
T3  Nós                          /api/casos/{id}/nos          (a Banda é derivada do tipo)
T4  Arestas                      /api/casos/{id}/arestas      (tipo, peso e justificativa obrigatória)
T4b Fluxos principais            /api/casos/{id}/fluxos       (entrada do usuário, validada por caminho)
T5  Mapa visual                  (grafo por banda, gargalos e comunidades)
T6  Radar                        GET /api/casos/{id}/metricas (dez eixos, comunidades, Q, μ/σ por banda)
T7  Perfil mais próximo          (mesma chamada de T6: distâncias, % do máximo, margem, contribuição por eixo)
T8  Matriz de Rastreabilidade    GET /api/casos/{id}/matriz-rastreabilidade   + exportação JSON/CSV
T9  Perfis e Parâmetros          /api/perfis-arquiteturais   /api/parametros
```

As telas T3 a T8 não formam um assistente bloqueante: é possível navegar livremente entre elas, e cada tela busca o estado atual na API (por isso nenhum resultado calculado é gravado). A API é síncrona: cada chamada calcula e responde na hora.

## 3. Sbar.Domain

- **Entidades:** `EstudoDeCaso` é a raiz do agregado (Nós, Arestas e Fluxos só existem dentro dele; `FluxoPasso` guarda a sequência de um `Fluxo`). Também há `PerfilArquitetural`, `ConfiguracaoDeEixo` (limites e peso de cada eixo) e `VersaoDeParametros`. `EstudoDeCaso`, `PerfilArquitetural`, `ConfiguracaoDeEixo` e `VersaoDeParametros` têm um `DonoId` (só um `Guid`: a conta em si fica na Infrastructure, para o Domain não conhecer Identity).
- **Enums:** `Banda` (Ator, Frontstage, Backstage, Suporte, Externo), `TipoDeNo`, `TipoDeAresta` (Síncrona, Assíncrona, Crítica, Batch) e `Eixo` (CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP).
- **`RegraDeBanda`:** serviço estático que decide a Banda a partir do Tipo de Nó. O `No` a chama no construtor e na edição; a Banda nunca é um campo editável.
- **`Peso`:** value object que só aceita 1, 2 ou 3.
- **Value objects de parâmetros:** `ParametroDeEixo`, `AssinaturaDeReferencia`, `ConjuntoDeParametros` (valida que a soma dos pesos é 1) e `DadosDoPerfilArquitetural` (evita construtores com onze parâmetros soltos).
- **Exceções de domínio:** todas herdam de `DomainException`, que carrega o status HTTP (404 para `RecursoNaoEncontradoException`, 422 para as de validação, 409 para `ConflitoDeEstadoException`). O `GlobalExceptionHandler` converte qualquer uma em `ProblemDetails`, sem `try/catch` nos controllers.
- **Interfaces:** repositórios (`IEstudoDeCasoRepository`, `IPerfilArquiteturalRepository`, `IConfiguracaoDeEixoRepository`, `IVersaoDeParametrosRepository`, `IUnidadeDeTrabalho`), serviços (`IServicoDeCasos`, `…DeNos`, `…DeArestas`, `…DeFluxos`, `…DeMetricas`, `…DePerfis`, `…DeParametros`, `…DeRastreabilidade`) e `IUsuarioAtual`.
- **Regras de validação:** nomes limitados (Estudo de Caso 120 caracteres, descrição 1000), justificativa de aresta obrigatória (até 500), arestas paralelas permitidas, auto-referência proibida e cada nó pertencendo a um único Estudo de Caso.

## 4. Sbar.Application

**`ContextoDoMapa`.** Objeto calculado **uma única vez por consulta** (graus, cargas, comunidades e Q, gargalos, bandas ocupadas, estatísticas por banda e fluxos) e compartilhado pelas dez calculadoras. Isso evita recalcular a mesma coisa dez vezes e impede definições divergentes entre calculadoras. Quem o monta é o `ConstrutorDeContextoDoMapa`, apoiado em:

- `CalculadoraDeCarga` e `CalculadoraDeEstatisticasDeBanda` (média μ e desvio σ por banda);
- `ClassificadorDeGargalo` (carga estritamente maior que μ+σ da banda; a carga igual ao limiar não conta);
- `DetectorDeComunidades` (ver seção 9).

**Calculadoras de eixo.** Uma classe por eixo (`CalculadoraCO`, `…ESC`, `…INT`, `…UX`, `…DISP`, `…FLEX`, `…TR`, `…AUT`, `…VOL`, `…DEP`), todas implementando `ICalculadoraDeEixo`. São funções puras: recebem o `ContextoDoMapa` e devolvem um `ResultadoDeMetricaBruta` (valor ou "não aplicável", com o motivo). Cada uma cita, em comentário, a seção do TCC de onde vem a fórmula.

**Da métrica bruta ao perfil mais próximo.** `Normalizador` leva cada eixo para a escala de 0 a 5 usando os limites configurados; `ComparadorDePerfis` calcula a distância euclidiana ponderada até cada assinatura e a contribuição de cada eixo; `GeradorDeRecomendacao` monta o resultado (perfil mais próximo, distância em % do máximo e margem entre o primeiro e o segundo). A saída é sempre apresentada como **subsídio**, nunca como decisão.

**Casos de uso.** `ServicoDeCasos`, `…DeNos`, `…DeArestas`, `…DeFluxos`, `…DePerfis`, `…DeParametros`, `…DeMetricas` e `…DeRastreabilidade` orquestram repositórios e cálculo. `ConstrutorDeMatrizDeRastreabilidade` monta a matriz a partir das mesmas fórmulas, sem duplicá-las.

**Parâmetros por conta.** Os limites do Quadro 7 e as assinaturas do Quadro 8 vivem em **um só lugar em código** (`PadroesDoSistema`). Na primeira leitura de uma conta, `ServicoDeParametros` copia esses padrões para ela e grava a versão 1. Toda alteração grava, na mesma transação, um snapshot imutável (`VersaoDeParametros`), e uma análise pode ser refeita com uma versão antiga (`?versao=N`).

## 5. Sbar.Infrastructure

- **Mapeamento por Fluent API** (`IEntityTypeConfiguration<T>`, uma classe por entidade em `Mappings/`), nunca por atributos, para o Domain permanecer sem dependência de EF. Nós, Arestas e Fluxos são coleções privadas expostas só como `IReadOnlyList`, então o EF acessa o campo (`PropertyAccessMode.Field`).
- **`SbarDbContext`** herda de `IdentityUserContext`, guarda as contas e as chaves do Data Protection (sem isso, a sessão seria perdida a cada reinício do container) e aplica **filtros globais por dono** em `EstudoDeCaso`, `PerfilArquitetural`, `ConfiguracaoDeEixo` e `VersaoDeParametros`. Esquecer um `Where` numa consulta nova não vaza dado de outra conta.
- **`UnidadeDeTrabalho`:** executa uma operação numa transação com `pg_advisory_xact_lock`, com chave **por usuário**. Isso mantém sequencial a numeração das versões de parâmetros sem que uma conta bloqueie as outras.
- **Migrations** são aplicadas automaticamente quando a API sobe (`Database.Migrate()` antes de `app.Run()`); não há passo separado no deploy. Elas só **adicionam** estruturas: `DonoId` continua anulável.

## 6. Sbar.Api

- **Controllers finos:** recebem o request, chamam o serviço e devolvem um DTO. Os DTOs ficam na Api (a Application não conhece o formato HTTP). Rotas em inglês e kebab-case (`/api/casos`, `/api/perfis-arquiteturais`, `/api/account`).
- **Autenticação:** ASP.NET Core Identity com cookie `sbar.sessao` (`HttpOnly`, `SameSite=Lax`, `Secure` quando a requisição chega por HTTPS, 7 dias com renovação). Uma *fallback policy* exige usuário autenticado em todo controller; só `register`, `login` e `logout` são anônimos. Sem sessão a API responde 401, nunca redireciona.
- **Proteções do cadastro e do login:** código de convite (`Sbar:CodigoDeConvite`; vazio significa cadastro fechado), senha com no mínimo 8 caracteres, bloqueio de 5 minutos após 5 erros, limite de tentativas por IP (`Sbar:TentativasPorMinuto`, padrão 10 por minuto) e no máximo 20 Estudos de Caso por conta.
- **Atrás de um proxy HTTPS:** `UseForwardedHeaders` lê `X-Forwarded-Proto`, o que faz o cookie sair com `Secure`.
- **`IUsuarioAtual`** lê o dono da requisição do cookie e o expõe à Application e à Infrastructure como um `Guid`.
- **Exportação:** `MontadorDeExportacao` junta caso, parâmetros, métricas, recomendação e matriz; `ExportadorDeCsv` gera o CSV (`;`, UTF-8 com BOM, ponto decimal).
- **Swagger** está ligado em todos os ambientes, com exemplos de request.
- **Configuração** (variáveis de ambiente, com `__` no lugar de `:`): `ConnectionStrings__Sbar`, `Sbar__CodigoDeConvite`, `Sbar__TentativasPorMinuto`.

## 7. Fluxo de uma requisição

1. O navegador chama `GET /api/casos/{id}/metricas` com o cookie de sessão.
2. A autenticação valida o cookie; sem ele, 401. `UsuarioAtual` extrai o `DonoId`.
3. `MetricasController` chama `ServicoDeMetricas`, que lê o caso por `IEstudoDeCasoRepository`. O filtro global devolve vazio se o caso for de outra conta, e o serviço responde 404.
4. O serviço obtém os parâmetros da conta (a versão pedida ou a vigente), valida que os pesos somam 1 e monta o `ContextoDoMapa`.
5. As dez calculadoras produzem as métricas brutas; o `Normalizador` e o `ComparadorDePerfis` chegam ao perfil mais próximo.
6. O controller converte o resultado em DTO e responde 200. Nada disso é gravado.

## 8. Testes e integração contínua

Cada comportamento é testado na **menor camada que falha pelo motivo certo**:

| Camada | Ferramenta | O que cobre |
|---|---|---|
| Domain | xUnit | Regras puras (Banda, Peso, exceções) |
| Application | xUnit + Moq nos serviços de CRUD; **sem mock nas calculadoras** | Fórmulas, normalização, comparação e matriz, com os números dos dois Estudos de Caso como valores esperados |
| Api | xUnit + Testcontainers (PostgreSQL real) | Rotas ponta a ponta, isolamento entre contas, parâmetros por conta, Swagger |
| Front | Vitest + Testing Library + user-event | Telas e componentes, com a API simulada |
| E2E | Playwright (Chromium) | Só o que exige navegador, API e banco reais |

Os projetos de teste ficam em `backend/src/`, ao lado de cada camada. O CI (`.github/workflows/ci.yml`) roda em todo push e pull request para `main`, com três jobs: `build-and-test` (back-end, via `Sbar.CI.slnf`, com relatório de cobertura), `frontend` (tipos e Vitest) e `e2e`.

## 9. Decisões do método que o código preserva

- **Comunidades.** `comunidade(v)` é a comunidade por modularidade (Clauset–Newman–Moore guloso). A modularidade é calculada em **aritmética inteira**, o algoritmo explora **todos os empates** e adota a divisão de maior Q, de modo que o resultado **não depende da ordem de cadastro** dos nós. Q e a partição são devolvidos pela API. Grafo sem arestas e comunidade única têm tratamento explícito.
- **Fluxos principais** são cadastrados pelo usuário e validados como caminho dirigido (cada par consecutivo precisa de uma aresta origem→destino). Excluir um Nó ou uma Aresta nunca apaga o fluxo: só o torna inválido. Sem fluxos válidos, o eixo DEP é "não aplicável".
- **Justificativa obrigatória** em toda aresta (RN03).
- **Soma dos pesos igual a 1** (RN07): editar ou calcular com pesos inválidos é bloqueado.
- **Matriz de Rastreabilidade** derivada das mesmas fórmulas dos eixos; a etapa do Blueprint é aproximada por "Nome (Banda)", sem campo próprio.
- **Vocabulário:** a interface fala em "perfil de referência mais próximo" e mostra o aviso de que o resultado não substitui o arquiteto. Os identificadores da API (`recomendacao`, `estiloRecomendado`) foram mantidos por compatibilidade de contrato.

## 10. Limites conhecidos

- Sem compartilhamento de Estudos de Caso entre contas, sem papéis e sem recuperação de senha nem confirmação de e-mail.
- O "Assistente de Tradução" (inferir peso e tipo de aresta a partir de linguagem de negócio) não está implementado.
- Sem comparação automática entre versões de parâmetros.
- As assinaturas do Quadro 8 são **provisórias**: valores atribuídos para o experimento, ainda a validar com especialistas.
