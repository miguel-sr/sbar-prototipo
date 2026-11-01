import { useState } from 'react';
import { Link } from 'react-router';
import { BotaoDeIcone } from '../componentes/BotaoDeIcone';
import { useCaso } from '../componentes/CasoLayout';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { ExclusaoComConfirmacao } from '../componentes/ExclusaoComConfirmacao';
import { FluxoForm } from '../componentes/FluxoForm';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';
import type { CriarFluxoRequest, Fluxo } from '../tipos/Fluxo';

// T4b — Fluxos: cadastro dos fluxos principais F (TCC 4.2.7, RF05), entrada do eixo DEP.
// GET/POST/PUT/DELETE /api/casos/{id}/fluxos. A API valida o caminho dirigido (CT09) e marca como
// inválido o fluxo que perdeu uma Aresta ou um Nó (US03); só fluxos válidos entram no DEP.
export function Fluxos() {
  const { caso } = useCaso();
  const [editando, setEditando] = useState<Fluxo | null>(null);
  // Muda quando Nós/Arestas mudam (inclusive troca de origem/destino) — a validade dos fluxos depende deles.
  const chave = `${caso.id}:${caso.nos.map((n) => n.id).join(',')}:${caso.arestas.map((a) => `${a.id}${a.noOrigemId}${a.noDestinoId}`).join(',')}`;
  const fluxos = useCarregar(() => apiClient.listarFluxos(caso.id), [chave]);
  const nomeDo = (id: string) => caso.nos.find((n) => n.id === id)?.nome ?? 'Nó removido';

  async function criar(dados: CriarFluxoRequest) {
    await apiClient.criarFluxo(caso.id, dados);
    await fluxos.recarregar();
  }

  async function salvarEdicao(fluxo: Fluxo, dados: CriarFluxoRequest) {
    await apiClient.atualizarFluxo(caso.id, fluxo.id, dados);
    await fluxos.recarregar();
    setEditando(null);
  }

  async function excluir(fluxo: Fluxo) {
    await apiClient.excluirFluxo(caso.id, fluxo.id);
    if (editando?.id === fluxo.id) setEditando(null);
    await fluxos.recarregar();
  }

  if (caso.nos.length < 2 || caso.arestas.length === 0) {
    return (
      <EstadoVazio titulo="Cadastre Nós e Arestas primeiro">
        Um fluxo é um caminho pelo Mapa, então precisa de Nós ligados por Arestas. <Link to="../nos">Nós</Link> ·{' '}
        <Link to="../arestas">Arestas</Link>
      </EstadoVazio>
    );
  }
  if (fluxos.carregando && !fluxos.dados) return <Carregando texto="Carregando fluxos…" />;
  if (!fluxos.dados) return <MensagemDeErro erro={fluxos.erro} aoTentarDeNovo={fluxos.recarregar} />;

  const lista = fluxos.dados;
  const invalidos = lista.filter((f) => !f.valido).length;

  return (
    <div className="duas-colunas">
      <div className="duas-colunas__lateral">
        {editando ? (
          <FluxoForm
            key={editando.id}
            nos={caso.nos}
            inicial={{ nome: editando.nome, nosIds: editando.nosIds }}
            aoSalvar={(dados) => salvarEdicao(editando, dados)}
            aoCancelar={() => setEditando(null)}
          />
        ) : (
          <FluxoForm key="novo" nos={caso.nos} aoSalvar={criar} />
        )}
      </div>
      <div className="duas-colunas__principal">
        <div className="cabecalho-secao">
          <div>
            <h2 className="titulo-secao">Fluxos principais ({lista.length})</h2>
            <p className="texto-suave">
              Os caminhos que as jornadas fazem pelo Mapa. Alimentam o eixo Dependência Organizacional (DEP); só fluxos válidos entram no
              cálculo.
            </p>
          </div>
          {lista.length > 0 && (
            <Link to="../mapa" className="botao botao--secundario botao--pequeno">
              Próximo: ver o Mapa →
            </Link>
          )}
        </div>
        {invalidos > 0 && (
          <p className="nota nota--alerta" role="status">
            {invalidos === 1 ? '1 fluxo ficou inválido e não entra' : `${invalidos} fluxos ficaram inválidos e não entram`} no DEP até ser corrigido.
          </p>
        )}
        {lista.length === 0 ? (
          <EstadoVazio titulo="Nenhum fluxo ainda">
            Sem fluxos principais o eixo DEP fica “não aplicável”. Descreva cada jornada como uma sequência de Nós, do primeiro ao último passo.
          </EstadoVazio>
        ) : (
          <ul className="lista-de-fluxos">
            {lista.map((f) => (
              <li key={f.id} className="cartao fluxo">
                <div className="fluxo__topo">
                  <h3 className="fluxo__nome">{f.nome}</h3>
                  <span className={`selo-estado ${f.valido ? 'selo-estado--ok' : 'selo-estado--invalido'}`}>{f.valido ? 'válido' : 'inválido'}</span>
                  <span className="texto-suave fluxo__bandas">
                    {f.bandasPercorridas} {f.bandasPercorridas === 1 ? 'banda' : 'bandas'}
                  </span>
                  <span className="fluxo__acoes">
                    <BotaoDeIcone icone="lapis" rotulo={`Editar o fluxo ${f.nome}`} onClick={() => setEditando(f)} />
                    <ExclusaoComConfirmacao
                      alvo={`Fluxo ${f.nome}`}
                      aviso="Excluir este fluxo. Os Nós e as Arestas do Mapa não são afetados."
                      aoConfirmar={() => excluir(f)}
                    />
                  </span>
                </div>
                <ol className="fluxo__caminho" aria-label={`Passos do fluxo ${f.nome}`}>
                  {f.nosIds.map((id, i) => (
                    <li key={`${i}-${id}`}>{nomeDo(id)}</li>
                  ))}
                </ol>
                {!f.valido && f.etapaAusente && (
                  <p className="fluxo__problema">
                    <strong>Etapa {f.etapaAusente.posicao}:</strong> {f.etapaAusente.motivo} ({nomeDo(f.etapaAusente.origemId)} →{' '}
                    {nomeDo(f.etapaAusente.destinoId)})
                  </p>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
