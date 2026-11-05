# Arquitetura do Backend SBAR

> Documento de arquitetura previsto pelo POP-03 ("Arquitetura e Design Técnico"). Detalha e refina a estrutura esboçada em `docs/fontes/POP09_Especificacao_Tecnica_Prototipo_SBAR.pdf` à luz das regras de negócio consolidadas em `docs/dominio/`. Descreve o backend **[implementado]** (Etapas 0–9). As Etapas 0–7 foram construídas sobre a versão anterior do TCC (`TCC_II_Entrega_3.pdf`) e a **Etapa 9 (2026-10-21/22) adequou o código ao TCC revisado** (`docs/fontes/TCC.pdf`): comunidades por modularidade, fluxos principais como entrada, justificativa da aresta, parâmetros versionados, saída como subsídio com contribuição por eixo, matriz derivada das fórmulas e exportação. A seção 12 registra o desenho final e o que ficou de fora; as divergências **DV-01…DV-10** (seção 12.0) estão fechadas, salvo as marcadas como parciais. Trechos históricos (decisões datadas das seções 3 e 10) foram mantidos e anotados quando a Etapa 9 os superou.

Pré-requisitos de leitura: `docs/dominio/regras-de-negocio.md` (o quê), `docs/dominio/formulas-metricas.md` (as fórmulas), `docs/dominio/estudos-de-caso.md` (dados de teste).

> Para uma visão rápida com diagramas de sequência e uma tabela de status por Etapa, veja [`FLUXO_E_STATUS.md`](./FLUXO_E_STATUS.md) — este documento aqui explica o *porquê* de cada decisão; aquele mostra o *o quê* de forma visual.

## 0. Fluxo do produto e modelo de acesso

Sem esta seção, a arquitetura das seções seguintes fica sem o contexto de "para quem" e "como" ela é usada — por isso vem antes de tudo.

**Não há usuário nem login.** Isso não é uma lacuna de design, é a decisão **D4** (`regras-de-negocio.md`), que por sua vez já era a linha de base do próprio POP-09 — a tela inicial do protótipo (`T1 ListaDeCasos`) é a lista de casos, não uma tela de autenticação; não existe nenhuma tela de login em nenhuma versão do POP-09. Consequência direta: `EstudoDeCaso` não tem dono. É um **workspace compartilhado** — qualquer pessoa que abrir o site vê a mesma lista de Estudos de Caso que qualquer outra pessoa, e pode criar, editar ou excluir qualquer um deles. Não há isolamento por sessão, por navegador nem por usuário.

**Tudo é síncrono.** Não há fila, job em background, nem estado "processando" — o padrão AsyncSystemJob observado num dos projetos de referência do autor (seção 11) não se aplica aqui: cada tela chama a API, espera a resposta, e mostra o resultado na mesma requisição. Cadastrar um Nó é um `POST` que responde 201 na hora; abrir a tela de Métricas dispara um `GET` que recalcula os dez eixos ao vivo (RF-11) e devolve tudo na mesma resposta.

**Fluxo de telas** (nomes e ordem do POP-09 seção 1 — mapeados aos Controllers da seção 5):

```
T1 ListaDeCasos (tela inicial — GET /api/casos)
  → botão "novo caso" → T2 CasoForm (POST /api/casos: nome, domínio, descrição, data)
    → T3 MapaNos (POST/GET /api/casos/{id}/nos) — banda sempre derivada do tipo, nunca escolhida
      → T4 MapaArestas (POST/GET /api/casos/{id}/arestas) — origem, destino, tipo, peso e justificativa
        → T4b Fluxos (GET/POST /api/casos/{id}/fluxos) — fluxos principais F (entrada do usuário, validados por caminho)
          → T5 MapaVisual — desenha o grafo (nós por banda, halo em nó-gargalo, comunidades opcionais)
            → T6 MetricasRadar (GET /api/casos/{id}/metricas) — os dez eixos, comunidades e Q, μ/σ por banda, avisos
              → T7 Perfil mais próximo — mesma chamada de T6: distâncias, % do máximo, margem e contribuição por eixo
                → T8 MatrizRastreabilidade (GET /api/casos/{id}/matriz-rastreabilidade) + exportação JSON/CSV

T9 PerfisArquiteturais e Parâmetros — fora do fluxo linear, acessíveis a qualquer momento pelo menu
  (CRUD de assinaturas em /api/perfis-arquiteturais; limites, pesos e versões em /api/parametros)
```

T3 a T8 (e os fluxos) não formam um wizard bloqueante — o usuário navega livremente entre elas (ex.: voltar e editar um nó depois de já ter visto o Radar), e cada tela sempre busca o estado atual via API, nunca guarda uma cópia local desatualizada (é o motivo de RF-11 proibir persistir resultado calculado).

> ⚠️ **Consequência de D4, aceita conscientemente pelo usuário (2026-09-18)**: num deploy público (API na EC2, front na Vercel), **qualquer pessoa com o link pode apagar ou sobrescrever o Estudo de Caso de qualquer outra pessoa** — inclusive durante a avaliação com especialistas (seção 5.4 do TCC revisado; o cronograma, Quadro 17, a prevê para 10/10/2026), se mais de uma pessoa testar ao mesmo tempo. Decisão: **aceitar o risco como está**, sem nenhuma mitigação de código (nem GUID não sequencial, nem restrição de acesso) — adequado para um protótipo acadêmico de curta duração, onde o custo de um caso apagado é baixo (é só recriar). Não implementar nenhuma salvaguarda para isso a menos que essa decisão mude.

## 1. Visão geral: Clean Architecture

```
Sbar.Api  →  Sbar.Application  →  Sbar.Domain
                    ↑
            Sbar.Infrastructure
```

Regra da dependência: `Sbar.Domain` não conhece nada fora de si mesmo (nem EF Core, nem ASP.NET). `Sbar.Application` conhece `Sbar.Domain`, mas não conhece `Sbar.Infrastructure` nem `Sbar.Api` — depende apenas de **interfaces** que a Infrastructure implementa (inversão de dependência). `Sbar.Api` é a única camada que conhece todas as outras, e é onde a composição (DI) acontece.

## 2. Sbar.Domain

Zero dependências externas. Contém as entidades e a única regra de domínio verdadeiramente universal (banda deriva de tipo).

### 2.1 Entidades

```
No             { Id, Nome, Tipo (TipoDeNo), Banda (Banda), Ordem, EstudoDeCasoId }
Aresta         { Id, NoOrigemId, NoDestinoId, Tipo (TipoDeAresta), Peso (1–3), Justificativa, Ordem, EstudoDeCasoId }
Fluxo          { Id, Nome, Ordem, EstudoDeCasoId, Passos: lista ordenada de FluxoPasso { Posicao, NoId } }   // Etapa 9
EstudoDeCaso   { Id, Nome, DominioDescricao, DataCriacao, IReadOnlyList<No> Nos, IReadOnlyList<Aresta> Arestas, IReadOnlyList<Fluxo> Fluxos }
PerfilArquitetural { Id, Nome, ValorCO, ValorESC, ValorINT, ValorUX, ValorDISP, ValorFLEX, ValorTR, ValorAUT, ValorVOL, ValorDEP }
ConfiguracaoDeEixo { Eixo, LimiteMinimo, LimiteMaximo, Peso }
VersaoDeParametros { Id, Numero, CriadaEm, Descricao, SnapshotJson }                                        // Etapa 9
```

**[implementado desde 2026-10-07]** `Ordem` é a posição de cadastro (`Nos`/`Arestas` saem ordenados por `Ordem` e depois `Id`; registros anteriores têm `Ordem = 0`). O agregado também edita e remove: `Atualizar`, `AtualizarNo` (a Banda é re-derivada), `RemoverNo` (remove junto as arestas incidentes), `AtualizarAresta` e `RemoverAresta`. Textos digitados passam por `Sbar.Domain/Validations/TextoValidado` (trim; nome do caso ≤ 120, do Nó ≤ 100, descrição ≤ 1000; vazio ou acima do limite → `NomeInvalidoException`, 422). **[não existem hoje]** `Justificativa` na `Aresta`, a entidade `FluxoPrincipal` e a versão de parâmetros exigidas pelo TCC revisado — ver seção 12.

**[implementado na Etapa 9]** `Aresta.Justificativa` (obrigatória, `trim`, ≤500; `TextoValidado`); `Fluxo`/`FluxoPasso` dentro do agregado (`AdicionarFluxo`, `AtualizarFluxo`, `RemoverFluxo`, `AvaliarFluxo`): o `NoId` do passo **não tem chave estrangeira** para `Nos`, de modo que excluir um Nó ou uma Aresta não apaga nem altera o fluxo em silêncio — ele passa a ser avaliado como inválido (`AvaliacaoDeFluxo`: `Valido`, `EtapaAusente`, `BandasPercorridas`). `VersaoDeParametros` é uma entidade global (fora do agregado) e imutável: guarda o snapshot dos limites, pesos e assinaturas de cada versão (seção 12.4).

`EstudoDeCaso` é o **aggregate root**: `No` e `Aresta` só existem dentro do agregado de um Estudo de Caso (nunca são acessados/criados fora dele). Isso é imposto no construtor/métodos do agregado — ex.: `EstudoDeCaso.AdicionarNo(nome, tipo)`, nunca `new No(...)` solto sendo persistido diretamente.

**Convenção de construção das entidades**: construtor privado que valida invariantes de criação + factory estático nomeado (`No.Criar(...)`, `EstudoDeCaso.Criar(...)`) para o caminho "usuário está cadastrando agora".

**Correção feita na Etapa 2 (2026-09-18): removido o factory `Rebuild(...)`, que a versão original deste documento previa para reconstruir a entidade a partir de dados já persistidos sem rodar as validações de criação** — inspirado no padrão `Rebuild()`/`Hydrate()` do `auvo.sales.core.applications` do autor (ver seção 11). Na prática, o EF Core **nunca chama métodos de fábrica definidos pela aplicação** ao materializar uma entidade — ele usa o construtor privado sem parâmetros e escreve direto nos campos/propriedades via reflexão (é exatamente por isso que existe um `PropertyAccessMode.Field` configurado nas coleções do `EstudoDeCaso`, seção 4.2). Ou seja, `Rebuild(...)` nunca era chamado por ninguém — confirmado com `grep` no código depois de medir a cobertura de testes da Etapa 2 e notar que os métodos apareciam com 0% de uso. Removido como código morto. Se um dia a aplicação precisar reconstruir uma entidade fora do EF Core (ex.: um teste, ou uma segunda fonte de dados), essa necessidade pode reintroduzir o padrão — mas só quando houver uma chamada real, não preventivamente.

### 2.2 Enums

```
TipoDeNo    { Ator, Touchpoint, Processo, Suporte, Dados, Externo }
Banda       { Ator, Frontstage, Backstage, Suporte, Externo }
TipoDeAresta{ Sincrona, Assincrona, Critica, Batch }
```

### 2.3 Serviço de domínio: `RegraDeBanda`

Único ponto que decide `Banda` a partir de `TipoDeNo` (RF-02). Estático, sem estado, chamado pelo construtor de `No` — **nunca** exposto como campo editável em DTO de entrada. Vive em `Sbar.Domain/Services/RegraDeBanda.cs` — um "Domain Service" no sentido do DDD (regra que não pertence naturalmente a uma única entidade), mesmo padrão do `PackageDebitService` no `Domain/Services/` do `auvo.sales.core.applications` do autor (ver seção 11). Continua existindo como classe própria (não uma função privada dentro de `No`) porque precisa ser testável isoladamente (POP-06 exige testar os 6 tipos → banda um a um) e porque `regras-de-negocio.md`/`analise-critica.md` já a tratam como um conceito nomeado, não um detalhe de implementação.

```
Ator → Ator | Touchpoint → Frontstage | Processo → Backstage
Suporte → Suporte | Dados → Suporte | Externo → Externo
```

### 2.4 Value Object: `Peso`

Decisão nova (2026-09-18, inspirada no padrão de Value Objects do WorldTracker — `Email`, `Password`, `Coordinates`): em vez de `Peso` ser um `int` cru validado espalhadamente em cada lugar que o recebe, ele é um Value Object imutável que só pode existir em estado válido:

```csharp
public sealed class Peso : IEquatable<Peso>
{
    public int Valor { get; }
    private Peso(int valor) => Valor = valor;

    public static Peso Criar(int valor) =>
        valor is 1 or 2 or 3
            ? new Peso(valor)
            : throw new PesoDeArestaInvalidoException(valor);

    public static implicit operator int(Peso peso) => peso.Valor;
    public bool Equals(Peso? outro) => outro is not null && Valor == outro.Valor;
    public override bool Equals(object? obj) => Equals(obj as Peso);
    public override int GetHashCode() => Valor.GetHashCode();
    public override string ToString() => Valor.ToString();
}
```

Efeito prático: `Aresta` nunca consegue existir com peso fora de `{1,2,3}` — a invariante mora num único lugar (o construtor de `Peso`), não é responsabilidade de quem chama. `TipoDeNo`, `Banda` e `TipoDeAresta` continuam como `enum` simples (não há invariante além de "é um dos valores listados", que o próprio compilador já garante) — Value Object só compensa quando há validação de valor além do tipo.

### 2.5 Hierarquia de exceções de domínio

Decisão nova (2026-09-18, copiada quase 1:1 dos dois projetos anteriores do autor — ver seção 10): em vez de cada exceção de domínio herdar direto de `Exception`, todas herdam de uma base comum que já embute o `HttpStatusCode` esperado, para o `GlobalExceptionHandler` (seção 5.3) não precisar de um `switch` gigante mapeando tipo→status:

```csharp
public abstract class DomainException : HttpRequestException
{
    protected DomainException(string mensagem, HttpStatusCode status) : base(mensagem, null, status) { }
}

public class RecursoNaoEncontradoException : DomainException
{
    public RecursoNaoEncontradoException(string recurso, object chave)
        : base($"{recurso} '{chave}' não encontrado.", HttpStatusCode.NotFound) { }
}

public class ConflitoDeEstadoException : DomainException
{
    public ConflitoDeEstadoException(string mensagem) : base(mensagem, HttpStatusCode.Conflict) { }
}
```

Exceções específicas do método (RF-02/RF-03) herdam de `DomainException` diretamente, com `HttpStatusCode.UnprocessableEntity` (422 — erro de regra de negócio, não de "recurso ausente" nem "conflito"):

- `PesoDeArestaInvalidoException` (RF-03: fora de `{1,2,3}`, lançada pelo Value Object `Peso`)
- `NoDeOutroEstudoDeCasoException` (aresta ligando nós de Estudos de Caso diferentes)
- `ArestaAutoReferenciadaException` (self-loop proibido — decisão D6, `duvidas-pendentes.md` item 2)
- `NomeInvalidoException` (nome vazio, só espaços ou acima do limite — também usada para a descrição do domínio; **[implementado 2026-10-07]**)
- `LimitesDeNormalizacaoInvalidosException` (`max <= min`, ver `formulas-metricas.md`)
- `FluxoInvalidoException` (fluxo com menos de 2 nós ou com etapa sem aresta origem→destino; a mensagem indica a etapa e os nomes — RF05, CT09; **[implementado 2026-10-21]**)
- `ParametrosInvalidosException` (eixo ausente ou repetido, peso negativo, soma dos pesos ≠ 1 — RN07, CT10; **[implementado 2026-10-22]**)
- `FormatoDeExportacaoInvalidoException` (formato diferente de `json`/`csv` — 400; **[implementado 2026-10-22]**)

## 3. Sbar.Application

Onde mora toda a lógica das fórmulas — funções puras, sem HTTP, sem banco, testáveis isoladamente (POP-06).

### 3.1 Por que um `ContextoDoMapa` compartilhado (decisão nova em relação ao POP-09)

O POP-09 listava as dez calculadoras como se cada uma lesse `No`/`Aresta` crus. Na prática, ao verificar as fórmulas (`formulas-metricas.md`), quase todos os eixos precisam dos **mesmos dados derivados**: grau de entrada/saída por nó, carga por nó, comunidade por nó (modularidade, CNM guloso — seção 12.1; até a Etapa 9 era o componente conexo ignorando direção), gargalo por nó, bandas ocupadas, arestas por banda. Calcular isso dez vezes (uma por calculadora) seria redundante e arriscaria dessincronizar critérios (ex.: duas calculadoras usando definições ligeiramente diferentes de "cluster").

**Decisão**: introduzir um objeto de leitura `ContextoDoMapa`, construído **uma vez por consulta de métricas**, que pré-calcula tudo isso. As dez calculadoras recebem o `ContextoDoMapa` pronto, não o `EstudoDeCaso` cru.

```
Grafo/
  ContextoDoMapa.cs              (DTO imutável: graus, cargas, comunidades + Q + avisos, gargalos, bandas ocupadas, estatísticas por banda, fluxos cadastrados)
  ConstrutorDeContextoDoMapa.cs  (monta o ContextoDoMapa a partir de EstudoDeCaso)
  CalculadoraDeCarga.cs          (carga(v), seção 4.3.3)
  DetectorDeComunidades.cs       (CNM guloso sobre o grafo não-direcionado ponderado, aritmética inteira, desempate fixo — seção 12.1; substitui o DetectorDeClusters)
  ResultadoDeComunidades.cs      (IndicePorNo, Membros, ModularidadeQ?, Avisos)
  CalculadoraDeEstatisticasDeBanda.cs (μ_b, σ_b — desvio padrão POPULACIONAL, confirmado por conferência com o Estudo de Caso 1)
  ClassificadorDeGargalo.cs      (carga(v) > μ_b + σ_b, seção 4.2.6)
  FluxoPrincipal.cs              (fluxo cadastrado + validade; só os válidos formam F e entram no DEP — o DetectorDeFluxosPrincipais foi removido na Etapa 9)
```

### 3.2 Calculadoras de eixo

```
Calculadoras/
  ICalculadoraDeEixo.cs   — Calcular(ContextoDoMapa contexto) : ResultadoDeMetricaBruta
  CalculadoraCO.cs   CalculadoraESC.cs   CalculadoraINT.cs   CalculadoraUX.cs
  CalculadoraDISP.cs CalculadoraFLEX.cs  CalculadoraTR.cs    CalculadoraAUT.cs
  CalculadoraVOL.cs  CalculadoraDEP.cs
```

Cada uma implementa exatamente a fórmula correspondente em `formulas-metricas.md`, com a seção do TCC citada em comentário (RNF-03/POP-04).

**`ResultadoDeMetricaBruta`** (não é só `double`): várias métricas têm caso "não aplicável" na própria especificação (INT quando `M=0`, UX quando não há Frontstage, AUT quando não há clusters/comunidades, DEP quando não há fluxos principais). Modelar isso como tipo cru evita usar `NaN`/`-1` como sinalizador escondido:

```csharp
record ResultadoDeMetricaBruta(bool Aplicavel, double? Valor, string? Motivo);
```

**[implementado na Etapa 9]** `CalculadoraDEP` usa `F` = os **fluxos cadastrados pelo usuário e válidos** (`contexto.FluxosPrincipais`): `DEP = (média de bandas distintas por fluxo, incluindo origem e destino) / 5`; só fica `Aplicavel = false` quando `F = ∅` ("Nenhum fluxo principal válido cadastrado"). `CalculadoraFLEX`/`CalculadoraAUT` implementam as fórmulas de 4.4.2 sobre `contexto.Comunidades` (isolamento de cada nó interno em relação às comunidades; AUT = comunidades com ≤ 1 aresta de saída para outras / |C|) — deixaram de saturar em 5,00. Histórico: até a Etapa 9, F era derivado por DFS (decisão D2, **superada**) e `cluster(v)` era o componente conexo (decisão D7, **resolvida pelo TCC revisado**).

### 3.3 Da métrica bruta à recomendação

```
Normalizacao/Normalizador.cs                              — aplica o Quadro 6 do TCC revisado (limites do conjunto de parâmetros usado na análise)
Comparacao/ComparadorDePerfis.cs                           — distância euclidiana ponderada e contribuição de cada eixo (pesos do conjunto de parâmetros)
Comparacao/GeradorDeRecomendacao.cs                        — menor distância → perfil de referência mais próximo, com % do máximo e margem (a classe e o DTO mantêm o nome "Recomendacao": D14, opção 1)
Rastreabilidade/ConstrutorDeMatrizDeRastreabilidade.cs     — usa o ContextoDoMapa para derivar as linhas das MESMAS definições das calculadoras (gargalos de qualquer banda, CRI, externas, comunidades, fluxos, Touchpoints) — seção 12.6
Parametros/SnapshotDeParametros.cs                         — serializa/lê o snapshot imutável de uma versão de parâmetros (seção 12.4)
```

### 3.4 Casos de uso (orquestração, chamados pelos Controllers)

Cada `ServicoDeX` implementa uma interface `IServicoDeX` — ver seção 3.5 sobre onde essa interface mora e por quê.

```
CasosDeUso/
  ServicoDeCasos.cs     : IServicoDeCasos     — CRUD de EstudoDeCaso (RX01)
  ServicoDeNos.cs       : IServicoDeNos       — CRUD de No, aplicando RegraDeBanda (RF02)
  ServicoDeArestas.cs   : IServicoDeArestas   — CRUD de Aresta, validando peso/tipo/justificativa/mesmo-estudo-de-caso (RF03)
  ServicoDeFluxos.cs    : IServicoDeFluxos    — CRUD de Fluxo principal com validação de caminho dirigido (RF05)
  ServicoDeMetricas.cs  : IServicoDeMetricas  — orquestra: carregar EstudoDeCaso → obter o conjunto de parâmetros (vigente ou ?versao=N)
                           e validar Σ pesos = 1 → ConstrutorDeContextoDoMapa → 10 calculadoras → Normalizador →
                           ComparadorDePerfis → GeradorDeRecomendacao (RF04 a RF10)
  ServicoDeRastreabilidade.cs : IServicoDeRastreabilidade — monta a matriz sob demanda (RF12)
  ServicoDePerfis.cs    : IServicoDePerfis    — CRUD de PerfilArquitetural; cada alteração registra uma nova versão de parâmetros (RF09/RF11)
  ServicoDeParametros.cs: IServicoDeParametros — limites, pesos e versões (snapshot imutável; v1 sob demanda) (RF11)
```

Nenhum desses serviços persiste métricas/gargalo/matriz — eles só leem e calculam na hora (RF-11).

### 3.5 Interfaces (portas) — todas moram em `Sbar.Domain`

**Decisão nova (2026-09-18, refinada depois de conferir os dois projetos de referência linha a linha — ver seção 11)**: `Sbar.Domain` concentra **toda** interface do sistema — tanto repositório/provider quanto os `IServicoDeX` da seção 3.4. Quem implementa cada uma depende de que tipo de dependência ela tem:

Subpastas por categoria — divisão sugerida pelo usuário para não deixar tudo solto num único `Interfaces/` (2026-09-18; a pasta `Providers/` deixou de existir na Etapa 9, quando limites e pesos passaram a viver no conjunto de parâmetros):

```
Sbar.Domain/Interfaces/
  Repositories/   — implementadas por Sbar.Infrastructure, tocam o banco PostgreSQL:
                    IEstudoDeCasoRepository, IPerfilArquiteturalRepository,
                    IConfiguracaoDeEixoRepository (limites e pesos), IVersaoDeParametrosRepository (snapshots),
                    IUnidadeDeTrabalho (transação + bloqueio consultivo para numerar versões — Etapa 9)
  Services/       — implementadas por Sbar.Application, "exclusivamente internas": orquestram
                    as interfaces acima e as calculadoras, sem tocar em nada externo diretamente:
                    IServicoDeCasos, IServicoDeNos, IServicoDeArestas, IServicoDeFluxos, IServicoDePerfis,
                    IServicoDeParametros, IServicoDeMetricas, IServicoDeRastreabilidade
```

Histórico: até a Etapa 8 existia uma terceira subpasta `Providers/` (`ILimitesDeNormalizacaoProvider`, `IPesosDeComparacaoProvider`, implementadas por `ConfiguracaoDeEixoProvider`) para leituras de configuração; foi **removida** quando `IServicoDeParametros` passou a entregar um `ConjuntoDeParametros` (limites + pesos + assinaturas) vigente ou de uma versão antiga (seção 12.4).

**Correção feita na Etapa 2 (2026-09-18): `INoRepository` e `IArestaRepository` foram removidas.** Não fazem sentido dado o padrão de aggregate root (seção 2.1) — `No` e `Aresta` nunca são persistidos fora de um `EstudoDeCaso`, então só existe `IEstudoDeCasoRepository` (que carrega/salva o agregado inteiro, nós e arestas inclusos via a mesma transação). Isso só ficou claro ao implementar o repositório de verdade — é o tipo de coisa que só se descobre tentando usar a interface, não só desenhando ela no papel.

Isso replica exatamente o padrão do **WorldTracker** do autor: lá, `Domain/IServices/` tem tanto `IUserService` (implementado em `Application`, porque só orquestra repositório) quanto `IExternalWeatherService`/`ITokenService` (implementados em `Infra`, porque tocam uma API HTTP externa/geração de token). O SBAR não tem hoje nenhuma dependência de API externa nem de credencial — então, diferente do WorldTracker, `Sbar.Infrastructure` só implementa interfaces que tocam o banco (repositórios e providers de configuração), nunca um `IServicoDeX`.

O outro projeto de referência (`auvo.sales.core.applications`) segue essa regra só pela metade — as interfaces de **repositório** ficam em `Domain/Contract/IRepositories/` (igual ao que decidimos aqui), mas as interfaces de **serviço de aplicação** ficam dentro do próprio `Application/IServices/`, não em Domain. Preferimos o padrão mais uniforme do WorldTracker: uma única pasta de portas, sem exceção por tipo de serviço.

`Sbar.Application` referencia `Sbar.Domain` para implementar seus `IServicoDeX` e para consumir (via DI) os repositórios/providers que `Sbar.Infrastructure` implementa — nunca referencia `Sbar.Infrastructure` diretamente (diferente do `auvo.sales`, onde `Application` referencia `Infra` no `.csproj`; mantemos aqui a inversão de dependência mais estrita).

### 3.6 Onde ficam os limites de normalização e os pesos — CONFIRMADO (2026-09-17), ampliado na Etapa 9

Decisão: **tabela no banco** (`ConfiguracoesDeEixo`, uma linha por eixo com min/max/peso), editável sem redeploy — mesmo espírito da tela de Perfis Arquiteturais (RF09). **[implementado na Etapa 9]** além da tabela e do seed por migration: rota (`PUT /api/parametros/limites-e-pesos`) e tela de edição, validação de `Σ pesos = 1` e de `mín < máx` (`ConjuntoDeParametros.ValidarEixos`; RN07/CT10) e o **registro da versão usada** em cada análise (RF11) — a tabela continua sendo o **estado vigente** e cada alteração grava um snapshot imutável (seção 12.4). Os valores do Quadro 6 seguem como seed.

### 3.7 DTOs de saída — moram em `Sbar.Api`, não em `Sbar.Application`

**Correção feita na Etapa 2 (2026-09-18)**: este documento originalmente colocava os DTOs em `Sbar.Application`, mas isso é impossível dado que os `IServicoDeX` moram em `Sbar.Domain` (seção 3.5) — `Sbar.Domain` nunca pode depender de `Sbar.Application` (seria referência circular, já que `Application` depende de `Domain`). Confirmado direto no código do WorldTracker: `IUserService.CreateAsync(User user)` recebe/devolve a **entidade de Domain** (`User`), nunca um DTO; o mapeamento pra DTO acontece só no Controller (`WorldTracker.Web/DTOs/`). O SBAR segue o mesmo padrão:

- `IServicoDeCasos`/`IServicoDeNos`/`IServicoDeArestas` (Domain) trabalham com `EstudoDeCaso`/`No`/`Aresta` (entidades) diretamente.
- Os DTOs ficam em `Sbar.Api/Dtos/`, cada um com um factory `DeEntidade(...)` que faz o mapeamento — só o Controller conhece DTO e entidade ao mesmo tempo.

```
Sbar.Api/Dtos/
  NoDto(Id, Nome, Tipo, Banda)                          — DeEntidade(No)
  ArestaDto(Id, NoOrigemId, NoDestinoId, Tipo, Peso, Justificativa) — DeEntidade(Aresta)
  FluxoDto(Id, Nome, NosIds, Valido, EtapaAusente?, BandasPercorridas) — DeAvaliado(FluxoAvaliado)
  EstudoDeCasoDto(Id, Nome, DominioDescricao, DataCriacao, Nos, Arestas) — DeEntidade(EstudoDeCaso)
  ParametrosDto (VersaoDeParametrosDto, ParametroDeEixoDto, AssinaturaDeReferenciaDto, ConjuntoDeParametrosDto)
  ExportacaoDto(...)                                    — registro completo da análise (RF13)
```

Os DTOs de métricas/recomendação/rastreabilidade (`EixoResultadoDto`, `ResultadoDeMetricasDto` — com comunidades, `modularidadeQ`, avisos, bandas, fluxos e `versaoDosParametros` —, `ComunidadeDto`, `EstatisticaDaBandaDto`, `RecomendacaoDto` — com `margem` e `distanciaMaxima` —, `DistanciaPerfilDto` — com `percentualDoMaximo` e `contribuicoes` — e `LinhaDeRastreabilidadeDto`) seguem o mesmo padrão: também ficam em `Sbar.Api/Dtos/`, não em `Application`.

**Confirmado (2026-09-17)**: "não aplicável" (`Aplicavel=false` + `Motivo`) aparece explicitamente no DTO em vez de virar `null` silencioso — assim a tela sempre pode explicar *por que* um eixo não foi calculado (ex.: "Nenhum nó na banda Frontstage" para UX), reforçando a rastreabilidade (RNF-01).

## 4. Sbar.Infrastructure

```
SbarDbContext.cs
UnidadeDeTrabalho.cs   (IUnidadeDeTrabalho: transação + pg_advisory_xact_lock para numerar versões sem colisão)
Mappings/      NoConfiguration.cs, ArestaConfiguration.cs, EstudoDeCasoConfiguration.cs, FluxoConfiguration.cs,
               FluxoPassoConfiguration.cs, PerfilArquiteturalConfiguration.cs, ConfiguracaoDeEixoConfiguration.cs,
               VersaoDeParametrosConfiguration.cs
               (cada uma implementa IEntityTypeConfiguration<T>)
Repositories/  EstudoDeCasoRepository.cs, PerfilArquiteturalRepository.cs, ConfiguracaoDeEixoRepository.cs,
               VersaoDeParametrosRepository.cs
Migrations/
```

### 4.1 Como o EF Core aprende a mapear as entidades sem o Domain conhecer o EF Core

Decisão (2026-09-18): usar **Fluent API** (`IEntityTypeConfiguration<T>`, uma classe por entidade em `Sbar.Infrastructure/Mappings/`), não atributos (`[Table]`/`[Column]`) nas entidades. Isso cumpre a regra da seção 1 ("`Sbar.Domain` nunca importa EF Core") sem precisar duplicar cada entidade numa classe de persistência à parte (`NoDb`, `ArestaDb`, ...) como o `auvo.sales.core.applications` do autor faz — aquele padrão existe lá porque o schema de banco diverge mais da entidade de domínio (colunas técnicas extras, histórico, índices compostos). O modelo do SBAR é simples o bastante (4-5 tabelas, sem esse tipo de divergência) para que a Fluent API sozinha baste; se um dia o schema precisar divergir muito da entidade, reavaliar para o padrão de classe de persistência separada.

O Value Object `Peso` é mapeado com uma conversão simples: `builder.Property(a => a.Peso).HasConversion(p => p.Valor, v => Peso.Criar(v))`.

### 4.2 Duas pegadinhas reais do EF Core encontradas na Etapa 2 (guardar para as próximas entidades)

**1. Chaves `Guid` geradas pelo cliente precisam de `ValueGeneratedNever()`.** Sem isso, o EF Core assume por convenção que toda chave `Guid` é `ValueGeneratedOnAdd` — e como `No.Criar()`/`Aresta.Criar()`/etc. já preenchem o `Id` com `Guid.NewGuid()` antes de chegar no `SaveChangesAsync()`, o EF Core acha que a entidade **já existe no banco** (porque a chave "gerada pelo banco" já tem valor) e tenta um `UPDATE` em vez de `INSERT` — resultando em `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0 row(s)`. Isso só acontece para entidades descobertas via navegação de um agregado já rastreado (ex.: um `No` novo dentro de um `EstudoDeCaso` recém-carregado) — criar e salvar um agregado inteiro do zero (`AddAsync` direto) não sofre disso. Correção: `builder.Property(x => x.Id).ValueGeneratedNever()` em **toda** entidade com chave `Guid` gerada no construtor (as 4 configurações já têm isso).

**2. Coleções privadas expostas como `IReadOnlyList` precisam de `HasField(...)` + `UsePropertyAccessMode(PropertyAccessMode.Field)`.** Como `EstudoDeCaso.Nos`/`Arestas` são `_nos.AsReadOnly()` (não uma lista mutável direta), o EF Core precisa ser instruído a ler/escrever direto no campo `_nos`/`_arestas`, nunca pela propriedade — ver `EstudoDeCasoConfiguration.cs`. Sem isso, o EF Core tenta usar a propriedade (que devolve uma cópia `ReadOnlyCollection` a cada chamada) e não consegue rastrear adições/remoções.

Ambas confirmadas com um teste de ponta a ponta real (cadastro dos 15 nós e 21 arestas do Estudo de Caso 1 via API, soma de pesos = 47 batendo com `estudos-de-caso.md`) — não é só "compilou", foi verificado contra o banco de verdade.

Schema (PostgreSQL via EF Core — trocado de SQLite em 2026-09-21, débito D8):

```
EstudosDeCaso (Id, Nome, DominioDescricao, DataCriacao)
Nos           (Id, EstudoDeCasoId FK, Nome, Tipo, Banda, Ordem)
Arestas       (Id, EstudoDeCasoId FK, NoOrigemId FK, NoDestinoId FK, Tipo, Peso, Justificativa, Ordem)
Fluxos        (Id, EstudoDeCasoId FK, Nome, Ordem)
FluxosPassos  (FluxoId FK, Posicao, NoId  — sem FK para Nos, de propósito)
PerfisArquiteturais (Id, Nome, Valor_CO...Valor_DEP)
ConfiguracoesDeEixo (Eixo PK, LimiteMinimo, LimiteMaximo, Peso)
VersoesDeParametros (Id, Numero único, CriadaEm, Descricao, SnapshotJson)
```

Migrações da Etapa 9: `AssinaturasDoQuadro8` (seed dos perfis), `AdicionaFluxosPrincipais`, `AdicionaJustificativaDaAresta` (com backfill do texto-padrão) e `AdicionaVersoesDeParametros` (sem seed: a versão 1 nasce sob demanda). Antes: `NomesComLimiteEOrdemDeCadastro` (2026-10-07; colunas `Ordem` e limites de tamanho; o `Up` corta antes os nomes que já excediam o limite). `Banda` é gravada em `Nos` (derivada no momento da criação via `RegraDeBanda`), mas nunca editável diretamente. Nada derivado (carga, gargalo, comunidades, Q, eixos, distâncias, matriz, validade de fluxo) é gravado.

## 5. Sbar.Api

```
Controllers/
  CasosController      GET/POST/PUT/DELETE /api/casos, /api/casos/{id}
  NosController         GET/POST/PUT/DELETE /api/casos/{casoId}/nos
  ArestasController     GET/POST/PUT/DELETE /api/casos/{casoId}/arestas
  FluxosController      GET/POST/PUT/DELETE /api/casos/{casoId}/fluxos
  MetricasController    GET /api/casos/{casoId}/metricas[?versao=N]
  RastreabilidadeController  GET /api/casos/{casoId}/matriz-rastreabilidade
  ExportacaoController  GET /api/casos/{casoId}/exportacao?formato=json|csv[&versao=N]
  PerfisController      GET/POST/PUT/DELETE /api/perfis-arquiteturais
  ParametrosController  GET /api/parametros, /atual, /{numero}; PUT /api/parametros/limites-e-pesos
Exportacao/ (MontadorDeExportacao, ExportadorDeCsv) — monta o ExportacaoDto e o CSV
Program.cs
  — hosting mínimo (estilo .NET 8, sem Startup.cs — um único arquivo)
  — DI via ProjectBuilderExtension.BuildProject() (seção 5.2)
  — GlobalExceptionHandler (seção 5.3)
  — Swagger/OpenAPI em /swagger, em todos os ambientes
  — CORS: NÃO configurado (nenhum AddCors/UseCors) e desnecessário: o front local usa o proxy do Vite e o publicado na Vercel usa um rewrite de `/api/*` para a API (frontend/vercel.json), então o navegador só fala com a própria origem. A API segue em HTTP na EC2
  — JsonStringEnumConverter registrado em AddControllers().AddJsonOptions(...) — sem isso,
    TipoDeNo/Banda/TipoDeAresta seriam serializados como número (0,1,2...) em vez do nome
    ("Ator", "Frontstage"...), tanto na entrada quanto na saída da API (achado da Etapa 2)
```

### 5.2 Injeção de dependência: um único ponto de wiring

Decisão (2026-09-18, copiada do padrão dos dois projetos anteriores do autor — `ProjectBuilderExtension`/`DependencyModule`): um único método de extensão, chamado uma vez em `Program.cs`, registra tudo — sem container de DI de terceiros.

```csharp
public static class ProjectBuilderExtension
{
    public static IServiceCollection BuildProject(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SbarDbContext>(o => o.UseNpgsql(config.GetConnectionString("Sbar")));

        services.AddScoped<IEstudoDeCasoRepository, EstudoDeCasoRepository>();
        services.AddScoped<IPerfilArquiteturalRepository, PerfilArquiteturalRepository>();
        services.AddScoped<IConfiguracaoDeEixoRepository, ConfiguracaoDeEixoRepository>();
        services.AddScoped<IVersaoDeParametrosRepository, VersaoDeParametrosRepository>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        services.AddScoped<IServicoDeCasos, ServicoDeCasos>();
        services.AddScoped<IServicoDeNos, ServicoDeNos>();
        services.AddScoped<IServicoDeArestas, ServicoDeArestas>();
        services.AddScoped<IServicoDeFluxos, ServicoDeFluxos>();
        services.AddScoped<IServicoDePerfis, ServicoDePerfis>();
        services.AddScoped<IServicoDeParametros, ServicoDeParametros>();
        services.AddScoped<IServicoDeMetricas, ServicoDeMetricas>();
        services.AddScoped<IServicoDeRastreabilidade, ServicoDeRastreabilidade>();
        services.AddScoped<MontadorDeExportacao>();

        // uma calculadora por eixo, resolvidas como coleção (IEnumerable<ICalculadoraDeEixo>) por ServicoDeMetricas
        services.AddScoped<ICalculadoraDeEixo, CalculadoraCO>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraESC>();
        // ... uma linha por eixo (CalculadoraINT, UX, DISP, FLEX, TR, AUT, VOL, DEP)

        return services;
    }
}
```

### 5.3 Tratamento de erros: `GlobalExceptionHandler`

Decisão (2026-09-18, copiada 1:1 dos dois projetos anteriores do autor — implementação idêntica nos dois, claramente uma convenção pessoal consolidada): um `IExceptionHandler` (padrão .NET 8) que converte qualquer `DomainException` (seção 2.5) no `HttpStatusCode` que ela já carrega, e qualquer outra exceção não tratada em 500, sempre devolvendo `ProblemDetails` (RFC 7807):

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var status = ex switch
        {
            DomainException dex when dex.StatusCode is not null => (int)dex.StatusCode,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = ex.Message,
            Extensions = { ["traceId"] = http.TraceIdentifier },
        }, ct);
        return true;
    }
}
```

Como não há autenticação/multiusuário nesta fase (D4), não há `[Authorize]`/JWT para tratar aqui — só os erros de regra de negócio e os genéricos do ASP.NET Core.

## 6. Fluxo de uma requisição, passo a passo (estado atual do código)

Esta seção descreve o que o código **de fato faz** (estado após a Etapa 9, 2026-10-22) — não o plano — e serve para entender "quem chama quem" em cada rota.

### 6.1 Montar o Service Blueprint — `POST /api/casos`, `.../nos`, `.../arestas`

```
POST /api/casos {nome, descricao}
  CasosController.Criar
    → ServicoDeCasos.CriarAsync(nome, descricao)
        → EstudoDeCaso.Criar(nome, descricao)         [Domain — entidade nasce vazia, sem Nós/Arestas]
        → IEstudoDeCasoRepository.AdicionarAsync + SalvarAsync   [Infrastructure — EF Core faz o INSERT]
  ← EstudoDeCasoDto {id, nome, ...}

POST /api/casos/{casoId}/nos {nome, tipo}
  NosController.Criar
    → ServicoDeNos.CriarAsync(casoId, nome, tipo)
        → repositorio.ObterPorIdAsync(casoId)          [carrega o agregado completo — Nós e Arestas já existentes]
        → estudoDeCaso.AdicionarNo(nome, tipo)          [Domain — RegraDeBanda.Derivar(tipo) decide a Banda; nunca é escolhida pelo cliente]
        → repositorio.SalvarAsync()                     [EF Core detecta o novo No pela navegação do agregado já rastreado e faz o INSERT]
  ← NoDto {id, nome, tipo, banda}

POST /api/casos/{casoId}/arestas {noOrigemId, noDestinoId, tipo, peso, justificativa}
  ArestasController.Criar
    → ServicoDeArestas.CriarAsync(casoId, origemId, destinoId, tipo, peso)
        → estudoDeCaso.AdicionarAresta(...)             [Domain — valida self-loop (ArestaAutoReferenciadaException), Peso ∈ {1,2,3} (Value Object) e Justificativa obrigatória (TextoValidado)]
        → repositorio.SalvarAsync()
  ← ArestaDto

POST /api/casos/{casoId}/fluxos {nome, nosIds}
  FluxosController.Criar
    → ServicoDeFluxos.CriarAsync(casoId, nome, nosIds)
        → estudoDeCaso.AdicionarFluxo(...)              [Domain — nome, ≥2 nós, nós do mesmo caso e CADA par consecutivo com aresta origem→destino; senão FluxoInvalidoException (422) indicando a etapa]
        → repositorio.SalvarAsync()
  ← FluxoDto {valido, etapaAusente, bandasPercorridas}
```

Nada aqui calcula métrica nenhuma — essas rotas só persistem a estrutura do grafo (Nós/Arestas) e os fluxos de entrada. O cálculo só acontece quando alguém pede `/metricas` (RF-11: nada de resultado derivado persistido).

### 6.2 Calcular o Radar Arquitetural e o perfil mais próximo — `GET /api/casos/{id}/metricas[?versao=N]`

```
MetricasController.Obter(casoId, versao?)
  → ServicoDeMetricas.CalcularAsync(casoId, versao)

    1. IEstudoDeCasoRepository.ObterPorIdAsync(casoId)
       Carrega o EstudoDeCaso completo (Nós, Arestas e Fluxos). Não existe → RecursoNaoEncontradoException (404).

    2. IServicoDeParametros.ObterAsync(versao)
       Devolve o ConjuntoDeParametros (limites do Quadro 6, pesos p_i e assinaturas do Quadro 7): o VIGENTE
       (tabelas ConfiguracoesDeEixo/PerfisArquiteturais) ou, com ?versao=N, o SNAPSHOT imutável daquela versão
       (404 se não existe). parametros.ValidarSomaDosPesos(): Σ p_i ≠ 1 ⇒ ParametrosInvalidosException (422) e o
       cálculo é bloqueado (RN07, CT10).

    3. ConstrutorDeContextoDoMapa.Construir(estudoDeCaso)
       Pré-computa, uma única vez, tudo que as 10 calculadoras vão precisar (decisão de design, seção 3.1):
         - CalculadoraDeCarga        → soma os pesos das arestas incidentes a cada nó (carga(v))
         - DetectorDeComunidades     → CNM guloso sobre o grafo não-direcionado ponderado: comunidades, Q e avisos (4.4.1)
         - CalculadoraDeEstatisticasDeBanda → μ e σ populacional da carga, por Banda
         - ClassificadorDeGargalo    → nó é gargalo se carga(v) > μ_banda + σ_banda (estritamente)
         - fluxos cadastrados        → cada Fluxo é avaliado contra as arestas; só os válidos formam F (DEP)

    4. Para cada uma das 10 ICalculadoraDeEixo (CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP):
       Calcular(contexto) → ResultadoDeMetricaBruta {Aplicavel, Valor, Motivo}. Função pura; se a fórmula não se
       aplica, o eixo entra como Aplicavel=false + Motivo textual, nunca um valor mascarado.

    5. Normalizador.Normalizar(valorBruto, limites) para cada eixo aplicável → ResultadoDeEixo.

    6. Assinaturas do conjunto de parâmetros (hoje: as duas do Quadro 7, mais as que o usuário criou).
       Lista vazia → a recomendação final é `null`.

    7. Para cada assinatura: ComparadorDePerfis.CalcularDistancia / contribuições
       Distância euclidiana ponderada (só eixos aplicáveis), com a contribuição de cada eixo.

    8. GeradorDeRecomendacao.Gerar(distancias)
       O perfil de menor distância é o "perfil de referência mais próximo" (subsídio, não decisão): nome,
       lista completa de distâncias (com % do máximo e contribuições), margem entre as duas menores e
       distância máxima (5).

  ← ResultadoDeMetricasDto { eixos[10], nosGargalo, comunidades, modularidadeQ, avisos, bandas, fluxos,
                              versaoDosParametros, recomendacao | null }
```

Cada chamada refaz os passos do zero a partir do que está salvo — nada de eixo, gargalo, comunidade, distância ou validade de fluxo é gravado no banco (RX02). Gravados são apenas as **entradas** (inclusive os snapshots de parâmetros).

### 6.3 Montar a Matriz de Rastreabilidade — `GET /api/casos/{id}/matriz-rastreabilidade`

```
RastreabilidadeController.Obter(casoId)
  → ServicoDeRastreabilidade.ObterAsync(casoId)
    1. IEstudoDeCasoRepository.ObterPorIdAsync(casoId) — mesma carga do agregado do fluxo 6.2.
    2. ConstrutorDeContextoDoMapa.Construir(estudoDeCaso) — recalculado nesta chamada (requisição independente
       de `/metricas`; mesmas classes de Grafo/, logo os mesmos números).
    3. ConstrutorDeMatrizDeRastreabilidade.Construir(contexto) — linhas derivadas das mesmas definições das calculadoras:
         - gargalos de qualquer banda → DISP (Backstage/Suporte), UX (Frontstage) ou nenhum eixo (Ator/Externo)
         - arestas CRI → TR · arestas ligadas à banda Externo → INT (com tipo e peso)
         - uma linha por comunidade → FLEX e AUT (nós, saídas externas, autonomia, isolamento médio)
         - uma linha por fluxo válido → DEP (bandas percorridas)
         - Touchpoints com grau de entrada acima da média → UX
  ← [ LinhaDeRastreabilidadeDto ] {elemento, noOuAresta, metrica, eixos, contribuicao, categoria, nosIds, arestasIds}
```

Nada aqui é persistido. A coluna "elemento/etapa do Blueprint" do TCC (Quadros 28/36) é aproximada por `elemento = "Nome (Banda)"` — não há campo de etapa no Nó (seção 12.6).

## 7. Estrutura de testes (POP-06)

Os projetos de teste ficam dentro de `backend/src/`, como mais um projeto irmão de cada camada — não numa pasta `tests/` separada no nível do repositório. Decisão 2026-09-18, por consistência com os dois projetos .NET anteriores do autor (`auvo.sales.core.applications` tem `src/Auvo.Sales.Core.Applications.Tests`; WorldTracker tem `src/WorldTracker.Tests` — os dois colocam o projeto de teste dentro de `src/`, nunca num nível separado).

```
backend/
  Sbar.sln
  src/
    Sbar.Domain/  Sbar.Application/  Sbar.Infrastructure/  Sbar.Api/
    Sbar.Domain.Tests/         — RegraDeBanda, invariantes do agregado EstudoDeCaso,
                                  PesoTests (Value Object: valores válidos 1/2/3 e inválidos)
    Sbar.Application.Tests/    — o grosso da suíte:
        cada Calculadora testada contra docs/dominio/estudos-de-caso.md (os DOIS Estudos de Caso, valores do TCC revisado)
        casos-limite: banda de 1 nó (σ=0), carga == limiar (não é gargalo), banda vazia,
                       M=0, n_Frontstage=0, C=∅ (métricas "não aplicável")
        ConstrutorDeContextoDoMapa, DetectorDeComunidades, Normalizador, ComparadorDePerfis, matriz, desempenho (RNF05)
        ServicoDeNos/ServicoDeArestas/ServicoDeCasos/ServicoDePerfis — com Moq mockando os
        repositórios (IEstudoDeCasoRepository etc.), já que estes são CRUD com regra de negócio,
        não funções puras
    Sbar.Api.Tests/            — ponta a ponta pela API real (WebApplicationFactory<Program> +
                                  Postgres real via Testcontainers, um banco isolado por classe
                                  de teste dentro de um único container compartilhado, migrado
                                  de verdade): cadastrar caso/nós/arestas → métricas → perfis →
                                  matriz de rastreabilidade, mais os casos de erro HTTP (404, 422)
```

**Três estratégias de teste, uma por tipo de componente** (decisão 2026-09-18, escolhendo entre as duas filosofias observadas nos projetos anteriores do autor — ver seção 11 — mais uma terceira adicionada na Etapa 6 para cobrir a "cola" entre camadas):

- **Calculadoras de eixo, `ContextoDoMapa` e tudo em `Grafo/`/`Comparacao/`/`Rastreabilidade/`**: são funções puras (entrada → saída, sem I/O) — testadas **diretamente**, sem nenhum mock, comparando contra os valores de `docs/dominio/estudos-de-caso.md`. Não faz sentido mockar nada aqui.
- **Serviços de CRUD (`ServicoDeNos`, `ServicoDeArestas`, `ServicoDeCasos`, `ServicoDePerfis`)**: testados com **Moq** sobre as interfaces de repositório (`Sbar.Domain/Interfaces/Repositories`), no estilo do WorldTracker — isola a regra de negócio do serviço (ex.: "não pode criar aresta com nó de outro Estudo de Caso") da persistência real.
- **Ponta a ponta pela API real (`Sbar.Api.Tests`, decisão 2026-09-21)**: um punhado de testes deliberadamente pequeno — não duplica a cobertura de fórmulas das duas estratégias acima. Existe porque pelo menos três bugs reais nesta sessão (round-trip de enum em JSON, `ValueGeneratedNever` do EF Core, `PropertyNameCaseInsensitive` do `HttpClient`) só apareceram testando via API de verdade, nunca nos testes com mock — são falhas de "cola" entre camadas (rotas, DI, serialização, mapeamento EF) que testes unitários não pegam por design. Cobre: o pipeline feliz completo de ponta a ponta (`PipelineCompletoTests`, reproduzindo os números do Quadro 10 do TCC anterior (hoje Quadro 25) e a matriz de 7 linhas do Estudo de Caso 1 através da API), mais os casos de erro HTTP mais comuns (404 em rota com `{id}` inexistente, 422 em payload que viola invariante de Domain).

**Convenção de nome de teste**: `Metodo_DeveResultado_QuandoCondicao` (ex.: `Calcular_DeveRetornarNaoAplicavel_QuandoNaoHaNoAtor`, `CriarAresta_DeveLancarPesoDeArestaInvalidoException_QuandoPesoForaDoIntervalo`). A estrutura (`Metodo_Verbo_Condição`) é a mesma convenção do WorldTracker do autor (lá: `Metodo_ShouldResultado_WhenCondicao`), mas com `Should`/`When` traduzidos para `Deve`/`Quando` (decisão 2026-09-18) — o WorldTracker mistura sem problema porque o domínio dele inteiro é em inglês (`User`, `Country`); o do SBAR é em português (RF-02/POP-04), então "`Should` Retornar Banda Correta `When` Tipo For Válido" ficaria com uma palavra em inglês colada a uma frase em português. Nomes de classe/projeto de teste (`RegraDeBandaTests`, `Sbar.Domain.Tests`) continuam com o sufixo `Tests` em inglês — é rótulo estrutural genérico (como `Repository`/`Service`/`Interface`), não parte da frase que descreve o comportamento testado.

Framework: **xUnit** + **FluentAssertions** + **Moq** (para os serviços de CRUD; as calculadoras não precisam de mock).

**Front-end (`frontend/`)**: duas camadas, descritas em `frontend/README.md` — Vitest + Testing Library + user-event (unidade e componente, API mockada na fronteira do `apiClient`) e Playwright (só Chromium, navegador + API + banco reais, banco isolado `sbar_e2e`, poucos fluxos longos com justificativa por arquivo). O front não testa fórmulas do método (são do backend). A suíte atual é de **2026-10-07**: backend 152 testes, front 163 testes Vitest + 7 testes E2E (5 arquivos).

## 8. Integração Contínua (POP-07/POP-08)

Decisão nova (2026-09-18): nenhum dos dois projetos de referência do autor tem hoje um pipeline que **bloqueia o merge quando um teste quebra** — o CI do WorldTracker só builda a imagem Docker e publica, sem rodar `dotnet test`. Isso é exatamente o gap que o POP-07 do próprio SBAR pede para evitar, então aqui vamos além do que os projetos de referência fazem hoje, não só copiar.

`.github/workflows/ci.yml` (roda em todo PR para `main` e todo push em `main`) tem quatro jobs: `build-and-test` (backend, via `backend/src/Sbar.CI.slnf` — o filtro de solução exclui o projeto `docker-compose.dcproj`, que a CLI do `dotnet` não carrega), `frontend` (typecheck, typecheck do E2E, build e Vitest, em paralelo ao backend), `e2e` (Playwright em Chromium com Postgres como serviço, só depois dos dois anteriores; envia `playwright-report/` e `test-results/` como artifact por 7 dias) e `build-and-push-image` (só em push direto a `main`, e só se `build-and-test` passar — publica a imagem no Docker Hub; **ainda não depende do `e2e`**, decisão pendente). Para o desenvolvimento local, `backend/src/docker-compose.dcproj` + `launchSettings.json` fazem o F5 do Visual Studio subir a API em container e o Postgres. Regra do repositório (a configurar no GitHub, não é código): branch `main` protegida, exigindo o workflow verde antes de permitir merge — é o que efetivamente cumpre o Definition of Done do POP-07 ("pipeline de CI que bloqueia merge com testes quebrando"), algo que nenhum dos dois projetos de referência tem configurado hoje.

**Deploy (`.github/workflows/cd.yml`, 2026-09-21 — decisão de hospedagem, D8)**: dispara via `workflow_run` quando o CI termina com sucesso em `main`, e roda num runner **self-hosted** instalado na própria EC2 (não em runner do GitHub) — mesmo padrão já em produção nos outros dois projetos do autor (WorldTracker, Veritus): `git pull` (atualiza `deploy/docker-compose.yml` se mudou) + `docker compose pull && docker compose up -d`. **Atualização (2026-10-25, publicado):** o CD agora faz `docker pull` + `docker run` (rede `sbar-net`, Postgres com volume `sbar-db-data`, API recriada a cada deploy) com `POSTGRES_PASSWORD` vindo dos segredos do GitHub — a EC2 não guarda repositório nem `.env`, como no WorldTracker — e a EC2, o Elastic IP, o Security Group e o key pair são criados por Terraform (`deploy/terraform/`, com trava contra rodar fora da conta do projeto). Nada de ECS/Fargate/ALB/RDS — decisão deliberada de reaproveitar a receita já validada nos outros dois projetos. O front fica na Vercel e repassa `/api/*` para a API. Passos e segredos em `docs/projeto/DEPLOY.md`.

A API aplica as migrations pendentes automaticamente no startup (`Program.cs`, `Database.Migrate()` antes de `app.Run()`) — não existe um passo separado de "rodar migration" no pipeline de deploy, já que não há orquestração gerenciada que ofereça isso de graça.

## 9. Débitos técnicos já conhecidos e como o design lida com eles

| Débito | Onde aparece no design | Status |
|---|---|---|
| D1 — valores dos perfis de referência | `PerfisArquiteturais` (seed) | **Resolvido pelo TCC revisado** (2026-10-21): o Quadro 7 publica as assinaturas "provisórias" (validação por especialistas na seção 5.4). O seed foi atualizado na Etapa 9 (migration `AssinaturasDoQuadro8`; nomes "(provisório)", Microsserviços VOL = 2,0) — DV-03 fechada. |
| D2 — definição de "fluxos principais" | `CalculadoraDEP`, entidade `Fluxo` | **Superado** (2026-10-21): a derivação automática de 2026-09-17 deu lugar ao TCC revisado, em que F é **cadastrado e validado** pelo usuário (RF05, CT09). Implementado na Etapa 9 (seção 12.2; DV-02 fechada); o `DetectorDeFluxosPrincipais` foi removido. |
| D6 — self-loop / arestas paralelas | `ServicoDeArestas`, `CalculadoraESC` | Presumido: self-loop proibido (validação explícita), arestas paralelas permitidas e contadas como par único em `arestas_intra(b)` (ver `formulas-metricas.md`). |
| D7 — `CalculadoraFLEX`/`CalculadoraAUT` eram matematicamente constantes (sempre 5,00) com `cluster(v)` por conectividade fraca (prova em `docs/dominio/analise-critica.md` Achado 1) | `Grafo/DetectorDeComunidades`, `CalculadoraFLEX`, `CalculadoraAUT` | **Resolvido pelo TCC revisado** (2026-10-21) e **implementado na Etapa 9**: `cluster(v)` é a comunidade por modularidade (CNM guloso, 4.4.1) e FLEX/AUT deixaram de ser constantes (seção 12.1; DV-01 fechada). A proposta anterior de limiar de intensidade de aresta foi preterida pelo TCC. |
| D9–D14 — decisões novas do TCC revisado (determinismo das comunidades, fluxos como entrada, justificativa da aresta, versão dos parâmetros, exportação, vocabulário "subsídio") | Seção 12 | **Recomendações implementadas por padrão** (2026-10-22), sem resposta do usuário; **aguardam confirmação** — ver `docs/dominio/duvidas-pendentes.md` (cada item traz o custo de mudar). |
| D8 — SQLite em disco efêmero no Render (deploy) | Etapa 7 do `PLANO_DE_IMPLEMENTACAO.md` | **Resolvido** (2026-09-21): trocado SQLite por PostgreSQL (Npgsql) e hospedagem decidida como EC2 + Docker Compose (não Render, não ECS/RDS) — Postgres roda em container com volume nomeado no disco da própria EC2 (não efêmero), mesmo padrão do WorldTracker/Veritus do autor. Pipeline de deploy completo e **publicado** (2026-10-25: Terraform, runner, CD por `docker run`); ver `docs/projeto/DEPLOY.md`. |

## 10. Decisões de design confirmadas

**2026-09-17:**
1. **Limites de normalização e pesos** (seção 3.6): tabela no banco (`ConfiguracoesDeEixo`), não `appsettings.json`.
2. **Métrica "não aplicável" no contrato da API** (seção 3.7): objeto explícito `{aplicavel, motivo}`, nunca `null` silencioso.
3. **Framework de testes**: xUnit + FluentAssertions.

**2026-09-18** (após levantar as convenções dos dois projetos anteriores do autor — seção 11):
4. **Todas as interfaces (repositório, provider e `IServicoDeX`) moram em `Sbar.Domain`** (seção 3.5) — `Sbar.Application` implementa os `IServicoDeX` (internos), `Sbar.Infrastructure` implementa repositórios/providers (tocam o banco). `Sbar.Application` nunca referencia `Sbar.Infrastructure` diretamente.
5. **`Peso` é um Value Object** (seção 2.4), não um `int` cru — a invariante `{1,2,3}` mora só ali.
6. **Hierarquia de exceções de domínio** com `HttpStatusCode` embutido (seção 2.5) + `GlobalExceptionHandler` genérico (seção 5.3).
7. **Mapeamento EF Core via Fluent API** (`IEntityTypeConfiguration<T>`, seção 4.1), sem atributos no Domain e sem classes de persistência paralelas.
8. **DI num único `ProjectBuilderExtension.BuildProject()`** (seção 5.2), sem container de terceiros.
9. **Moq para os serviços de CRUD; nenhum mock para as calculadoras** (seção 7) — dois tipos de componente, duas estratégias de teste.
10. **CI com `dotnet test` bloqueando merge** (seção 8) — vai além do que os dois projetos de referência fazem hoje.

**2026-09-18** (revisão de organização de pastas e nomenclatura, feita depois da Etapa 1 escrita):
11. **Pastas estruturais/de padrão em inglês; nomes de domínio em português.** Regra explícita: pastas e classes que só rotulam um *padrão de software* (`Entities`, `Enums`, `Exceptions`, `Interfaces`, `Repositories`, `Services`, `ValueObjects`, `Tests`, `Controllers`, `Extensions`) ficam em inglês, por não terem significado de negócio — combina com o que `auvo.sales.core.applications`/WorldTracker fazem, e evita meia-tradução de termo técnico sem ganho real. Nomes que carregam significado do TCC (`No`, `Aresta`, `Banda`, `Peso`, `EstudoDeCaso`, `PerfilArquitetural`, `RegraDeBanda`, e todas as futuras `CalculadoraDeX`/`ServicoDeX`) continuam em português, sem exceção (POP-04). `Sbar.Domain/Entidades/` foi renomeada para `Entities/` para seguir essa regra (o nome da pasta é estrutural; o nome de cada classe dentro dela, esse sim, é o termo do domínio).
12. **`Sbar.Domain/Interfaces/` dividida em `Repositories/`, `Providers/` e `Services/`** (seção 3.5) — sugestão do usuário para não deixar 12 interfaces soltas numa pasta só; três categorias porque `Providers` não é semanticamente um `Repository` (não segue o padrão `Obter`/`Salvar`/`Listar` de uma entidade).
13. **Convenção de nome de teste traduzida para `Metodo_DeveResultado_QuandoCondicao`** (seção 7) — o WorldTracker usa `Should`/`When`, mas o domínio dele é em inglês; o do SBAR é em português, então manter as palavras inglesas coladas a nomes de domínio em português formava uma frase híbrida. Sufixo de classe/projeto `Tests` continua em inglês (rótulo estrutural, não parte da frase).

**2026-09-18** (Etapa 2 — correções descobertas ao implementar de verdade):
14. **DTOs moram em `Sbar.Api`, não em `Sbar.Application`** (seção 3.7) — a versão anterior deste documento era logicamente impossível (`Domain` nunca pode depender de `Application`, dado que `IServicoDeX` mora em `Domain`). `IServicoDeX` trabalha com entidades de Domain diretamente; o mapeamento pra DTO acontece só no Controller, exatamente como o WorldTracker faz.
15. **`INoRepository`/`IArestaRepository` removidas** (seção 3.5) — desnecessárias dado o aggregate root; só `IEstudoDeCasoRepository` existe.
16. **Chaves `Guid` geradas pelo cliente precisam de `ValueGeneratedNever()`** (seção 4.2) — sem isso, o EF Core tenta `UPDATE` em vez de `INSERT` para entidades novas descobertas via navegação de um agregado já rastreado.
17. **`JsonStringEnumConverter` registrado globalmente na Api** (seção 5) — enums trafegam como nome (`"Ator"`), não número, tanto na entrada quanto na saída da API.

Os itens 14-17 só ficaram claros ao implementar e testar de ponta a ponta contra um banco de verdade — reforça por que a Etapa 2 exige rodar o fluxo completo antes de ser considerada pronta, não só compilar.

**2026-09-19** (Etapa 4 — as dez calculadoras + normalização):
18. **`ContextoDoMapa` passou a carregar o próprio `EstudoDeCaso`** (seção 3.1) — várias fórmulas (ESC, INT, TR, UX, FLEX...) precisam examinar nós/arestas crus além dos dados já derivados na Etapa 3; carregar a referência evita passar dois parâmetros em toda `ICalculadoraDeEixo.Calcular(...)`.
19. **`ILimitesDeNormalizacaoProvider`/`IPesosDeComparacaoProvider` com nomes de método distintos** (`ObterLimitesAsync`/`ObterPesosAsync`, não os dois `ObterTodosAsync`) — a mesma classe (`ConfiguracaoDeEixoProvider`) implementa as duas interfaces, e C# não permite dois métodos com a mesma assinatura e retorno diferente na implementação implícita.
20. **`CalculadoraFLEX`/`CalculadoraAUT` implementam a fórmula literalmente** (não retornam uma constante hardcoded) — dado como `cluster(v)` está definido, o resultado sempre converge para 1,0 bruto/5,00 normalizado (D7), mas o código calcula isso de verdade a cada chamada, não força o número. **[Superado em 2026-10-21: o TCC revisado troca a definição de cluster por comunidades — DV-01.]** *[Atualização 2026-10-22: com `cluster(v)` por comunidades (seção 12.1) as duas deixaram de saturar; a fórmula segue implementada literalmente.]*
21. **`ConfiguracoesDeEixo` semeada via migration** (`HasData`, Fluent API) com os valores provisórios do Quadro 5 do TCC anterior (hoje Quadro 7) — inclusive `Peso=0,1` para todos os eixos, usado só a partir da Etapa 5.

Validado de ponta a ponta: os dez valores brutos e normalizados batem exatamente com os Quadros 10/17 de `estudos-de-caso.md` para os dois Estudos de Caso, tanto nos testes automatizados quanto numa chamada real a `GET /api/casos/{id}/metricas`.

Nenhum ponto de arquitetura em aberto **na época da Etapa 4**. (Em 2026-10-21 o TCC revisado reabriu vários — comunidades, fluxos como entrada, justificativa, versão dos parâmetros; ver seções 9 e 12.)

**2026-09-20** (Etapa 5 — comparação e recomendação, RF-08/RF-09):
22. **`ComparadorDePerfis`/`GeradorDeRecomendacao` como funções estáticas puras** (mesmo padrão das calculadoras de eixo, seção 3.2) — recebem `IReadOnlyList<ResultadoDeEixo>` + `PerfilArquitetural` + pesos, devolvem `double`/`Recomendacao`; nenhum acesso a banco ou HTTP.
23. **`IServicoDeMetricas.CalcularAsync` passou a devolver `ResultadoDeMetricas`** (eixos + `Recomendacao?`), não mais `IReadOnlyList<ResultadoDeEixo>` cru — a recomendação é opcional (fica `null` quando não há nenhum `PerfilArquitetural` cadastrado) para não forçar o cliente da API a lidar com uma coleção vazia de distâncias como caso especial.
24. **Divergência encontrada ao validar `ComparadorDePerfis` contra a golden data**: os Quadros 11 (EC1) e 18 (EC2) de `docs/dominio/estudos-de-caso.md` calculam a distância usando o `DEP` **manual** original do TCC, não o `DEP` automático recalculado quando resolvemos D2 (item 18 desta lista). Como o código só implementa o automático, as distâncias que `ComparadorDePerfis` de fato produz são `1,862`/`1,306` (EC1) e `1,900`/`1,258` (EC2) — diferentes das publicadas (`1,867`/`1,368` e `1,915`/`1,221`), embora a recomendação preliminar (Microsserviços) não mude em nenhum dos dois casos. Registrado com nota em `estudos-de-caso.md` nos dois Quadros; `ComparadorDePerfisTests` usa os valores recalculados como golden value.
25. **Vetores de "Monólito Modular"/"Microsserviços" reconstruídos por engenharia reversa** a partir das tabelas de contribuição publicadas dos Quadros 11 e 18 do TCC anterior (hoje 27 e 37, com valores novos), resolvendo o sistema de equações simultaneamente para os dois Estudos de Caso (elimina a ambiguidade de duas soluções por eixo de um único caso, citada em D1). Os vetores reproduzem as quatro distâncias originais do TCC quase exatamente e servem de golden data em `ComparadorDePerfisTests`. **[Superado em 2026-10-21: o Quadro 8 do TCC revisado publica os vetores — diferem só em VOL de Microsserviços, 2,0 em vez de 4,2 — DV-03.]**
26. **Migration `AddSeedPerfisArquiteturais`** aplica os dois perfis placeholder (2,5 em todos os eixos) via `HasData`, mesma técnica de `ConfiguracoesDeEixo` (item 21).
27. **Decisão (2026-09-20): promover os vetores reconstruídos do item 25 a seed do produto**, substituindo o placeholder — migration `UpdateSeedPerfisArquiteturaisReconstruidos` (`UpdateData`, não `DeleteData`+`InsertData`, para preservar os `Id` fixos e não quebrar referências de quem já criou um Estudo de Caso e consultou `/metricas` antes da troca). Nomes mantidos como "(reconstruído)", não "(oficial)" — decisão revisitável na validação com especialistas de 10/10/2026 (ver `docs/dominio/duvidas-pendentes.md` item 1).

Validado de ponta a ponta via API real: `POST /api/casos` + `POST .../nos` + `POST .../arestas` recriando o Estudo de Caso 1 completo, `GET .../metricas` reproduzindo os dez valores do Quadro 10 do TCC anterior; após a migration do item 27, a mesma chamada devolveu a recomendação "Microsserviços (reconstruído)" com as distâncias `1,862`/`1,306`, sem precisar de nenhum `PUT` manual — exatamente como calculado à mão e nos testes automatizados.

**2026-09-20** (revisão pós-Etapa 5 — lista longa de parâmetros):
28. **`DadosDoPerfilArquitetural` (Value Object em `Sbar.Domain/ValueObjects/`) substitui os onze parâmetros posicionais** (`nome` + dez `double`) que `PerfilArquitetural.Criar`/`Atualizar` e `IServicoDePerfis.CriarAsync`/`AtualizarAsync` recebiam soltos. Investigado como os dois projetos de referência do autor resolvem isso (seção 11): `auvo.sales.core.applications` usa um objeto `XxxInput` por operação, definido na própria camada de serviço, reaproveitado como corpo de request na Api. Adaptação necessária para o SBAR: como `IServicoDeX` mora em `Sbar.Domain` (decisão 4/14), o objeto de entrada **também precisa morar em `Sbar.Domain`** (senão a interface de Domain dependeria de um tipo de camada superior) — por isso `DadosDoPerfilArquitetural` é um Value Object de Domain, não um `Input` de Application como no projeto de referência. `CriarPerfilArquiteturalRequest` (Api) ganhou um método `ParaDados()` que converte para esse Value Object, no mesmo espírito do `GetEntity()` do `auvo.sales`. Escopo por ora: só `PerfilArquitetural`, o caso mais grave (11 parâmetros, 10 do mesmo tipo `double`, alto risco de troca de ordem); `ServicoDeArestas.CriarAsync`/`EstudoDeCaso.AdicionarAresta` (5 parâmetros, tipos todos distintos — `Guid, Guid, Guid, TipoDeAresta, int`) não apresentam o mesmo risco e foram deixados como estão.

**2026-09-21** (Etapa 6 — Matriz de Rastreabilidade, RF-10):
29. **`ConstrutorDeMatrizDeRastreabilidade` como função pura** (mesmo padrão de `ComparadorDePerfis`/`GeradorDeRecomendacao`), em `Sbar.Application/Rastreabilidade/`, recebendo o `ContextoDoMapa` já construído — nenhum acesso a banco/HTTP, nenhum recálculo de carga/gargalo (reaproveita exatamente `contexto.Gargalos`/`contexto.Cargas`/`contexto.EstatisticasPorBanda`, os mesmos dados que `CalculadoraDISP` usa).
30. **Algoritmo da matriz é uma decisão de implementação, não uma prescrição do TCC**: o TCC não define um algoritmo determinístico para "gerar" a matriz — os Quadros 12/19 do TCC anterior (hoje Quadros 28/38 do revisado) de `estudos-de-caso.md` são exemplos de linhas escolhidas à mão pelos autores, com critério editorial (por que UX/DEP aparecem em uma linha e não em outra não é dedutível de uma regra única). A leitura adotada aqui gera exatamente três categorias de linha, cada uma usando a **mesma definição já implementada** por uma calculadora existente — nunca uma nova regra paralela: (a) todo Nó com `gargalo(v)=true` e Banda ∈ {Backstage, Suporte} (o mesmo universo somado por `CalculadoraDISP`) vira uma linha com eixo DISP; um gargalo em Ator/Frontstage/Externo não gera linha, porque não alimenta nenhuma fórmula dos dez eixos; (b) toda Aresta com `Tipo=Critica` vira uma linha com eixo TR (mesmo critério de `CalculadoraTR`); (c) toda Aresta com uma ponta na Banda Externo vira uma linha com eixo INT (mesmo critério de `CalculadoraINT`, `E_Externo`). Validado com os dois Estudos de Caso completos: EC1 gera exatamente 7 linhas (2 gargalo, 1 crítica, 4 externa), EC2 gera exatamente 8 (2 gargalo — `TP2` fica de fora por ser Frontstage —, 1 crítica, 5 externa). *[Atualização 2026-10-22: ampliado na Etapa 9 — gargalos de qualquer banda, comunidades, fluxos e Touchpoints; ver seção 12.6.]*

**2026-09-21** (revisão pós-Etapa 6 — testes de ponta a ponta):
31. **`Sbar.Api.Tests` ganhou uma terceira estratégia de teste** (seção 7): um punhado pequeno de testes pela API real via `WebApplicationFactory`, cobrindo a "cola" entre camadas (rotas, DI, serialização/enum, mapeamento EF Core) — três bugs reais desta sessão (round-trip de enum em JSON, `ValueGeneratedNever`, `PropertyNameCaseInsensitive`) só apareceram testando assim, nunca com mock. Deliberadamente pequeno: não duplica a cobertura de fórmulas das outras duas estratégias.

**2026-09-21** (Etapa 7, parte 1 — Swagger e documentação da API):
32. **Swagger habilitado em todos os ambientes**, não só Development (`app.UseSwagger()`/`UseSwaggerUI()` sem o `if (app.Environment.IsDevelopment())`) — decisão análoga à D4 original (workspace compartilhado, sem autenticação; a D4 foi revista em 2026-10-30, mas o Swagger segue público e não expõe dado de ninguém sem login): é assim que os especialistas vão bater na API antes do front existir, e não há dado sensível num protótipo acadêmico.
33. **Pegadinha real encontrada e corrigida**: por padrão, o Swashbuckle documenta enums (`TipoDeNo`, `Banda`, `TipoDeAresta`, `Eixo`) como **número** no schema OpenAPI, mesmo com o `JsonStringEnumConverter` registrado globalmente fazendo a API aceitar/devolver o **nome** em tempo de execução — o gerador de schema do Swashbuckle não inspeciona os `JsonConverter`s configurados. Sem correção, quem seguisse só o Swagger tentaria enviar `"tipo": 0` e receberia `400`. Corrigido com `EnumSchemaFilter` (`Sbar.Api/Swagger/`), um `ISchemaFilter` que reescreve o schema de todo enum como `type: string` com a lista de nomes.
34. **Exemplos de payload via `ISchemaFilter`** (`ExemploDeRequestSchemaFilter`), não via pacote adicional (`Swashbuckle.AspNetCore.Filters`) — evita uma dependência a mais só para popular `schema.Example` dos quatro Requests de criação (`CriarEstudoDeCasoRequest`, `CriarNoRequest`, `CriarArestaRequest`, `CriarPerfilArquiteturalRequest`).
35. **Comentários XML (`<summary>`/`<param>`/`<response>`) em todo Controller/Request/Dto** (`GenerateDocumentationFile` no `.csproj`, `IncludeXmlComments` no `AddSwaggerGen`) — alimentam o Swagger diretamente; `CS1591` (comentário ausente) suprimido globalmente porque só documentamos o contrato público da API, não Domain/Application.
36. **`docs/projeto/API.md`**: guia de integração para quem vai consumir a API (o front-end React da Etapa 8) — convenções gerais, formato de erro, tabela de valores de enum, fluxo de telas recomendado (POP-09) e o contrato completo com exemplos de cada uma das 14 rotas. Complementa o Swagger interativo com uma leitura corrida, sem precisar rodar a API.

**2026-09-21** (Etapa 7, parte 2 — D8 resolvido: SQLite → PostgreSQL):
37. **Decisão do usuário: trocar SQLite por PostgreSQL** (`Microsoft.EntityFrameworkCore.Sqlite` → `Npgsql.EntityFrameworkCore.PostgreSQL`, `UseSqlite` → `UseNpgsql`), resolvendo D8. Migrations antigas (SQLite) apagadas e recriadas do zero como uma única `InitialCreate` para Npgsql — sem continuidade de dados esperada, já que nenhuma base de produção existe ainda. Tipos gerados corretamente pelo provider Npgsql sem nenhum ajuste manual: `Guid`→`uuid`, `double`→`double precision`, enums continuam `text` (mesma serialização `ToString()` de antes).
38. **Postgres local via Docker Compose** (`docker-compose.yml` na raiz, `postgres:16-alpine`, credenciais `sbar`/`sbar`/banco `sbar`) — `docker compose up -d` uma vez, e o backend aponta pra ele via `appsettings.Development.json` (gitignored, só o `.example` é versionado).
39. **`Sbar.Api.Tests` migrado de SQLite em arquivo temporário para Postgres real via Testcontainers** (`Testcontainers.PostgreSql`), já que o provider mudou e as duas coisas não podem mais coexistir na mesma classe `SbarDbContext`. Um único container Postgres compartilhado por toda a suíte (`PostgresContainerFixture`, `[CollectionDefinition("Postgres")]`) — mais rápido que subir um container por classe; cada classe de teste cria/derruba seu próprio banco lógico dentro desse container (`ApiTestFactory`), preservando o mesmo isolamento que o arquivo `.db` temporário garantia antes. Ambas as classes de teste passaram de `IDisposable` síncrono para `IAsyncLifetime` (xUnit) porque criar/derrubar um banco via `NpgsqlConnection` é assíncrono. `JsonOptions` duplicado nas duas classes virou `JsonTestOptions.Padrao` compartilhado.
40. **CLAUDE.md e demais menções a "SQLite"/"ainda não implementado" no stack corrigidas** para refletir o estado real (PostgreSQL, backend com RF-01 a RF-10 prontos) — a tabela de stack do CLAUDE.md estava desatualizada desde a Etapa 0 e nunca tinha sido revisada.

**2026-09-21** (Etapa 7, parte 3 — hospedagem decidida: EC2 + Docker Compose, não Render/ECS):
41. **Decisão do usuário: reaproveitar o padrão de hospedagem já em produção no WorldTracker e no Veritus** (outros dois projetos do autor) — investigado o código real dos dois antes de decidir (não só a lembrança do autor). Achado: nenhum dos dois usa ECS/Fargate/ALB/RDS/Terraform — os dois usam **uma EC2 crua rodando Docker Compose**, atualizada por um runner self-hosted do GitHub Actions instalado na própria EC2, com imagem publicada no Docker Hub (não ECR). O Veritus especificamente já roda Postgres como container com volume nomeado (sem RDS) para persistência — exatamente o padrão adotado aqui. Decisão explícita de **não** ir para ECS/Fargate + RDS "gerenciado de verdade": isso seria trabalho novo (nenhum dos dois projetos de referência ensina esse padrão), não reaproveitamento — o critério de decisão foi "menor esforço real dado o que já existe funcionando", não "melhor prática de nuvem em abstrato".
42. **`Program.cs` aplica as migrations automaticamente no startup** (`Database.Migrate()` antes de `app.Run()`) — sem isso, não haveria nenhum passo no pipeline EC2+Compose que rodasse a migration (Render/ECS tipicamente têm um "release phase" pra isso; aqui não). Idempotente, e passou a ser usado também pelos testes de integração (`ApiTestFactory` não precisa mais chamar `Database.MigrateAsync()` manualmente).
43. **`Dockerfile` multi-stage** (`backend/src/Sbar.Api/Dockerfile`, contexto de build `backend/src/`) — restaura só com os `.csproj` primeiro pra cachear a camada de restore, depois copia o código e publica. Validado com `docker build` local e um smoke test real (`deploy/docker-compose.yml` local, API respondendo e migrations aplicando dentro do container).
44. **`deploy/docker-compose.yml`** (API + Postgres, volume nomeado, healthcheck) e **`.github/workflows/cd.yml`** (self-hosted, `git pull` + `docker compose pull/up`) versionados no repositório; `.github/workflows/ci.yml` ganhou o job `build-and-push-image` (só em push a `main`, só se os testes passarem). O que **não** dá pra versionar — criar a EC2, instalar Docker, registrar o runner, configurar os secrets do Docker Hub — está documentado passo a passo em `docs/projeto/DEPLOY.md`, já que só o usuário pode executar essa parte.

**2026-10-07** (rodada de testes manuais do front — bugs, edição/exclusão, validação):
45. **`GET /api/casos` passou a incluir Nós e Arestas** (`ListarAsync` sem `Include` fazia a tela inicial mostrar "0 nós · 0 arestas").
46. **PUT/DELETE de Estudo de Caso, Nó e Aresta** no agregado (`Atualizar`, `AtualizarNo`, `RemoverNo` com cascata nas arestas incidentes, `AtualizarAresta`, `RemoverAresta`) e nas três rotas correspondentes.
47. **Validação de nomes no domínio** (`TextoValidado`, `NomeInvalidoException`, 422) — antes só o front validava; a API aceitava nome vazio e de 5000 caracteres. Pasta `Sbar.Domain/Validations` (estrutural, em inglês — decisão 11).
48. **`Ordem` em Nó e Aresta** (ordem estável de cadastro) e migration `NomesComLimiteEOrdemDeCadastro`.
49. **`GET /metricas` devolve `nosGargalo`** (ids dos gargalos de **qualquer** banda, vindos de `ContextoDoMapa.Gargalos`): o Mapa deixou de deduzir gargalos da matriz, que só lista os de Backstage/Suporte. A matriz passou a informar tipo e peso das arestas externas, distinguindo arestas paralelas.
50. **F5 no Visual Studio e CI do front** (decisões de infraestrutura de desenvolvimento): projeto `docker-compose` na `Sbar.sln` + `Sbar.CI.slnf` para a CLI (seção 8), jobs `frontend` e `e2e` no CI, testes do front em duas camadas (seção 7).

**2026-10-21** (revisão da documentação para o TCC revisado — `docs/fontes/TCC.pdf`):
51. **Registro das divergências código × TCC** (DV-01…DV-10, seção 12.0) em vez de alterar o código: o TCC revisado muda `cluster(v)` (comunidades por modularidade), a definição de F (entrada do usuário), publica as assinaturas de referência (Quadro 8) e acrescenta RF/RNF/RN/US/CT (seção 5.3). O código segue o TCC anterior até a Etapa 9 do plano. *[Atualização 2026-10-22: DV-01…DV-10 fechadas na Etapa 9, salvo o que a seção 12.0 marca como parcial.]*
52. **Seção 12 — design proposto** (comunidades CNM determinísticas, `FluxoPrincipal`, `Justificativa`, `ConjuntoDeParametros`, contribuição por eixo e margem, matriz por eixo, exportação). O desenho do CNM foi conferido contra o TCC reproduzindo as partições e o Q dos dois estudos de caso com o `networkx`. *[Atualização 2026-10-22: o desenho foi implementado; a seção 12 passou a descrever o resultado final e os desvios do proposto.]*

**2026-10-21/22** (Etapa 9 — adequação ao TCC revisado; detalhes na seção 12):
53. **`DetectorDeComunidades` com aritmética inteira e exploração de todos os empates** (adota a divisão final de maior Q, como o TCC de 2026-10-25) no lugar do `DetectorDeClusters`; numeração das comunidades pela menor posição de cadastro dos nós. O resultado não depende da ordem de cadastro (D9 resolvida); a regra provisória anterior (maior chave de ordem de cadastro) foi substituída.
54. **Fluxo principal como entidade do agregado**, com passos sem FK para `Nos`: excluir Nó/Aresta não apaga nem altera o fluxo, que passa a inválido e sai do DEP (D10, adotada por padrão). `DetectorDeFluxosPrincipais` removido, sem "sugerir fluxos".
55. **`Aresta.Justificativa` obrigatória**, com backfill do texto-padrão "Justificativa pendente…" para as existentes (D11, adotada por padrão).
56. **Versionamento de parâmetros por snapshot**: o estado vigente fica nas tabelas existentes e cada alteração grava, na mesma transação, um snapshot imutável em `VersoesDeParametros` (v1 sob demanda); `IUnidadeDeTrabalho` com `pg_advisory_xact_lock` para numeração sequencial; `?versao=N` reaplica uma versão; Σ pesos ≠ 1 bloqueia a edição e o cálculo (D12, adotada por padrão). `ILimitesDeNormalizacaoProvider`/`IPesosDeComparacaoProvider` e a pasta `Providers/` foram removidos.
57. **Saída como subsídio**: contribuição por eixo, `% do máximo` e `margem` na API; interface com o vocabulário "perfil de referência mais próximo"; identificadores `recomendacao`/`estiloRecomendado` mantidos (D14, opção 1, adotada por padrão).
58. **Matriz derivada das fórmulas, sem campo de etapa do Blueprint** (`Elemento = "Nome (Banda)"`); TP2 (Frontstage) gera linha de UX, não de DISP, seguindo a fórmula de 7.4 em vez do Quadro 36.
59. **Exportação JSON e CSV** (`ExportacaoController`) — registro completo da análise (D13, adotada por padrão).
60. **Seed dos perfis do Quadro 7** por migration `UpdateData` nos `Id` fixos (substitui o seed "reconstruído" do item 25).


## 11. Convenções herdadas dos projetos anteriores do autor

Levantamento feito em 2026-09-18 sobre dois projetos .NET do autor (Miguel) que ele identificou como referência de estilo: `auvo.sales.core.applications` (.NET 9, Clean Architecture, 6 projetos, AsyncSystemJob/SQS/Postgres) e `WorldTracker` (monorepo .NET + React, DynamoDB, CI/CD real no GitHub Actions). Nenhum dos dois é sobre o domínio do SBAR — o valor deles aqui é puramente de **estilo de engenharia**, não de modelo de negócio.

| Adotado (seções 2.4, 2.5, 4.1, 5.2, 5.3, 7, 8) | Adaptado | Não se aplica ao SBAR |
|---|---|---|
| Hierarquia `DomainException`/`RecursoNaoEncontradoException`/`ConflitoDeEstadoException` com `HttpStatusCode` embutido — idêntica nos dois projetos, convenção pessoal consolidada | **Todas** as interfaces (repositório, provider e `IServicoDeX`) centralizadas em `Domain` — padrão do WorldTracker (`Domain/IRepositories` + `Domain/IServices`, cobrindo tanto serviços internos quanto externos). O `auvo.sales` segue isso só pela metade: interfaces de repositório ficam em Domain, mas `IXService` fica dentro do próprio `Application`/`Infra` que o implementa — não adotamos essa segunda parte | `AsyncSystemJob`/SQS/Worker (auvo.sales) — SBAR não tem operação assíncrona de longa duração |
| `GlobalExceptionHandler : IExceptionHandler` genérico com `ProblemDetails` — idêntico nos dois projetos | Value Objects (`Email`/`Password`/`Coordinates` no WorldTracker) → só `Peso` no SBAR tem invariante que justifique um Value Object; `TipoDeNo`/`Banda`/`TipoDeAresta` continuam `enum` | DynamoDB/Terraform/AWS (WorldTracker) — SBAR é PostgreSQL em container na EC2 (SQLite até 2026-09-21, trocado por D8; sem Render) |
| Um único método de wiring de DI (`ProjectBuilderExtension`/`DependencyModule`) chamado uma vez em `Program.cs` | Persistência: Fluent API em vez do padrão de classe de persistência separada (`NoDb`/`Hydrate()`) do auvo.sales — o modelo do SBAR é simples demais para justificar a duplicação | JWT/`[Authorize]`/CORS restrito por usuário — SBAR não tem autenticação nesta fase (D4) |
| Construtor privado + factory `Criar(...)` nas entidades | Testes: Moq para CRUD (estilo WorldTracker) + testes diretos sem mock para as calculadoras (nem auvo.sales nem WorldTracker têm um caso como "função pura testável isoladamente" — decisão própria do SBAR) | `MapFallback` servindo SPA do mesmo host (WorldTracker) — front do SBAR é separado, no Vercel |
| — | `Rebuild(...)` (inspirado no `Hydrate()`/reconstrução do `auvo.sales`) foi tentado e **removido na Etapa 2** — o EF Core nunca chama factory da aplicação ao materializar, então o método nunca era executado (confirmado por cobertura de teste 0% + `grep` sem nenhuma chamada) | — |
| xUnit como framework de teste | CI: ir além do que os dois projetos fazem hoje (nenhum bloqueia merge por teste quebrado) — ver seção 8 | Terraform (WorldTracker) — sem infraestrutura como código nesta fase |

Um achado à parte, fora do escopo de arquitetura mas que vale registrar: o `appsettings.json` do `auvo.sales.core.applications` tem credenciais em texto plano commitadas no repositório. O SBAR não deve repetir isso — quando houver algo sensível a configurar (nesta fase provavelmente nada, já que não há autenticação nem serviço externo), usar variáveis de ambiente (EC2/GitHub Actions secrets) ou `dotnet user-secrets` em vez de `appsettings.json` versionado.

## 12. Adequação ao TCC revisado — implementado (Etapa 9)

> **Tudo nesta seção está [implementado]** (Etapa 9, 2026-10-21/22), salvo o que consta em 12.9. O código das Etapas 0–8 seguia a versão **anterior** do TCC (`TCC_II_Entrega_3.pdf`, substituído por `TCC.pdf` em 2026-10-21); o TCC revisado mudou o método e acrescentou uma Engenharia de Requisitos do protótipo (seção 5.3: RF01–RF13, RNF01–RNF06, RN01–RN09, US01–US07, CT01–CT10). Esta seção registra o **desenho final** de cada mudança, dentro da arquitetura existente — regra do `CLAUDE.md`: se código e TCC divergem, a divergência é registrada (**DV-01…DV-10**) e o código é corrigido para o TCC. A ordem de execução e os desvios ficam em `PLANO_DE_IMPLEMENTACAO.md` (Etapa 9); as decisões D9–D14 foram **adotadas por padrão** (recomendações) e aguardam confirmação do usuário (`docs/dominio/duvidas-pendentes.md`).

### 12.0 Divergências código × TCC revisado — estado

| ID | Antes da Etapa 9 | TCC revisado [alvo] | Estado | Seção |
|---|---|---|---|---|
| **DV-01** | `DetectorDeClusters` = componentes conexos; FLEX/AUT saturavam em 5,00 | `cluster(v)` = **comunidade** por modularidade (CNM guloso, 4.4.1); registrar **Q** e a partição (RF06, CT02) | ✅ fechada | 12.1 |
| **DV-02** | `DetectorDeFluxosPrincipais` derivava F por DFS (D2); DEP 0,72/3,25 e 0,68/3,00 | F **cadastrado** pelo usuário e validado (RF05, CT09); DEP 0,6/2,50 (CE1) e 0,8/3,75 (CE2) | ✅ fechada | 12.2 |
| **DV-03** | Seed "(reconstruído)", Microsserviços VOL = 4,2 | Quadro 7: assinaturas "provisórias", VOL = **2,0** | ✅ fechada | 12.5 |
| **DV-04** | "recomendação"; só a distância total por perfil | RN09/4.6: "perfil de referência mais próximo" + aviso; % do máximo, margem e **contribuição por eixo** (RF10) | ✅ fechada (identificadores de API mantidos — D14, opção 1) | 12.5 |
| **DV-05** | `Aresta` sem justificativa | `justificativa` obrigatória (RF03, RN03, US02, CT06) | ✅ fechada | 12.3 |
| **DV-06** | Limites e pesos sem API e sem versão; soma dos pesos não validada | Editar limites, pesos e assinaturas **registrando a versão** (RF11, US06); Σ pesos = 1 (RN07, CT10) | ✅ fechada | 12.4 |
| **DV-07** | Matriz de 3 categorias; sem exportação | Quadros 28/36 e US07; exportação (RF13) | ✅ fechada — exportação; matriz **derivada das fórmulas**, com linhas por elemento para todos os eixos (CO por Banda ocupada, ESC por Banda, VOL por aresta); sem campo de etapa do Blueprint (aproximada por "Nome (Banda)") | 12.6, 12.7 |
| **DV-08** | Telas sem μ/σ por banda (US04), sem validação de fluxos (US03), sem versão (US06) | US03, US04, US06 | ✅ fechada | 12.2, 12.4 |
| **DV-09** | Sem teste de desempenho | RNF05: Mapa de 50 nós em ≤ 2 s | ✅ fechada (teste nas camadas Application e API) | 12.1 |
| **DV-10** | Comentários citando Quadros antigos; oráculos de teste do critério antigo | Numeração e valores dos Quadros 20–39 | ✅ comentários e oráculos de backend e Vitest; 🟡 E2E atualizado em seguida | 12.8 |
| **DV-11** | Eixo "não aplicável" omitido da distância **sem renormalizar** os pesos (Σ p efetivo < 1; distância encolhida; máximo deixava de ser 5) | RN07 (Σ p = 1) e RN08: `p'_i = p_i ÷ Σ p_j` entre os eixos aplicáveis; `pesosEfetivos` e `eixosOmitidos` no resultado (convenção do projeto; o TCC não detalha o caso) | ✅ fechada (2026-11-05) | `ComparadorDePerfis.CalcularPesosEfetivos` |

### 12.1 Detecção de comunidades (DV-01, D9) — substituiu `DetectorDeClusters`

- **Componente** `Sbar.Application/Grafo/DetectorDeComunidades.cs` (função pura) devolve `ResultadoDeComunidades { IndicePorNo, Membros, ModularidadeQ?, Avisos }`; o conceito agora se chama **comunidade** (TCC 4.4.1). `ContextoDoMapa.Comunidades` substitui `Clusters`.
- **Entrada**: grafo **não direcionado** ponderado, em que `A_ij` é a **soma de `w(e)`** das arestas entre `i` e `j` em qualquer sentido (paralelas e bidirecionais somam; self-loop é proibido). `m` = soma dos pesos (47 no CE1, 45 no CE2).
- **Algoritmo** (Clauset–Newman–Moore, guloso): cada nó começa numa comunidade; a cada passo une-se o par **conectado** de maior ganho de modularidade; para quando nenhuma fusão tem ganho > 0. A comparação usa **aritmética inteira exata**: `ΔQ·(2m)² = 2·(2m·W_ij − K_i·K_j)` — nunca `double` para decidir empates. `Q = Σ_c (e_cc − a_c²)`, equivalente à fórmula do TCC.
- **Empates (D9, resolvida pelo TCC de 2026-10-25)**: a cada passo explora-se **cada** fusão de ganho máximo (estado = partição, visitado uma única vez) e, entre as divisões finais, adota-se a de maior `Q`, comparado como inteiro exato (`Σ_c 4m·W_c − K_c²`). Reproduz CE1 (3 comunidades, `Q = 0,4117`) e CE2 (4 comunidades, `Q = 0,3440`) **sem depender da ordem de cadastro** (`analise-critica.md` Achado 8). Se duas divisões finais distintas tiverem o mesmo `Q`, adota-se a primeira na ordem de cadastro e o resultado avisa; acima de 20.000 estados explorados segue-se só o primeiro empate de cada passo e avisa. Comunidades são numeradas de 1 a n pela menor posição de cadastro dos seus nós (lista ordenada por `Ordem`, depois `Id`; não pela numeração narrativa do TCC).
- **Casos-limite**: `m = 0` → `Q = null`, cada nó é uma comunidade e há aviso; comunidade única → `FLEX = AUT = 1` e aviso ("não discriminam"); `Q < 0,3` → aviso de estrutura não significativa; nó sem arestas = comunidade própria; arestas paralelas e nos dois sentidos somam.
- **Efeito**: `CalculadoraFLEX`/`CalculadoraAUT` leem `Comunidades`; `GET /metricas` devolve `comunidades` (índice, nós, saídas externas), `modularidadeQ`, `avisos` e `bandas` (μ, σ, limiar).
- **Desempenho (DV-09, RNF05)**: mapa sintético de 50 nós e 70 arestas medido em poucos milissegundos (`GET /metricas` na casa de dezenas de ms; limite de 2 s); há teste nas camadas Application e API.

### 12.2 Fluxos principais como entrada do usuário (DV-02, D10)

- **Entidade `Fluxo` (+ `FluxoPasso`) dentro do agregado `EstudoDeCaso`**: `{ Id, Nome (≤100), Ordem, Passos: lista ordenada de NoId (repetição permitida) }`; métodos do agregado `AdicionarFluxo`, `AtualizarFluxo`, `RemoverFluxo` e `AvaliarFluxo`. Tabelas `Fluxos` e `FluxosPassos`; o `NoId` do passo **não tem FK** para `Nos`.
- **Invariantes**: de 2 a 100 passos; todo nó pertence ao mesmo Estudo de Caso (`NoDeOutroEstudoDeCasoException`); **cada par consecutivo precisa de uma aresta dirigida** origem→destino (`FluxoInvalidoException`, 422, mensagem com a etapa e os nomes — CT09). **Não** exige começar no Ator.
- **Validade é derivada, não persistida**: `AvaliarFluxo` devolve `AvaliacaoDeFluxo { Valido, EtapaAusente?, BandasPercorridas }` a cada leitura. Excluir um Nó ou uma Aresta **não apaga nem altera** o fluxo: ele passa a `valido = false` com a etapa ausente (inclusive "nó removido"), e deixa de contar no DEP; `avisos` informa.
- **Efeito no DEP**: `CalculadoraDEP` usa só os fluxos **válidos** (`contexto.FluxosPrincipais`): média de bandas distintas / 5; sem nenhum válido, "não aplicável". Valores de referência: DEP 0,6 → 2,50 (CE1) e 0,8 → 3,75 (CE2).
- **`DetectorDeFluxosPrincipais` foi removido** (sem "sugerir fluxos"); Estudos de Caso gravados antes da Etapa 9 ficam com `F = ∅` (DEP "não aplicável") até cadastrarem os fluxos.
- **API/UI**: `GET/POST /api/casos/{id}/fluxos`, `PUT/DELETE …/{fluxoId}` e a aba "Fluxos" entre Arestas e Mapa (US03).

### 12.3 Justificativa da aresta (DV-05, D11)

- `Aresta.Justificativa` obrigatória (`TextoValidado`: `trim`, ≤500), em criação e edição; vazia, só espaços ou longa demais → 422 (CT06). `w(e) ∉ {1,2,3}` continua 422 (CT05).
- **Migration** `AdicionaJustificativaDaAresta`: coluna `NOT NULL` com backfill do texto-padrão **"Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)"** (`Aresta.JustificativaPendente`); a tela marca essas arestas como pendentes. O mapeamento do EF não declara default, então toda aresta nova precisa informar a justificativa.
- **Contrato**: `CriarArestaRequest`, `AtualizarArestaRequest` e `ArestaDto` ganharam `justificativa` — mudança incompatível (front, `apiClient`, testes de API e E2E mudaram juntos).

### 12.4 Parâmetros editáveis e versionados (DV-06, DV-08, D12)

- **Parâmetros** = limites mín/máx por eixo (Quadro 6), pesos `p_i` e assinaturas dos Perfis (Quadro 7).
- **Desenho final** (difere da proposta inicial de um "ponteiro para a versão ativa"): o **estado vigente** continua em `ConfiguracoesDeEixo` e `PerfisArquiteturais`; **cada alteração** — `PUT /api/parametros/limites-e-pesos` ou criar/atualizar/remover um Perfil — grava, na **mesma transação**, um **snapshot imutável** em `VersoesDeParametros` (`Numero`, `CriadaEm`, `Descricao`, `SnapshotJson`, serializado por `SnapshotDeParametros`). A versão 1 nasce **sob demanda**, a partir do estado vigente, na primeira consulta ou alteração. Guardar versão é guardar **entrada**, compatível com RX02.
- **`IUnidadeDeTrabalho`** (Domain; implementação em `Sbar.Infrastructure/UnidadeDeTrabalho.cs`): abre a transação e toma um `pg_advisory_xact_lock`, de modo que a numeração das versões seja sequencial e sem colisão mesmo com chamadas concorrentes; chamadas aninhadas só participam da transação em curso.
- **`ConjuntoDeParametros`** (value object, Domain): `ValidarEixos` (os dez eixos, cada um uma vez; `mín < máx`; peso ≥ 0; Σ pesos = 1 com tolerância 1e-9 — `ParametrosInvalidosException`/`LimitesDeNormalizacaoInvalidosException`, 422) e `ValidarSomaDosPesos` (usado também no cálculo). `ServicoDeMetricas` obtém o conjunto de `IServicoDeParametros` (vigente ou `?versao=N`) e **bloqueia o cálculo** se Σ pesos ≠ 1 (RN07, CT10). `ILimitesDeNormalizacaoProvider`/`IPesosDeComparacaoProvider` deixaram de existir.
- **Rotas**: `GET /api/parametros` (lista), `GET /api/parametros/atual`, `GET /api/parametros/{numero}` (404 se inexistente), `PUT /api/parametros/limites-e-pesos`; `GET /metricas` e `GET /exportacao` aceitam `?versao=N`; as respostas informam `versaoDosParametros`.
- **Limites desta fase**: não há comparação automática entre versões; se alguém editar `ConfiguracoesDeEixo` direto no banco, o vigente passa a divergir da última versão (sem autocorreção).
- **Front (DV-08)**: tela "Parâmetros" (edição com Σ pesos em tempo real, histórico e visualização de versões) e seletor de versão em "Perfil mais próximo".

### 12.5 Comparação: contribuição por eixo, margem e vocabulário (DV-03, DV-04, D14)

- **Seed (DV-03)**: migration `AssinaturasDoQuadro8` (`UpdateData` nos `Id` fixos) trocou os dois perfis para o Quadro 7 — Monólito Modular 2,0/1,5/1,0/2,5/3,0/1,5/1,5/1,0/3,0/3,0 e Microsserviços 4,0/4,0/3,5/2,5/4,0/4,0/2,5/4,5/**2,0**/4,0 (ordem CO, ESC, INT, UX, DISP, FLEX, TR, AUT, VOL, DEP) — e os nomes para "(provisório)". Os vetores "reconstruídos" da Etapa 5 coincidiam, **exceto** em VOL de Microsserviços (4,2; o TCC publicou 2,0 — a outra raiz da ambiguidade).
- **Contribuição por eixo (RF10)**: `ComparadorDePerfis` devolve `ContribuicaoDeEixo { Eixo, Contribuicao }` = `p_i·(Norm_i^Serviço − Norm_i^Perfil)²` **sem arredondar** (como os Quadros 26/34), só eixos aplicáveis; `DistanciaPerfil` carrega `PercentualDoMaximo` (= distância ÷ 5 × 100) e `Contribuicoes`; `Recomendacao` carrega `Margem` (segunda menor − menor distância; `null` com menos de 2 perfis) e `DistanciaMaxima` (5). Não há limiar de "margem pequena": a API devolve os números e a tela os mostra.
- **Vocabulário (D14, opção 1 — padrão adotado)**: a interface diz "perfil de referência mais próximo" com o aviso fixo de que não substitui a análise do arquiteto (RN09); os identificadores de código e de API (`Recomendacao`, `GeradorDeRecomendacao`, `recomendacao`, `estiloRecomendado`) foram **mantidos**. Renomeá-los é mudança incompatível de contrato.

### 12.6 Matriz de rastreabilidade (DV-07, US07)

- O TCC (Quadros 28 e 36) traz uma matriz em parte **editorial** (a coluna "elemento do Blueprint/etapa" e os eixos de cada linha não saem de uma regra determinística). A decisão (item 30 da seção 10, ampliada) segue sendo **derivar as linhas das mesmas definições das calculadoras**, para que cada eixo liste os nós e arestas que de fato contribuíram: gargalos de **qualquer banda** (DISP para Backstage/Suporte; UX para Frontstage, com texto explicando que não entra no DISP; sem eixo para Ator/Externo), arestas CRI (TR), arestas externas (INT, com tipo e peso), **comunidades** (FLEX e AUT), **fluxos válidos** (DEP) e Touchpoints de grau de entrada acima da média (UX).
- `LinhaDeRastreabilidade` ganhou `Categoria`, `NosIds` e `ArestasIds`; a tela tem a visão "Por eixo" (US07). **Sem campo de etapa do Blueprint**: `Elemento = "Nome (Banda)"`. CO gera uma linha por Banda ocupada, ESC uma por Banda com ao menos dois nós e VOL uma por aresta.
- **Inconsistência do TCC registrada**: o Quadro 36 liga TP2 a "DISP, UX", mas 7.4 diz que TP2 (Frontstage) não entra no DISP; o protótipo segue a fórmula (`duvidas-pendentes.md` item 15).

### 12.7 Exportação (RF13, D13)

`GET /api/casos/{id}/exportacao?formato=json|csv[&versao=N]` (`ExportacaoController`, `MontadorDeExportacao`, `ExportadorDeCsv` em `Sbar.Api/Exportacao/`). O conteúdo é o registro que a seção 4.6.1 do TCC manda guardar: caso, versão e parâmetros, nós, arestas (com justificativas e pesos), fluxos, comunidades e Q, avisos, bandas, eixos (brutos e normalizados, com "não aplicável"), distâncias com contribuições, margem e a matriz. JSON completo ou CSV (`;`, UTF-8 com BOM, ponto decimal, seções `# Nome`); `Content-Disposition` `sbar-{slug}-v{n}.{ext}`; 400 para formato inválido, 404 para caso/versão inexistente, 422 se Σ pesos ≠ 1. Gerado na hora; nada é persistido. PDF fora do escopo.

### 12.8 Testes e documentação (DV-10)

- **Oráculos** (`EstudosDeCasoDeReferencia` e os testes que o usam, de backend e front) passaram aos valores dos Quadros 25/27 (CE1) e 35/37 (CE2) de `docs/dominio/estudos-de-caso.md` — CE1: FLEX 3,50, AUT 1,67, DEP 2,50, distâncias 1,051/1,605; CE2: FLEX 2,76, AUT 2,50, DEP 3,75, distâncias 1,107/1,346 (CO, ESC, INT, UX, DISP, TR e VOL não mudam com a revisão). CT01–CT10 são rastreados por `[Trait("Requisito", "CTxx")]` no backend e comentários `// CTxx` no front (mapeamento definitivo após a auditoria dos testes). O E2E (Playwright) foi atualizado em 2026-10-25.
- **Comentários de código** que citavam Quadros pela numeração antiga foram corrigidos (sub-etapa 9.0).
- **Regra da dependência mantida**: `Sbar.Domain` continua sem EF Core/ASP.NET; o CNM, a avaliação dos fluxos e as contribuições ficam como funções puras (em `Sbar.Application`, e a avaliação do fluxo no agregado); entram em `Sbar.Domain` só entidades, invariantes e portas novas (`Fluxo`, `Justificativa`, `ConjuntoDeParametros`, `VersaoDeParametros`, `IUnidadeDeTrabalho`).

### 12.9 O que ficou de fora (conscientemente)

Comparação automática entre versões de parâmetros (D12); campo de **etapa do Blueprint** no Nó; "sugerir fluxos" a partir do grafo; campo `Código` do Nó como alternativa robusta de desempate (D9); PDF na exportação; renomear a API de "recomendação" (D14); layout mobile; a análise de sensibilidade do TCC reproduzida pelo sistema; a tag de versão (POP-08). Decisões D9–D14 aguardam confirmação do usuário.
