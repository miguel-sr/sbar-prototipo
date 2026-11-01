import { useState } from 'react';
import { Link } from 'react-router';
import { useCaso } from '../componentes/CasoLayout';
import { AvisosDoMapa } from '../componentes/AvisosDoMapa';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { MetricaCard } from '../componentes/MetricaCard';
import { PainelDeComunidades } from '../componentes/PainelDeComunidades';
import { CORES_DOS_PERFIS, RadarChart } from '../componentes/RadarChart';
import { ResumoDeFluxos } from '../componentes/ResumoDeFluxos';
import { TabelaDeBandas } from '../componentes/TabelaDeBandas';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';

// T6 — MetricasRadar: os dez eixos e o Radar Arquitetural. GET /api/casos/{id}/metricas
// (recalculado pela API a cada abertura — RF-11) e GET /api/perfis-arquiteturais (sobreposição).
// Abaixo dos cartões, a estrutura que explica FLEX/AUT/DISP/DEP: comunidades e Q (RF06), μ/σ por
// Banda (US04) e os fluxos principais cadastrados (US03).
export function MetricasRadar() {
  const { caso } = useCaso();
  const chave = `${caso.id}:${caso.nos.length}:${caso.arestas.length}`;
  const metricas = useCarregar(() => apiClient.obterMetricas(caso.id), [chave]);
  const perfis = useCarregar(() => apiClient.listarPerfis());
  const [ocultos, setOcultos] = useState<Set<string>>(new Set());

  if (caso.nos.length === 0) {
    return (
      <EstadoVazio titulo="Sem Mapa, sem métricas">
        <Link to="../nos">Cadastre os Nós</Link> e as Arestas para calcular o Radar.
      </EstadoVazio>
    );
  }
  if (metricas.carregando && !metricas.dados) return <Carregando texto="Calculando os dez eixos…" />;
  if (!metricas.dados) return <MensagemDeErro erro={metricas.erro} aoTentarDeNovo={metricas.recarregar} />;

  const listaDePerfis = perfis.dados ?? [];
  const { avisos = [], comunidades = [], modularidadeQ = null, bandas = [], fluxos = [], nosGargalo = [] } = metricas.dados;
  const visiveis = listaDePerfis.filter((p) => !ocultos.has(p.id));
  const alternar = (id: string) =>
    setOcultos((atual) => {
      const novo = new Set(atual);
      if (novo.has(id)) novo.delete(id);
      else novo.add(id);
      return novo;
    });

  return (
    <div className="pilha">
      <div className="cabecalho-secao">
        <div>
          <h2 className="titulo-secao">Radar Arquitetural</h2>
          <p className="texto-suave">Dez eixos na escala 0–5, calculados a partir do Mapa atual.</p>
        </div>
        <Link to="../recomendacao" className="botao botao--primario botao--pequeno">
          Ver perfil mais próximo →
        </Link>
      </div>

      <AvisosDoMapa avisos={avisos} />

      <div className="cartao radar-cartao">
        <RadarChart eixos={metricas.dados.eixos} perfis={visiveis} />
        <div className="radar-legenda">
          <span className="radar-legenda__item">
            <span className="amostra amostra--servico" aria-hidden="true" />
            Serviço (este Estudo de Caso)
          </span>
          {listaDePerfis.map((p, i) => (
            <label key={p.id} className="radar-legenda__item radar-legenda__alternavel">
              <input type="checkbox" checked={!ocultos.has(p.id)} onChange={() => alternar(p.id)} />
              <span className="amostra amostra--perfil" style={{ borderColor: CORES_DOS_PERFIS[i % CORES_DOS_PERFIS.length] }} aria-hidden="true" />
              {p.nome}
            </label>
          ))}
          {perfis.dados && listaDePerfis.length === 0 && (
            <span className="texto-suave">
              Nenhum Perfil Arquitetural cadastrado — <Link to="/perfis">cadastrar</Link>.
            </span>
          )}
        </div>
      </div>

      <div className="grade-metricas">
        {metricas.dados.eixos.map((resultado) => (
          <MetricaCard key={resultado.eixo} resultado={resultado} />
        ))}
      </div>

      <h3 className="titulo-secao">Estrutura do Mapa por trás dos eixos</h3>
      <PainelDeComunidades comunidades={comunidades} modularidadeQ={modularidadeQ} nos={caso.nos} />
      <TabelaDeBandas bandas={bandas} nos={caso.nos} nosGargalo={nosGargalo} />
      <ResumoDeFluxos fluxos={fluxos} />
    </div>
  );
}
