import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { CampoComContador } from './CampoComContador';

// Comportamento comum a todos os formulários de texto (nome do Nó, do Estudo de Caso, do Fluxo,
// justificativa da Aresta): o servidor recusa texto acima do limite (422), então o campo impede
// de passar do limite e mostra quanto resta. Os formulários só conferem o limite que usam.
function Campo({ max, multilinha }: { max: number; multilinha?: boolean }) {
  const [valor, definir] = useState('');
  return <CampoComContador id="campo" valor={valor} aoMudar={definir} max={max} multilinha={multilinha} />;
}

describe.each([
  ['linha única', false],
  ['várias linhas', true],
])('CampoComContador (%s)', (_, multilinha) => {
  it('mostra "n/limite" conforme se digita e não aceita passar do limite', async () => {
    const user = userEvent.setup();
    render(<Campo max={5} multilinha={multilinha} />);
    const campo = screen.getByRole('textbox');
    expect(screen.getByText('0/5')).toBeInTheDocument();

    await user.type(campo, 'abcdefgh');

    expect(campo).toHaveValue('abcde');
    expect(screen.getByText('5/5')).toBeInTheDocument();
  });
});
