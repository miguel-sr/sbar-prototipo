import { useState } from 'react';
import { NavLink, Outlet, useNavigate, useOutletContext, useParams } from 'react-router';
import { ehGuid } from '../dominio/validacao';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient, ehNaoEncontrado, ErroDaApi } from '../servicos/apiClient';
import type { EstudoDeCaso } from '../tipos/EstudoDeCaso';
import { BotaoDeIcone } from './BotaoDeIcone';
import { EditarCasoForm } from './EditarCasoForm';
import { Carregando, CasoNaoEncontrado, MensagemDeErro } from './Estados';
import { ExclusaoComConfirmacao } from './ExclusaoComConfirmacao';

export interface ContextoDoCaso {
  caso: EstudoDeCaso;
  /** Busca de novo o Estudo de Caso (Nós/Arestas) depois de uma alteração. */
  recarregarCaso: () => Promise<void>;
}

export function useCaso() {
  return useOutletContext<ContextoDoCaso>();
}

// Ordem do fluxo do método (POP-09, Figura A): T3 → T4 → T4b (Fluxos, TCC revisado) → T5 → T6 → T7 → T8.
// Não é um wizard bloqueante: dá para voltar e editar o Mapa a qualquer momento (API.md seção 2).
const ETAPAS = [
  { caminho: 'nos', rotulo: 'Nós', tela: 'T3' },
  { caminho: 'arestas', rotulo: 'Arestas', tela: 'T4' },
  { caminho: 'fluxos', rotulo: 'Fluxos', tela: 'T4b' },
  { caminho: 'mapa', rotulo: 'Mapa', tela: 'T5' },
  { caminho: 'radar', rotulo: 'Métricas e Radar', tela: 'T6' },
  { caminho: 'recomendacao', rotulo: 'Perfil mais próximo', tela: 'T7' },
  { caminho: 'rastreabilidade', rotulo: 'Rastreabilidade', tela: 'T8' },
];

export function CasoLayout() {
  const { casoId = '' } = useParams();
  const navegar = useNavigate();
  const [editando, setEditando] = useState(false);
  // Id que não é GUID nunca vai existir: nem chama a API, cai direto no "não encontrado".
  const idValido = ehGuid(casoId);
  const { dados: caso, erro, carregando, recarregar } = useCarregar(
    () => (idValido ? apiClient.obterCaso(casoId) : Promise.reject(new ErroDaApi(404, 'Estudo de Caso não encontrado.'))),
    [casoId],
  );

  if (!caso && carregando) return <Carregando texto="Carregando Estudo de Caso…" />;
  if (!caso) return ehNaoEncontrado(erro) ? <CasoNaoEncontrado /> : <MensagemDeErro erro={erro} aoTentarDeNovo={recarregar} />;

  async function salvarDados(dados: { nome: string; dominioDescricao: string | null }) {
    await apiClient.atualizarCaso(caso!.id, dados);
    await recarregar();
    setEditando(false);
  }

  async function excluir() {
    await apiClient.excluirCaso(caso!.id);
    navegar('/');
  }

  return (
    <div className="caso">
      <div className="caso__cabecalho">
        <p className="sobretitulo">Estudo de Caso</p>
        {editando ? (
          <EditarCasoForm caso={caso} aoSalvar={salvarDados} aoCancelar={() => setEditando(false)} />
        ) : (
          <>
            <div className="caso__titulo-linha">
              <h1 className="titulo-pagina">{caso.nome}</h1>
              <div className="caso__acoes">
                <BotaoDeIcone icone="lapis" rotulo="Editar Estudo de Caso" onClick={() => setEditando(true)} />
                <ExclusaoComConfirmacao
                  alvo="Estudo de Caso"
                  aviso="Excluir o Estudo de Caso apaga também todos os Nós e Arestas do Mapa. Não dá para desfazer."
                  aoConfirmar={excluir}
                />
              </div>
            </div>
            {caso.dominioDescricao && <p className="texto-suave caso__descricao">{caso.dominioDescricao}</p>}
          </>
        )}
        <p className="caso__contagem">
          {caso.nos.length} {caso.nos.length === 1 ? 'nó' : 'nós'} · {caso.arestas.length}{' '}
          {caso.arestas.length === 1 ? 'aresta' : 'arestas'}
        </p>
      </div>
      <nav className="etapas" aria-label="Etapas do método">
        {ETAPAS.map((etapa, i) => (
          <NavLink key={etapa.caminho} to={etapa.caminho} className="etapas__item">
            <span className="etapas__numero">{i + 1}</span>
            {etapa.rotulo}
          </NavLink>
        ))}
      </nav>
      <Outlet context={{ caso, recarregarCaso: recarregar } satisfies ContextoDoCaso} />
    </div>
  );
}
