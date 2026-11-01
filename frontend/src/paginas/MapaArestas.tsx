import { useState } from 'react';
import { Link } from 'react-router';
import { ArestaForm } from '../componentes/ArestaForm';
import { useCaso } from '../componentes/CasoLayout';
import { EstadoVazio } from '../componentes/Estados';
import { BotaoDeIcone } from '../componentes/BotaoDeIcone';
import { ExclusaoComConfirmacao } from '../componentes/ExclusaoComConfirmacao';
import { SeloDeAresta } from '../componentes/SeloDeAresta';
import { ROTULO_PESO } from '../dominio/rotulos';
import { justificativaEstaPendente } from '../dominio/validacao';
import { apiClient } from '../servicos/apiClient';
import type { Aresta, CriarArestaRequest } from '../tipos/Aresta';

// T4 — MapaArestas: cadastro das Arestas. POST/PUT/DELETE /api/casos/{id}/arestas.
export function MapaArestas() {
  const { caso, recarregarCaso } = useCaso();
  const [editando, setEditando] = useState<Aresta | null>(null);
  const nomeDo = (id: string) => caso.nos.find((n) => n.id === id)?.nome ?? '?';

  async function criar(dados: CriarArestaRequest) {
    await apiClient.criarAresta(caso.id, dados);
    await recarregarCaso();
  }

  async function salvarEdicao(aresta: Aresta, dados: CriarArestaRequest) {
    await apiClient.atualizarAresta(caso.id, aresta.id, dados);
    await recarregarCaso();
    setEditando(null);
  }

  async function excluir(aresta: Aresta) {
    await apiClient.excluirAresta(caso.id, aresta.id);
    if (editando?.id === aresta.id) setEditando(null);
    await recarregarCaso();
  }

  if (caso.nos.length < 2) {
    return (
      <EstadoVazio titulo="Cadastre pelo menos dois Nós primeiro">
        Uma Aresta liga dois Nós. <Link to="../nos">Ir para o cadastro de Nós</Link>
      </EstadoVazio>
    );
  }

  return (
    <div className="duas-colunas">
      <div className="duas-colunas__lateral">
        {editando ? (
          <ArestaForm
            key={editando.id}
            nos={caso.nos}
            inicial={{
              noOrigemId: editando.noOrigemId,
              noDestinoId: editando.noDestinoId,
              tipo: editando.tipo,
              peso: editando.peso,
              justificativa: editando.justificativa,
            }}
            aoSalvar={(dados) => salvarEdicao(editando, dados)}
            aoCancelar={() => setEditando(null)}
          />
        ) : (
          <ArestaForm key="nova" nos={caso.nos} aoSalvar={criar} />
        )}
      </div>
      <div className="duas-colunas__principal">
        <div className="cabecalho-secao">
          <h2 className="titulo-secao">Arestas cadastradas ({caso.arestas.length})</h2>
          {caso.arestas.length > 0 && (
            <Link to="../fluxos" className="botao botao--secundario botao--pequeno">
              Próximo: Fluxos →
            </Link>
          )}
        </div>
        {caso.arestas.length === 0 ? (
          <EstadoVazio titulo="Nenhuma Aresta ainda">
            Ligue os Nós na ordem em que as coisas acontecem: “isso leva a isso”.
          </EstadoVazio>
        ) : (
          <table className="tabela">
            <thead>
              <tr>
                <th>Origem</th>
                <th aria-label="leva a" />
                <th>Destino</th>
                <th>Tipo</th>
                <th>Peso</th>
                <th>Justificativa</th>
                <th className="tabela__acoes">Ações</th>
              </tr>
            </thead>
            <tbody>
              {caso.arestas.map((a) => {
                const descricao = `Aresta ${nomeDo(a.noOrigemId)} → ${nomeDo(a.noDestinoId)}`;
                return (
                  <tr key={a.id}>
                    <td>{nomeDo(a.noOrigemId)}</td>
                    <td className="texto-suave" aria-hidden="true">
                      →
                    </td>
                    <td>{nomeDo(a.noDestinoId)}</td>
                    <td>
                      <SeloDeAresta tipo={a.tipo} />
                    </td>
                    <td>
                      {a.peso} <span className="texto-suave">· {ROTULO_PESO[a.peso]}</span>
                    </td>
                    <td className="tabela__justificativa">
                      {justificativaEstaPendente(a.justificativa) ? (
                        <span className="selo-pendente" title={a.justificativa}>
                          pendente de revisão
                        </span>
                      ) : (
                        a.justificativa
                      )}
                    </td>
                    <td className="tabela__acoes">
                      <BotaoDeIcone icone="lapis" rotulo={`Editar ${descricao}`} onClick={() => setEditando(a)} />{' '}
                      <ExclusaoComConfirmacao alvo={descricao} aviso="Excluir esta Aresta. Não dá para desfazer." aoConfirmar={() => excluir(a)} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
