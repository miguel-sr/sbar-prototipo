import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { JUSTIFICATIVA_PENDENTE } from '../dominio/validacao';
import { fabricaNo } from '../testes/auxiliares';
import { ArestaForm } from './ArestaForm';

const nos = [fabricaNo('a', 'Paciente', 'Ator'), fabricaNo('b', 'Portal', 'Touchpoint'), fabricaNo('c', 'Verificação', 'Processo')];

const origem = () => screen.getByRole('combobox', { name: 'De (origem)' });
const destino = () => screen.getByRole('combobox', { name: 'Para (destino)' });
const botaoAdicionar = () => screen.getByRole('button', { name: 'Adicionar Aresta' });
const justificativa = () => screen.getByRole('textbox', { name: 'Justificativa' });

describe('ArestaForm — criação', () => {
  // RN03 / CT06: toda Aresta precisa de justificativa (o servidor também recusa; aqui o formulário nem deixa enviar).
  it('só habilita o envio com origem, destino e uma justificativa de verdade (só espaços não vale), limitada a 500', async () => {
    const user = userEvent.setup();
    render(<ArestaForm nos={nos} aoSalvar={vi.fn()} />);
    expect(screen.getByText('0/500')).toBeInTheDocument();
    expect(botaoAdicionar()).toBeDisabled();
    await user.selectOptions(origem(), 'Paciente');
    expect(botaoAdicionar()).toBeDisabled();
    await user.selectOptions(destino(), 'Portal');
    expect(botaoAdicionar()).toBeDisabled();
    await user.type(justificativa(), '    ');
    expect(botaoAdicionar()).toBeDisabled();
    await user.type(justificativa(), 'Paciente acessa o portal');
    expect(botaoAdicionar()).toBeEnabled();
  });

  it('avisa e desabilita quando origem e destino são o mesmo Nó', async () => {
    const user = userEvent.setup();
    render(<ArestaForm nos={nos} aoSalvar={vi.fn()} />);
    await user.selectOptions(origem(), 'Portal');
    await user.selectOptions(destino(), 'Portal');
    expect(screen.getByText('Origem e destino precisam ser Nós diferentes.')).toBeInTheDocument();
    expect(botaoAdicionar()).toBeDisabled();
  });

  it('envia os ids, o tipo e o peso (padrão: Síncrona, peso 3) e depois encadeia: o destino vira a próxima origem', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<ArestaForm nos={nos} aoSalvar={aoSalvar} />);
    await user.selectOptions(origem(), 'Paciente');
    await user.selectOptions(destino(), 'Portal');
    await user.type(justificativa(), '  Paciente acessa o portal  ');
    await user.click(botaoAdicionar());

    expect(aoSalvar).toHaveBeenCalledWith({ noOrigemId: 'a', noDestinoId: 'b', tipo: 'Sincrona', peso: 3, justificativa: 'Paciente acessa o portal' });
    await vi.waitFor(() => expect(origem()).toHaveValue('b'));
    expect(destino()).toHaveValue('');
    expect(justificativa()).toHaveValue('');
  });

  it('envia o tipo e o peso escolhidos', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<ArestaForm nos={nos} aoSalvar={aoSalvar} />);
    await user.selectOptions(origem(), 'Portal');
    await user.selectOptions(destino(), 'Verificação');
    await user.click(screen.getByRole('radio', { name: /Assíncrona/ }));
    await user.click(screen.getByRole('radio', { name: /1 · Baixo/ }));
    await user.type(justificativa(), 'Notificação sem bloquear o fluxo');
    await user.click(botaoAdicionar());
    expect(aoSalvar).toHaveBeenCalledWith({
      noOrigemId: 'b',
      noDestinoId: 'c',
      tipo: 'Assincrona',
      peso: 1,
      justificativa: 'Notificação sem bloquear o fluxo',
    });
  });

  it('mostra o title do 422 da API', async () => {
    const user = userEvent.setup();
    const { ErroDaApi } = await import('../servicos/apiClient');
    const aoSalvar = vi.fn().mockRejectedValue(new ErroDaApi(422, 'Peso 4 inválido — deve ser 1, 2 ou 3.'));
    render(<ArestaForm nos={nos} aoSalvar={aoSalvar} />);
    await user.selectOptions(origem(), 'Paciente');
    await user.selectOptions(destino(), 'Portal');
    await user.type(justificativa(), 'qualquer');
    await user.click(botaoAdicionar());
    expect(await screen.findByRole('alert')).toHaveTextContent('Peso 4 inválido');
  });
});

describe('ArestaForm — edição', () => {
  const inicial = { noOrigemId: 'a', noDestinoId: 'b', tipo: 'Critica' as const, peso: 2 as const, justificativa: 'Etapa 2 do Blueprint' };

  it('pré-preenche origem, destino, tipo e peso', () => {
    render(<ArestaForm nos={nos} inicial={inicial} aoSalvar={vi.fn()} aoCancelar={vi.fn()} />);
    expect(screen.getByRole('heading', { name: 'Editar Aresta' })).toBeInTheDocument();
    expect(origem()).toHaveValue('a');
    expect(destino()).toHaveValue('b');
    expect(screen.getByRole('radio', { name: /Crítica/ })).toBeChecked();
    expect(screen.getByRole('radio', { name: /2 · Médio/ })).toBeChecked();
    expect(justificativa()).toHaveValue('Etapa 2 do Blueprint');
  });

  it('aresta antiga com a justificativa-padrão: o campo começa vazio, avisa e exige escrever', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    const pendente = { ...inicial, justificativa: JUSTIFICATIVA_PENDENTE };
    render(<ArestaForm nos={nos} inicial={pendente} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);

    expect(justificativa()).toHaveValue('');
    expect(screen.getByText(/cadastrada antes da exigência de justificativa/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar alterações' })).toBeDisabled();

    await user.type(justificativa(), 'Revisada contra o Blueprint');
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));
    expect(aoSalvar).toHaveBeenCalledWith({ ...inicial, justificativa: 'Revisada contra o Blueprint' });
  });

  it('salva as mudanças de tipo e de destino e não limpa os campos depois', async () => {
    const user = userEvent.setup();
    const aoSalvar = vi.fn().mockResolvedValue(undefined);
    render(<ArestaForm nos={nos} inicial={inicial} aoSalvar={aoSalvar} aoCancelar={vi.fn()} />);
    await user.selectOptions(destino(), 'Verificação');
    await user.click(screen.getByRole('radio', { name: /Batch/ }));
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    expect(aoSalvar).toHaveBeenCalledWith({ noOrigemId: 'a', noDestinoId: 'c', tipo: 'Batch', peso: 2, justificativa: 'Etapa 2 do Blueprint' });
    expect(origem()).toHaveValue('a');
    expect(destino()).toHaveValue('c');
  });
});
