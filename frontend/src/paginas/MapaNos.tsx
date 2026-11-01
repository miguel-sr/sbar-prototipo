import { useState } from 'react';
import { Link } from 'react-router';
import { useCaso } from '../componentes/CasoLayout';
import { ChipDeBanda } from '../componentes/ChipDeBanda';
import { EstadoVazio } from '../componentes/Estados';
import { BotaoDeIcone } from '../componentes/BotaoDeIcone';
import { ExclusaoComConfirmacao } from '../componentes/ExclusaoComConfirmacao';
import { NoForm } from '../componentes/NoForm';
import { ROTULO_TIPO_DE_NO } from '../dominio/rotulos';
import { apiClient } from '../servicos/apiClient';
import { BANDAS, type CriarNoRequest, type No } from '../tipos/No';

// T3 — MapaNos: cadastro dos Nós do Mapa de Interações. POST/PUT/DELETE /api/casos/{id}/nos.
export function MapaNos() {
  const { caso, recarregarCaso } = useCaso();
  const [editando, setEditando] = useState<No | null>(null);

  async function criar(dados: CriarNoRequest) {
    await apiClient.criarNo(caso.id, dados);
    await recarregarCaso();
  }

  async function salvarEdicao(no: No, dados: CriarNoRequest) {
    await apiClient.atualizarNo(caso.id, no.id, dados);
    await recarregarCaso();
    setEditando(null);
  }

  async function excluir(no: No) {
    await apiClient.excluirNo(caso.id, no.id);
    if (editando?.id === no.id) setEditando(null);
    await recarregarCaso();
  }

  const arestasLigadas = (no: No) => caso.arestas.filter((a) => a.noOrigemId === no.id || a.noDestinoId === no.id).length;

  const porBanda = BANDAS.map((banda) => ({ banda, nos: caso.nos.filter((n) => n.banda === banda) })).filter(
    (g) => g.nos.length > 0,
  );

  return (
    <div className="duas-colunas">
      <div className="duas-colunas__lateral">
        {editando ? (
          <NoForm
            key={editando.id}
            inicial={{ nome: editando.nome, tipo: editando.tipo }}
            aoSalvar={(dados) => salvarEdicao(editando, dados)}
            aoCancelar={() => setEditando(null)}
          />
        ) : (
          <NoForm key="novo" aoSalvar={criar} />
        )}
      </div>
      <div className="duas-colunas__principal">
        <div className="cabecalho-secao">
          <h2 className="titulo-secao">Nós cadastrados ({caso.nos.length})</h2>
          {caso.nos.length >= 2 && (
            <Link to="../arestas" className="botao botao--secundario botao--pequeno">
              Próximo: Arestas →
            </Link>
          )}
        </div>
        {caso.nos.length === 0 ? (
          <EstadoVazio titulo="Nenhum Nó ainda">
            Comece pelo Ator (quem inicia o fluxo) e depois cadastre as telas, processos, dados e serviços externos.
          </EstadoVazio>
        ) : (
          porBanda.map(({ banda, nos }) => (
            <div key={banda} className="grupo-banda">
              <div className="grupo-banda__titulo">
                <ChipDeBanda banda={banda} />
                <span className="texto-suave">{nos.length}</span>
              </div>
              <table className="tabela">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>Tipo</th>
                    <th className="tabela__acoes">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {nos.map((no) => {
                    const ligadas = arestasLigadas(no);
                    return (
                      <tr key={no.id}>
                        <td>{no.nome}</td>
                        <td className="texto-suave tabela__tipo">{ROTULO_TIPO_DE_NO[no.tipo]}</td>
                        <td className="tabela__acoes">
                          <BotaoDeIcone icone="lapis" rotulo={`Editar ${no.nome}`} onClick={() => setEditando(no)} />{' '}
                          <ExclusaoComConfirmacao
                            alvo={`Nó ${no.nome}`}
                            aviso={
                              ligadas > 0
                                ? `Excluir este Nó também remove ${ligadas} ${ligadas === 1 ? 'aresta ligada' : 'arestas ligadas'} a ele.`
                                : 'Excluir este Nó. Não dá para desfazer.'
                            }
                            aoConfirmar={() => excluir(no)}
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
