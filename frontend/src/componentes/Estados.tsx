import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { ehNaoEncontrado, mensagemDeErro } from '../servicos/apiClient';

export function Carregando({ texto = 'Carregando…' }: { texto?: string }) {
  return (
    <div className="estado estado--carregando" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      {texto}
    </div>
  );
}

// "Tentar de novo" só faz sentido para falha de rede/5xx; um 404 continua 404.
export function MensagemDeErro({ erro, aoTentarDeNovo }: { erro: unknown; aoTentarDeNovo?: () => void }) {
  const podeTentarDeNovo = aoTentarDeNovo !== undefined && !ehNaoEncontrado(erro);
  return (
    <div className="alerta alerta--erro" role="alert">
      <div>
        <strong>Algo deu errado.</strong> {mensagemDeErro(erro)}
      </div>
      {podeTentarDeNovo && (
        <button type="button" className="botao botao--secundario botao--pequeno" onClick={aoTentarDeNovo}>
          Tentar de novo
        </button>
      )}
    </div>
  );
}

export function EstadoVazio({ titulo, children }: { titulo: string; children?: ReactNode }) {
  return (
    <div className="estado-vazio">
      <p className="estado-vazio__titulo">{titulo}</p>
      {children && <div className="estado-vazio__texto">{children}</div>}
    </div>
  );
}

export function CasoNaoEncontrado() {
  return (
    <EstadoVazio titulo="Estudo de Caso não encontrado">
      Ele pode ter sido excluído ou o endereço está incorreto.
      <div className="espaco-topo">
        <Link to="/" className="botao botao--secundario">
          Voltar para os Estudos de Caso
        </Link>
      </div>
    </EstadoVazio>
  );
}
