import type { Eixo } from '../tipos/Metricas';

// Texto de exibição dos dez eixos do Radar. Nome e descrição espelham o [Description] do enum
// Eixo no backend (backend/src/Sbar.Domain/Enums/Eixo.cs); a fórmula espelha
// docs/dominio/formulas-metricas.md (TCC revisado, seção 4.4.2; comunidades em 4.4.1). Aqui só há TEXTO para o "ver cálculo":
// o front nunca calcula nenhum eixo — os números vêm sempre de GET /api/casos/{id}/metricas.
// Se uma fórmula mudar no TCC, atualizar formulas-metricas.md, a calculadora e este texto.

export interface InfoDoEixo {
  nome: string;
  avalia: string;
  formula: string;
  /** Aviso de limitação conhecida do método, quando existe. */
  limitacao?: string;
}

export const INFO_DO_EIXO: Record<Eixo, InfoDoEixo> = {
  CO: {
    nome: 'Complexidade Operacional',
    avalia:
      'O tamanho e a dispersão do Mapa (número de Nós e de Bandas ocupadas). Ajuda a perceber quando o sistema já tem peças suficientes para compensar o custo de modularizar.',
    formula: 'CO = N + 2 × |bandas ocupadas|',
  },
  ESC: {
    nome: 'Escalabilidade',
    avalia:
      'A independência entre os Nós de uma mesma Banda. Um valor alto aponta partes que podem escalar ou evoluir isoladamente.',
    formula: 'ESC = Σ_b [C(n_b, 2) − arestas_intra(b)] / Σ_b C(n_b, 2)',
  },
  INT: {
    nome: 'Integração',
    avalia:
      'O quanto a comunicação do sistema depende de Integrações Externas. Um valor alto expõe o sistema à instabilidade de terceiros.',
    formula: 'INT = |E_Externo| / M',
  },
  UX: {
    nome: 'Experiência do Usuário',
    avalia:
      'Quanta interação converge, em média, para cada Touchpoint. Um valor alto identifica pontos que concentram orquestração.',
    formula: 'UX = Σ grau⁻(v) / n_Frontstage   (v ∈ Frontstage)',
  },
  DISP: {
    nome: 'Disponibilidade',
    avalia:
      'A proporção de Nós internos (Backstage/Suporte) que não são Gargalo. Um valor baixo sinaliza pontos únicos de falha.',
    formula: 'DISP = 1 − Σ gargalo(v) / (n_Backstage + n_Suporte)',
  },
  FLEX: {
    nome: 'Flexibilidade/Evolução',
    avalia:
      'O quanto os Nós internos se comunicam só dentro da própria comunidade estrutural (grupo de Nós mais conectados entre si, detectado por modularidade — TCC 4.4.1). Um valor alto indica módulos que evoluem sem propagar mudança.',
    formula: 'FLEX = Σ isolamento(v) / (n_Backstage + n_Suporte),  isolamento(v) = 1 − arestas_para_outras_comunidades(v) / grau_total(v)',
    limitacao:
      'Depende da partição em comunidades: se o Mapa cair numa comunidade só (ou não tiver arestas), FLEX fica em 1,00 — veja o aviso e o valor de Q abaixo dos cartões.',
  },
  TR: {
    nome: 'Tempo Real',
    avalia:
      'A proporção de comunicação classificada como Crítica. Um valor alto exige baixa latência e acoplamento síncrono.',
    formula: 'TR = Σ w(e) das arestas CRI / Σ w(e) de todas as arestas',
  },
  AUT: {
    nome: 'Autonomia dos Módulos',
    avalia:
      'Quantas comunidades estruturais dependem de no máximo uma aresta de saída para outras comunidades. Um valor alto indica módulos baratos de extrair.',
    formula: 'AUT = |{c ∈ C : saídas_externas(c) ≤ 1}| / |C|   (C = comunidades, TCC 4.4.1)',
    limitacao:
      'Com poucas comunidades, AUT só assume poucos valores (múltiplos de 1/|C|); com uma única comunidade ele fica em 1,00.',
  },
  VOL: {
    nome: 'Volume de Comunicação',
    avalia:
      'A intensidade média de cada interação (Peso por Aresta). Um valor alto sinaliza tráfego pesado entre as partes.',
    formula: 'VOL = Σ w(e) / M',
  },
  DEP: {
    nome: 'Dependência Organizacional',
    avalia:
      'Quantas Bandas distintas, em média, os fluxos principais que você cadastrou (aba Fluxos) atravessam, contando origem e destino. Um valor alto sugere processos que cruzam várias responsabilidades (Lei de Conway). Sem fluxos válidos, o eixo fica “não aplicável”.',
    formula: 'DEP = [ Σ bandas_percorridas(f) / |F| ] / 5   (F = fluxos principais cadastrados e válidos)',
  },
};

export const FORMULA_DE_NORMALIZACAO = 'Norm = 5 × (bruto − mín) / (máx − mín), limitado a [0, 5]';
