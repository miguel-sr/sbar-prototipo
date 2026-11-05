# Glossário do Método SBAR

> Fonte: `docs/fontes/GLOSSARIO.pdf` (base, anterior à revisão do TCC), atualizado conforme o TCC revisado (`docs/fontes/TCC.pdf`, 2026-10-21) — códigos de tipo de aresta e regra de prevalência de CRI sobre ASY (seção 4.2.4), comunidades por modularidade (4.4.1), assinaturas de referência (Quadro 7, 4.6) e requisitos do protótipo (5.3). Este arquivo é a fonte única de conteúdo para o ícone de ajuda flutuante do protótipo (`AjudaFlutuante.tsx`, ver POP-09 seção 8.5) e é importado como texto pelo front: **mantenha os títulos `##` e `###` existentes**, pois a ajuda contextual aponta para eles. Qualquer atualização de termo deve ser feita aqui primeiro. Termos marcados como "previsto" existem no TCC revisado, mas ainda não nesta versão do protótipo.

## Nó

Um elemento do fluxo de negócio: uma tela, um processo interno, um banco de dados, um serviço de fora. Todo nó tem um **tipo** e pertence a **exatamente uma banda**.

### Tipos de nó

| Tipo | Descrição |
|---|---|
| **Ator** | Quem inicia o fluxo — normalmente o cliente. Não é implementado como componente de software, apenas referenciado. |
| **Touchpoint** | Algo que o cliente vê e usa diretamente (uma tela, um botão, uma notificação recebida). |
| **Processo** | Algo que acontece "por trás dos panos", que o cliente não vê, específico deste fluxo. |
| **Suporte** | Um processo que várias partes do sistema usam (ex.: autenticação, envio de notificações) — não é exclusivo de um fluxo só. |
| **Dados** | Onde as informações ficam guardadas (banco de dados, cadastro, histórico). |
| **Externo** | Um serviço de fora do sistema (gateway de pagamento, IA, envio de SMS/e-mail). |

## Banda

O "grupo" a que um nó pertence — decidido **automaticamente** pelo tipo dele, nunca escolhido à mão:

| Tipo de nó | Banda |
|---|---|
| Ator | Ator |
| Touchpoint | Frontstage |
| Processo | Backstage |
| Suporte | Suporte |
| Dados | Suporte |
| Externo | Externo |

A banda **Suporte** agrega dois tipos de nó distintos (Suporte e Dados) — eles compartilham a banda visual, mas continuam semanticamente diferentes (um é processo, o outro é persistência).

Uma banda é **ocupada** quando tem pelo menos um nó; o número de bandas ocupadas entra no eixo Complexidade Operacional (CO).

## Aresta

Uma ligação com direção entre dois nós — "isso leva a isso". Toda aresta tem um **tipo**, um **peso** e deve ter uma **justificativa** que a ligue ao Service Blueprint.

### Tipos de aresta

| Código | Nome | Critério |
|---|---|---|
| **SYN** | Síncrona | O cliente/etapa seguinte fica esperando a resposta do processamento; é rápido. |
| **CRI** | Crítica / tempo real | O cliente fica esperando e um atraso aqui seria grave (ex.: pagamento, check-in). Prevalece sobre ASY quando a interação tem ambas as características. |
| **ASY** | Assíncrona / evento | Acontece nos bastidores, com desacoplamento temporal (notificação, mensageria, callback, webhook). |
| **BAT** | Eventual / batch | Roda de tempos em tempos, programado (ex.: um lembrete diário, conciliação, relatório). Tolera atraso operacional. |

A classificação considera o **comportamento descrito no serviço**, nunca uma tecnologia presumida.

### Peso da ligação

Quanto a etapa "pesa" no fluxo — escala discreta `w(e) ∈ {1, 2, 3}`:

| Peso | Nome | Regra de decisão |
|---|---|---|
| **3** | Alto | Acontece em 100% das instâncias do fluxo, é caminho obrigatório e sequencial — sem ela o processo não anda. |
| **2** | Médio | Recorrente, mas com função de apoio (resposta, leitura/escrita auxiliar, etapa que não bloqueia o avanço do fluxo principal). |
| **1** | Baixo | Condicional, ocorre só num subtipo do fluxo, ou é periódica/batch. |

## Justificativa da aresta

Uma frase que explica **de qual interação do Service Blueprint** a aresta vem (ou a inferência feita para criá-la). Toda aresta precisa dela: é o que mantém o Mapa rastreável ao Blueprint e evita ligações criadas só para "fechar" o desenho. *Obrigatória no protótipo (RN03): a API e o formulário rejeitam a aresta sem justificativa. As arestas cadastradas antes dessa exigência aparecem como "pendente de revisão".*

## Carga de um nó

`carga(v) = soma dos pesos w(e) de todas as arestas incidentes a v` (entrada + saída). Quanto mais alta, mais aquele ponto concentra atividade do sistema.

## Gargalo (ponto potencialmente crítico)

Um nó vira gargalo quando sua carga é significativamente maior que a média dos outros nós **da mesma banda**:

```
carga(v) > μ_banda + σ_banda
```

onde μ e σ são calculados sobre as cargas de todos os nós daquela banda, e a carga precisa ser **estritamente maior** que o limiar. É um **indicador estrutural potencial**, não uma comprovação de ponto único de falha em produção. Observação: numa banda com apenas 2 nós o critério nunca aponta gargalo (μ+σ é igual à maior carga); e só os gargalos de Backstage e Suporte entram no eixo Disponibilidade.

## Comunidade estrutural

Um grupo de nós que se ligam mais entre si do que com o resto do Mapa — pense em "bairros" do sistema. O SBAR descobre as comunidades automaticamente, olhando as ligações sem considerar a direção e somando os pesos das arestas entre cada par de nós (método guloso de Clauset-Newman-Moore, que busca a divisão de maior **modularidade**). As comunidades alimentam os eixos Flexibilidade/Evolução (FLEX) e Autonomia dos Módulos (AUT). O TCC também as chama de *agrupamento* ou *cluster*. Com empates, o algoritmo pode dividir de formas diferentes; por isso a divisão usada deve ser registrada junto ao resultado. *Previsto no TCC revisado; esta versão do protótipo ainda usa a definição antiga de agrupamento (conectividade fraca).*

## Modularidade (Q)

Nota que mede o quão bem uma divisão do Mapa em comunidades separa grupos densos de grupos pouco ligados. Valores acima de aproximadamente **0,3** indicam estrutura de comunidades significativa; valores baixos indicam que o Mapa tem pouca estrutura de grupos (em Mapas muito pequenos ou muito centralizados isso é comum). O valor de Q e a divisão em comunidades devem acompanhar os resultados de cada análise. Sem nenhuma aresta, Q não se aplica. *Previsto no TCC revisado (RF06); ainda não exibido nesta versão do protótipo.*

## Isolamento

Para um nó interno (Backstage ou Suporte), é a fração das suas interações que ficam **dentro da própria comunidade**: `isolamento(v) = 1 − arestas para outras comunidades / total de arestas do nó` (vale 1 se o nó não tem arestas). O eixo FLEX é a média do isolamento dos nós internos: quanto mais isolados, maior o potencial de evoluírem de forma independente.

## Saídas externas

Arestas que **saem** de nós de uma comunidade em direção a nós de outra comunidade. Uma comunidade com no máximo uma saída externa é considerada **autônoma**; o eixo AUT é a proporção de comunidades autônomas.

## Fluxo principal

Um caminho de ponta a ponta que o serviço percorre — uma sequência de nós em que cada passo tem uma aresta ligando ao seguinte (ex.: *Ator → Cardápio → Carrinho → Validação → …*). Os fluxos principais são **definidos por quem modela** o serviço, e cada um precisa ter arestas correspondentes no Mapa. O eixo Dependência Organizacional (DEP) usa a média de bandas diferentes que esses fluxos atravessam. Um fluxo pode passar duas vezes pelo mesmo nó, e nem todo fluxo começa no Ator. *Previsto no TCC revisado (RF05); esta versão do protótipo ainda deriva os fluxos automaticamente do Mapa.*

## Mapa válido

Um Mapa em que todas as etapas do Service Blueprint estão representadas: cada fluxo principal tem arestas para todas as suas etapas, sem nós soltos nem ligações criadas só para completar o desenho. É o resultado do último passo de construção do Mapa (validação da malha).

## Radar Arquitetural

Um gráfico com 10 eixos (métricas) que resumem o comportamento do fluxo — cada eixo normalizado numa escala de 0 a 5. A escala mostra a **intensidade relativa** de cada característica no serviço; ela não diz que um valor é "melhor" ou "pior". Os dez eixos têm peso equivalente na representação visual.

## Perfil Arquitetural

Um "modelo de referência" (ex.: Monólito Modular, Microsserviços) com valores esperados para os mesmos 10 eixos do Radar — serve para comparar o fluxo analisado com estilos conhecidos de arquitetura. No TCC é chamado de **assinatura** ou **perfil de referência**. Os valores atuais (Quadro 7 do TCC) são **provisórios**: foram definidos pelos pesquisadores com base nas características usualmente associadas a cada estilo, não resultam de medição, e ainda serão validados por especialistas. Podem ser editados.

## Recomendação

O resultado da comparação entre o perfil do serviço e os perfis de referência: qual estilo de arquitetura mais se parece com o que foi desenhado (menor distância euclidiana ponderada), e por quê. No TCC revisado é descrito como o **perfil de referência mais próximo** e funciona como um **subsídio à decisão**: uma indicação fundamentada e rastreável, nunca uma decisão tomada pelo método — não substitui o julgamento do arquiteto. Distâncias pequenas entre os estilos indicam um serviço intermediário, em que a comparação pouco os diferencia. A distância máxima possível é 5, então cada distância também pode ser lida como percentual desse máximo.

## Subsídio à decisão

Nome que o TCC revisado dá ao que o SBAR entrega: informação estruturada e rastreável para o arquiteto decidir, e não uma escolha automática. O resultado precisa ser lido junto com o contexto do sistema, seus requisitos, custos, competências da equipe e restrições que o Mapa não representa.

## Versão dos parâmetros

Os limites de normalização, os pesos dos eixos e as assinaturas de referência usados numa análise. Registrar a versão permite reproduzir o resultado e comparar análises feitas com parâmetros diferentes. *Disponível no protótipo (RF11): cada alteração de limites, pesos ou assinaturas gera uma nova versão, e cada análise informa a versão usada; é possível reaplicar uma versão antiga.*

## Matriz de Rastreabilidade

Uma tabela que mostra o caminho completo de uma decisão: elemento do Service Blueprint → nó/aresta do Mapa → métrica → eixo do Radar → contribuição para a comparação com os perfis. Serve para explicar "por que o sistema sugeriu isso" a qualquer momento. No TCC revisado, uma linha pode alimentar mais de um eixo e pode representar também uma comunidade (eixos FLEX e AUT).
