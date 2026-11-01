import { render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { fabricaNo } from '../testes/auxiliares';
import { SeletorDeNo } from './SeletorDeNo';

// Os formulários de Aresta e de Fluxo escolhem Nós por aqui: a lista é agrupada por Banda, na ordem
// do Service Blueprint, e Bandas sem Nós não aparecem.
describe('SeletorDeNo', () => {
  it('agrupa os Nós por Banda na ordem do Blueprint e omite Bandas vazias', () => {
    const nos = [fabricaNo('p', 'Verificação', 'Processo'), fabricaNo('a', 'Paciente', 'Ator'), fabricaNo('t', 'Portal', 'Touchpoint')];
    render(<SeletorDeNo id="no" rotulo="Nó" nos={nos} valor="" aoMudar={vi.fn()} />);
    const seletor = screen.getByRole('combobox', { name: 'Nó' });

    const grupos = within(seletor).getAllByRole('group');
    expect(grupos.map((g) => g.getAttribute('label'))).toEqual(['Ator', 'Frontstage', 'Backstage']);
    expect(within(screen.getByRole('group', { name: 'Frontstage' })).getByRole('option', { name: 'Portal' })).toBeInTheDocument();
  });
});
