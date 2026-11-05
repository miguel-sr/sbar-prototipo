import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { CasoLayout } from '../componentes/CasoLayout';
import { Fluxos } from '../paginas/Fluxos';
import { MapaArestas } from '../paginas/MapaArestas';
import { MapaNos } from '../paginas/MapaNos';
import { MapaVisual } from '../paginas/MapaVisual';
import { MatrizRastreabilidade } from '../paginas/MatrizRastreabilidade';
import { MetricasRadar } from '../paginas/MetricasRadar';
import { Recomendacao } from '../paginas/Recomendacao';
import type { Aresta, Peso, TipoDeAresta } from '../tipos/Aresta';
import type { EstudoDeCaso } from '../tipos/EstudoDeCaso';
import type { Fluxo } from '../tipos/Fluxo';
import { EIXOS, type ContribuicaoDoEixo, type DistanciaPerfil, type Eixo, type Recomendacao as RecomendacaoDaApi, type ResultadoDeMetricas } from '../tipos/Metricas';
import type { Banda, No, TipoDeNo } from '../tipos/No';
import type { ConjuntoDeParametros, VersaoDeParametros } from '../tipos/Parametros';
import type { PerfilArquitetural } from '../tipos/PerfilArquitetural';
import type { CategoriaDaMatriz, LinhaDeRastreabilidade } from '../tipos/Rastreabilidade';

// Auxiliares dos testes de componente: fábricas de dados e renderização com as rotas reais.
// A API falsa (vi.mock do apiClient) vive em ./apiFalsa.ts, separada de propósito (ciclo de importação).

export const ID_DO_CASO = 'a1b2c3d4-0000-4000-8000-000000000001';

// Só dado de teste: a Banda real é sempre derivada pelo backend.
const BANDA_DO_TIPO: Record<TipoDeNo, Banda> = {
  Ator: 'Ator',
  Touchpoint: 'Frontstage',
  Processo: 'Backstage',
  Suporte: 'Suporte',
  Dados: 'Suporte',
  Externo: 'Externo',
};

export function fabricaNo(id: string, nome = id, tipo: TipoDeNo = 'Processo'): No {
  return { id, nome, tipo, banda: BANDA_DO_TIPO[tipo] };
}

export function fabricaAresta(
  id: string,
  origem: string,
  destino: string,
  tipo: TipoDeAresta = 'Sincrona',
  peso: Peso = 2,
  justificativa = 'Interação do Blueprint (teste)',
): Aresta {
  return { id, noOrigemId: origem, noDestinoId: destino, tipo, peso, justificativa };
}

/** Fluxo principal válido por padrão; passe `valido: false` e `etapaAusente` para simular um fluxo que perdeu uma Aresta. */
export function fabricaFluxo(id: string, nome: string, nosIds: string[], sobrescrever: Partial<Fluxo> = {}): Fluxo {
  return { id, nome, nosIds, valido: true, etapaAusente: null, bandasPercorridas: 3, ...sobrescrever };
}

export function fabricaCaso(sobrescrever: Partial<EstudoDeCaso> = {}): EstudoDeCaso {
  return {
    id: ID_DO_CASO,
    nome: 'Caso de teste',
    dominioDescricao: null,
    dataCriacao: '2026-10-07T12:00:00Z',
    nos: [],
    arestas: [],
    ...sobrescrever,
  };
}

/** Métricas com os dez eixos aplicáveis (valor 2,5 normalizado), sem recomendação, gargalo, comunidade, aviso nem fluxo. */
export function fabricaMetricas(sobrescrever: Partial<ResultadoDeMetricas> = {}): ResultadoDeMetricas {
  return {
    eixos: EIXOS.map((eixo) => ({ eixo, aplicavel: true, valorBruto: 0.5, valorNormalizado: 2.5, motivo: null })),
    recomendacao: null,
    nosGargalo: [],
    comunidades: [],
    modularidadeQ: null,
    avisos: [],
    bandas: [],
    fluxos: [],
    versaoDosParametros: fabricaVersao(1),
    ...sobrescrever,
  };
}

/** Versão de parâmetros de teste (data fixa). */
export function fabricaVersao(numero: number, descricao = `Versão ${numero} de teste`): VersaoDeParametros {
  return { numero, criadaEm: '2026-10-21T12:00:00Z', descricao };
}

/** Distância até um perfil, com percentual do máximo (5) e contribuições por eixo (só as informadas). */
export function fabricaDistancia(
  perfilId: string,
  perfilNome: string,
  distancia: number,
  contribuicoes: Partial<Record<Eixo, number>> = {},
): DistanciaPerfil {
  return {
    perfilId,
    perfilNome,
    distancia,
    percentualDoMaximo: (distancia / 5) * 100,
    contribuicoes: Object.entries(contribuicoes).map(([eixo, contribuicao]): ContribuicaoDoEixo => ({ eixo: eixo as Eixo, contribuicao })),
  };
}

/** Recomendação (perfil mais próximo) com a margem calculada a partir das duas menores distâncias. */
export function fabricaRecomendacao(estiloRecomendado: string, distancias: DistanciaPerfil[]): RecomendacaoDaApi {
  const ordenadas = [...distancias].sort((a, b) => a.distancia - b.distancia);
  return {
    estiloRecomendado,
    distancias,
    margem: ordenadas.length > 1 ? ordenadas[1].distancia - ordenadas[0].distancia : null,
    distanciaMaxima: 5,
    pesosEfetivos: [],
    eixosOmitidos: [],
  };
}

/** Conjunto de parâmetros: limites do Quadro 6 e pesos iguais (0,1), mais as assinaturas informadas. */
export function fabricaConjunto(numero = 1, descricao = 'Parâmetros iniciais', sobrescrever: Partial<ConjuntoDeParametros> = {}): ConjuntoDeParametros {
  const limites: Record<Eixo, [number, number]> = {
    CO: [6, 40], ESC: [0, 1], INT: [0, 1], UX: [0, 3], DISP: [0, 1], FLEX: [0, 1], TR: [0, 1], AUT: [0, 1], VOL: [1, 3], DEP: [0.2, 1],
  };
  return {
    versao: fabricaVersao(numero, descricao),
    eixos: EIXOS.map((eixo) => ({ eixo, minimo: limites[eixo][0], maximo: limites[eixo][1], peso: 0.1 })),
    assinaturas: [],
    ...sobrescrever,
  };
}

/** Linha da matriz de rastreabilidade (categoria padrão: gargalo). */
export function fabricaLinhaDaMatriz(
  noOuAresta: string,
  categoria: CategoriaDaMatriz,
  eixos: Eixo[],
  sobrescrever: Partial<LinhaDeRastreabilidade> = {},
): LinhaDeRastreabilidade {
  return { elemento: noOuAresta, noOuAresta, metrica: 'métrica', eixos, contribuicao: 'contribuição', categoria, nosIds: [], arestasIds: [], ...sobrescrever };
}

export function fabricaPerfil(id: string, nome: string, valor = 2): PerfilArquitetural {
  return {
    id, nome,
    valorCO: valor, valorESC: valor, valorINT: valor, valorUX: valor, valorDISP: valor,
    valorFLEX: valor, valorTR: valor, valorAUT: valor, valorVOL: valor, valorDEP: valor,
  };
}

/** Renderiza um elemento dentro de um MemoryRouter (para páginas que não dependem do Estudo de Caso). */
export function renderizarComRota(ui: ReactElement, caminho = '/', rotas: Record<string, ReactElement> = {}) {
  return render(
    <MemoryRouter initialEntries={[caminho]}>
      <Routes>
        <Route path={caminho} element={ui} />
        {Object.entries(rotas).map(([path, el]) => (
          <Route key={path} path={path} element={el} />
        ))}
      </Routes>
    </MemoryRouter>,
  );
}

/** Renderiza as rotas reais do Estudo de Caso (CasoLayout + telas T3–T8, incluindo Fluxos) e uma página inicial falsa. */
export function renderizarCaso(caminho = `/casos/${ID_DO_CASO}/nos`) {
  return render(
    <MemoryRouter initialEntries={[caminho]}>
      <Routes>
        <Route path="/" element={<p>Página inicial</p>} />
        <Route path="/casos/:casoId" element={<CasoLayout />}>
          <Route path="nos" element={<MapaNos />} />
          <Route path="arestas" element={<MapaArestas />} />
          <Route path="fluxos" element={<Fluxos />} />
          <Route path="mapa" element={<MapaVisual />} />
          <Route path="radar" element={<MetricasRadar />} />
          <Route path="recomendacao" element={<Recomendacao />} />
          <Route path="rastreabilidade" element={<MatrizRastreabilidade />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}
