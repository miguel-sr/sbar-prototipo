import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ErroDaApi } from '../servicos/apiClient';
import { fabricaNo } from '../testes/auxiliares';
import { FluxoForm } from './FluxoForm';

const nos = [
  fabricaNo('a', 'Paciente', 'Ator'),
  fabricaNo('b', 'Portal', 'Touchpoint'),
  fabricaNo('c', 'Verificação', 'Processo'),
  fabricaNo('d', 'Repositório', 'Dados'),
];

const nome = () => screen.getByRole('textbox', { name: 'Nome do fluxo' });
const proximoPasso = () => screen.getByRole('combobox', { name: 'Próximo passo' });
const botaoAdicionarPasso = () => screen.getByRole('button', { name: 'Adicionar passo' });
const botaoSalvar = () => screen.getByRole('button', { name: 'Adicionar Fluxo' });
const itensDaSequencia = () => within(screen.getByRole('list', { name: 'Passos do fluxo' })).getAllByRole('listitem');

async function adicionarPasso(user: ReturnType<typeof userEvent.setup>, nomeDoNo: string) {
  await user.selectOptions(proximoPasso(), nomeDoNo);
  await user.click(botaoAdicionarPasso());
}

describe('FluxoForm — criação', () => {
  // US03 / RF05: a sequência de passos é a entrada do método (a ORDEM e a repetição de Nós importam).
  it('monta a sequência na ordem dos passos adicionados, permitindo repetir o mesmo Nó', async () => {
    const user = userEvent.setup();
    render(<FluxoForm nos={nos} aoSalvar={vi.fn()} />);
    expect(screen.getByText('Nenhum passo ainda. Escolha o primeiro Nó abaixo.')).toBeInTheDocument();
    await adicionarPasso(user, 'Portal');
    await adicionarPasso(user, 'Verificação');
    await adicionarPasso(user, 'Portal');

    expect(itensDaSequencia().map((li) => li.textContent)).toEqual(['1Portal', '2Verificação', '3Portal'].map((t) => expect.stringContaining(t)));
    expect(proximoPasso()).toHaveValue('');
  });

  it('reordena e remove passos; subir o primeiro ou descer o último não altera a ordem', async () => {
    const user = userEvent.setup();
    render(<FluxoForm nos={nos} aoSalvar={vi.fn()} />);
    await adicionarPasso(user, 'Paciente');
    await adicionarPasso(user, 'Portal');
    await adicionarPasso(user, 'Verificação');

    await user.click(screen.getByRole('button', { name: 'Subir o passo 3 (Verificação)' }));
    await user.click(screen.getByRole('button', { name: 'Descer o passo 1 (Paciente)' }));
    expect(itensDaSequencia().map((li) => li.textContent)).toEqual(['1Verificação', '2Paciente', '3Portal'].map((t) => expect.stringContaining(t)));

    await user.click(screen.getByRole('button', { name: 'Subir o passo 1 (Verificação)' }));
    await user.click(screen.getByRole('button', { name: 'Descer o passo 3 (Portal)' }));
    expect(itensDaSequencia()[0]).toHaveTextContent('Verificação');
    expect(itensDaSequencia()[2]).toHaveTextContent('Portal');

    await user.click(screen.getByRole('button', { name: 'Remover o passo 1 (Verificação)' }));
    expect(itensDaSequencia()).toHaveLength(2);
    expect(itensDaSequencia()[0]).toHaveTextContent('Paciente');
  });

  it('só habilita o envio com nome e pelo menos dois passos (nome só com espaços não vale), com nome limitado a 100', async () => {
    const user = userEvent.setup();
    render(<FluxoForm nos={nos} aoSalvar={vi.fn()} />);
    expect(screen.getByText('0/100')).toBeInTheDocument();
    expect(screen.getByText('Um fluxo precisa de pelo menos dois passos.')).toBeInTheDocument();
    await adicionarPasso(user, 'Paciente');
    await user.type(nome(), 'Agendamento');
    expect(botaoSalvar()).toBeDisabled();
    await adicionarPasso(user, 'Portal');
    expect(botaoSalvar()).toBeEnabled();
    await user.clear(nome());
    await user.type(nome(), '   ');
    expect(botaoSalvar()).toBeDisabled();
  });

  it('envia o nome sem espaços nas pontas e os ids dos Nós na ordem; depois limpa o formulário', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<FluxoForm nos={nos} aoSalvar={aoSalvar} />);
    await user.type(nome(), '  Agendamento  ');
    await adicionarPasso(user, 'Paciente');
    await adicionarPasso(user, 'Portal');
    await adicionarPasso(user, 'Paciente');
    await user.click(botaoSalvar());

    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Agendamento', nosIds: ['a', 'b', 'a'] });
    await vi.waitFor(() => expect(nome()).toHaveValue(''));
    expect(screen.getByText('Nenhum passo ainda. Escolha o primeiro Nó abaixo.')).toBeInTheDocument();
  });

  it('mostra o title do 422 da API (etapa sem Aresta) e mantém o que foi digitado', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockRejectedValue(new ErroDaApi(422, "Fluxo sem aresta entre 'Portal' e 'Repositório' (etapa 2)."));
    render(<FluxoForm nos={nos} aoSalvar={aoSalvar} />);
    await user.type(nome(), 'Pedido');
    await adicionarPasso(user, 'Paciente');
    await adicionarPasso(user, 'Portal');
    await user.click(botaoSalvar());

    expect(await screen.findByRole('alert')).toHaveTextContent('etapa 2');
    expect(nome()).toHaveValue('Pedido');
    expect(itensDaSequencia()).toHaveLength(2);
  });
});

describe('FluxoForm — edição', () => {
  const inicial = { nome: 'Pedido e preparo', nosIds: ['a', 'b', 'c'] };

  it('pré-preenche nome e passos e muda o texto do botão', () => {
    render(<FluxoForm nos={nos} inicial={inicial} aoSalvar={vi.fn()} aoCancelar={vi.fn()} />);
    expect(screen.getByRole('heading', { name: 'Editar Fluxo' })).toBeInTheDocument();
    expect(nome()).toHaveValue('Pedido e preparo');
    expect(itensDaSequencia()).toHaveLength(3);
    expect(screen.getByRole('button', { name: 'Salvar alterações' })).toBeEnabled();
  });

  it('salva a sequência alterada e não limpa o formulário', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<FluxoForm nos={nos} inicial={inicial} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);
    await user.click(screen.getByRole('button', { name: 'Remover o passo 3 (Verificação)' }));
    await adicionarPasso(user, 'Repositório');
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(aoSalvar).toHaveBeenCalledWith({ nome: 'Pedido e preparo', nosIds: ['a', 'b', 'd'] });
    expect(nome()).toHaveValue('Pedido e preparo');
    expect(itensDaSequencia()).toHaveLength(3);
  });
});
