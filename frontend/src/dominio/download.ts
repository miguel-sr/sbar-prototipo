// Entrega um arquivo (Blob) ao usuário pelo navegador: link temporário com `download` e clique programático.
export function salvarComoArquivo(blob: Blob, nome: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = nome;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
