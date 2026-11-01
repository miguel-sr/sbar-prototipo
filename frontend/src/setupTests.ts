import '@testing-library/jest-dom/vitest';

// O jsdom não implementa <dialog>.showModal()/close() (verificado no jsdom 30). Polyfill MÍNIMO,
// só para os testes de componente conseguirem abrir/fechar os modais de confirmação: alterna o
// atributo `open` e dispara `close`. Esc, clique no fundo, foco preso e posicionamento NÃO são
// simulados de propósito — isso é comportamento de navegador real e fica no E2E (Playwright).
if (typeof HTMLDialogElement !== 'undefined' && typeof HTMLDialogElement.prototype.showModal !== 'function') {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '');
  };
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute('open');
    this.dispatchEvent(new Event('close'));
  };
}
