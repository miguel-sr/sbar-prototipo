import { useEffect, useRef, useState, type ReactNode } from 'react';
import { BotaoDeIcone } from './BotaoDeIcone';
import { MensagemDeErro } from './Estados';

interface Props {
  /** O que será excluído, para o título e o rótulo acessível (ex.: "Nó Carrinho"). */
  alvo: string;
  /** Aviso mostrado na confirmação (consequências da exclusão). */
  aviso: ReactNode;
  aoConfirmar: () => Promise<void>;
}

// Confirmação em modal centralizado (<dialog> nativo, sem window.confirm): o primeiro clique abre
// o aviso, só o botão "Confirmar exclusão" exclui. Esc, "Cancelar" e clique no fundo fecham.
export function ExclusaoComConfirmacao({ alvo, aviso, aoConfirmar }: Props) {
  const [confirmando, setConfirmando] = useState(false);
  const [excluindo, setExcluindo] = useState(false);
  const [erro, setErro] = useState<unknown>();
  const dialogo = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (confirmando && dialogo.current && !dialogo.current.open) dialogo.current.showModal();
  }, [confirmando]);

  function fechar() {
    if (excluindo) return;
    setConfirmando(false);
    setErro(undefined);
  }

  async function confirmar() {
    setExcluindo(true);
    setErro(undefined);
    try {
      await aoConfirmar();
    } catch (e) {
      setErro(e);
      setExcluindo(false);
    }
  }

  return (
    <>
      <BotaoDeIcone icone="lixeira" perigo rotulo={`Excluir ${alvo}`} onClick={() => setConfirmando(true)} />
      {confirmando && (
        <dialog
          ref={dialogo}
          className="modal"
          aria-label={`Confirmar exclusão de ${alvo}`}
          onCancel={(e) => {
            e.preventDefault();
            fechar();
          }}
          onClick={(e) => {
            if (e.target === dialogo.current) fechar();
          }}
        >
          <div className="modal__corpo">
            <h2 className="modal__titulo">Excluir {alvo}?</h2>
            <p className="modal__aviso">{aviso}</p>
            {erro !== undefined && <MensagemDeErro erro={erro} />}
            <div className="modal__acoes">
              <button type="button" className="botao botao--secundario" disabled={excluindo} onClick={fechar}>
                Cancelar
              </button>
              <button type="button" className="botao botao--perigo" disabled={excluindo} onClick={confirmar}>
                {excluindo ? 'Excluindo…' : 'Confirmar exclusão'}
              </button>
            </div>
          </div>
        </dialog>
      )}
    </>
  );
}
