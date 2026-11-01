import { useMemo, useState } from 'react';
import { Link } from 'react-router';
import { useCaso } from '../componentes/CasoLayout';
import { EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { MapaSvg } from '../componentes/MapaSvg';
import { formatarNumero } from '../dominio/rotulos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';

// T5 — MapaVisual: desenho do Mapa de Interações. GET /api/casos/{id} (já no contexto) e
// GET /api/casos/{id}/metricas (campo nosGargalo: quais Nós são gargalo, em qualquer Banda).
export function MapaVisual() {
  const { caso } = useCaso();
  const chave = `${caso.id}:${caso.nos.length}:${caso.arestas.length}`;
  const metricas = useCarregar(() => apiClient.obterMetricas(caso.id), [chave]);
  const [mostrarComunidades, setMostrarComunidades] = useState(false);
  const comunidadePorNo = useMemo(() => {
    const mapa = new Map<string, number>();
    for (const c of metricas.dados?.comunidades ?? []) for (const id of c.nosIds) mapa.set(id, c.indice);
    return mapa;
  }, [metricas.dados]);

  if (caso.nos.length === 0) {
    return (
      <EstadoVazio titulo="O Mapa ainda está vazio">
        <Link to="../nos">Cadastre os Nós</Link> e depois as Arestas para ver o desenho.
      </EstadoVazio>
    );
  }

  const gargalos = new Set(metricas.dados?.nosGargalo ?? []);
  const nomesDosGargalos = caso.nos.filter((n) => gargalos.has(n.id)).map((n) => n.nome);

  return (
    <div className="pilha">
      <div className="cabecalho-secao">
        <div>
          <h2 className="titulo-secao">Mapa de Interações</h2>
          <p className="texto-suave">
            Cada faixa é uma Banda. Passe o mouse sobre um Nó para destacar as ligações dele.
          </p>
        </div>
        <Link to="../radar" className="botao botao--primario botao--pequeno">
          Calcular métricas →
        </Link>
      </div>
      {metricas.erro !== undefined && <MensagemDeErro erro={metricas.erro} aoTentarDeNovo={metricas.recarregar} />}
      {comunidadePorNo.size > 0 && (
        <label className="alternador">
          <input type="checkbox" checked={mostrarComunidades} onChange={(e) => setMostrarComunidades(e.target.checked)} />
          Mostrar comunidades (C1…C{metricas.dados?.comunidades.length})
        </label>
      )}
      <MapaSvg nos={caso.nos} arestas={caso.arestas} gargalos={gargalos} comunidadePorNo={mostrarComunidades ? comunidadePorNo : undefined} />
      {metricas.dados && (
        <p className="nota">
          {nomesDosGargalos.length === 0
            ? 'Nenhum gargalo identificado.'
            : `Gargalos (carga acima de μ + σ da própria Banda): ${nomesDosGargalos.join(', ')}.`}
          {mostrarComunidades && metricas.dados.modularidadeQ !== null && ` Modularidade Q = ${formatarNumero(metricas.dados.modularidadeQ, 4)}.`}
        </p>
      )}
    </div>
  );
}
