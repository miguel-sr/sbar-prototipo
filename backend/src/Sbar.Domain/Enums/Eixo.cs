using System.ComponentModel;

namespace Sbar.Domain.Enums;

// Os dez eixos do Radar Arquitetural — TCC seção 4.4.2. O [Description] de cada valor traz o
// nome completo, o que a métrica avalia e o benefício prático de olhar para ela na hora de
// decidir entre Monólito Modular e Microsserviços — útil pra telas/relatórios que precisam do
// rótulo por extenso, sem duplicar essa tabela em outro lugar do código (ver EixoExtensions.ObterDescricao()).
public enum Eixo
{
    [Description("Complexidade Operacional — avalia o tamanho e a dispersão do Mapa (número de Nós e de Bandas ocupadas); ajuda a perceber quando o sistema já tem peças suficientes para compensar o custo de modularizar.")]
    CO,

    [Description("Escalabilidade — avalia a independência entre os Nós de uma mesma Banda; um valor alto aponta partes que podem escalar ou evoluir isoladamente, boas candidatas a virar serviços separados.")]
    ESC,

    [Description("Integração — avalia o quanto a comunicação do sistema depende de Integrações Externas; um valor alto expõe o sistema à instabilidade de terceiros, favorecendo isolar essas dependências num serviço próprio.")]
    INT,

    [Description("Experiência do Usuário — avalia quanta interação converge, em média, para cada Touchpoint; um valor alto identifica pontos que concentram orquestração e podem precisar de um gateway dedicado.")]
    UX,

    [Description("Disponibilidade — avalia a proporção de Nós internos (Backstage/Suporte) que não são Gargalo; um valor baixo sinaliza pontos únicos de falha que se beneficiariam de isolamento ou replicação independente.")]
    DISP,

    [Description("Flexibilidade/Evolução — avalia o quanto os Nós internos se comunicam só dentro da própria comunidade estrutural (detecção de comunidades por modularidade, TCC 4.4.1); um valor alto indica módulos que podem evoluir sem propagar mudança para o resto do sistema.")]
    FLEX,

    [Description("Tempo Real — avalia a proporção de comunicação classificada como Crítica; um valor alto exige baixa latência e acoplamento síncrono, o que costuma desaconselhar fragmentar essa parte em serviços remotos.")]
    TR,

    [Description("Autonomia dos Módulos — avalia a proporção de comunidades estruturais que dependem de no máximo uma interação de saída para outras comunidades; um valor alto indica módulos que já se comportam como unidades independentes, baratas de extrair.")]
    AUT,

    [Description("Volume de Comunicação — avalia a intensidade média de cada interação (Peso por Aresta); um valor alto sinaliza tráfego pesado entre partes, exigindo infraestrutura de mensageria robusta antes de separá-las.")]
    VOL,

    [Description("Dependência Organizacional — avalia quantas Bandas distintas um fluxo de negócio típico atravessa; um valor alto sugere processos que cruzam várias responsabilidades organizacionais, onde separar por equipe tende a reduzir esse acoplamento (Lei de Conway).")]
    DEP
}
