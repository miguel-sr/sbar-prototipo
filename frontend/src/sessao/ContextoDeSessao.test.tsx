import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RotaProtegida } from '../componentes/RotaProtegida';
import { Cadastrar } from '../paginas/Cadastrar';
import { Entrar } from '../paginas/Entrar';
import { apiClient, ErroDaApi } from '../servicos/apiClient';
import { destinoSeguro, SessaoProvider } from './ContextoDeSessao';

vi.mock('../servicos/apiClient', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../servicos/apiClient')>()),
  apiClient: (await import('../testes/apiFalsa')).apiClientFalso(),
}));

const usuario = { id: 'u1', email: 'ana@sbar.test' };

function renderizar(caminho: string) {
  return render(
    <SessaoProvider>
      <MemoryRouter initialEntries={[caminho]}>
        <Routes>
          <Route path="/entrar" element={<Entrar />} />
          <Route path="/cadastrar" element={<Cadastrar />} />
          <Route element={<RotaProtegida />}>
            <Route path="/" element={<p>Página inicial</p>} />
            <Route path="/casos/:id" element={<p>Tela do caso</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </SessaoProvider>,
  );
}

beforeEach(() => vi.clearAllMocks());

describe('rota protegida', () => {
  it('sem sessão, leva ao login guardando a rota de origem', async () => {
    vi.mocked(apiClient.eu).mockRejectedValue(new ErroDaApi(401, 'Unauthorized'));
    renderizar('/casos/abc');
    expect(await screen.findByRole('heading', { name: 'Entrar no SBAR' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Criar conta' })).toHaveAttribute('href', '/cadastrar?de=%2Fcasos%2Fabc');
  });

  it('com sessão, mostra a tela pedida', async () => {
    vi.mocked(apiClient.eu).mockResolvedValue(usuario);
    renderizar('/casos/abc');
    expect(await screen.findByText('Tela do caso')).toBeInTheDocument();
  });

  it('enquanto confere a sessão, não mostra a tela nem o login', () => {
    vi.mocked(apiClient.eu).mockReturnValue(new Promise(() => {}));
    renderizar('/');
    expect(screen.getByRole('status')).toHaveTextContent('Verificando sua sessão');
    expect(screen.queryByText('Página inicial')).not.toBeInTheDocument();
  });

  it('um 401 em qualquer chamada derruba a sessão e volta ao login', async () => {
    vi.mocked(apiClient.eu).mockResolvedValue(usuario);
    renderizar('/');
    expect(await screen.findByText('Página inicial')).toBeInTheDocument();

    // O provider registrou o aviso no módulo real; uma chamada real que volta 401 (fetch simulado) o dispara.
    const real = (await vi.importActual<typeof import('../servicos/apiClient')>('../servicos/apiClient')).apiClient;
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('', { status: 401, statusText: 'Unauthorized' }));
    await real.listarCasos().catch(() => undefined);

    expect(await screen.findByRole('heading', { name: 'Entrar no SBAR' })).toBeInTheDocument();
    vi.restoreAllMocks();
  });
});

describe('Entrar', () => {
  it('entra e vai para a rota de origem (?de=)', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.eu).mockRejectedValue(new ErroDaApi(401, 'Unauthorized'));
    vi.mocked(apiClient.entrar).mockResolvedValue(usuario);
    renderizar('/entrar?de=%2Fcasos%2Fabc');

    await user.type(await screen.findByLabelText('E-mail'), ' ana@sbar.test ');
    await user.type(screen.getByLabelText('Senha'), 'Senha1234');
    await user.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(apiClient.entrar).toHaveBeenCalledWith({ email: 'ana@sbar.test', senha: 'Senha1234' });
    expect(await screen.findByText('Tela do caso')).toBeInTheDocument();
  });

  it('mostra o erro do servidor e mantém o formulário', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.eu).mockRejectedValue(new ErroDaApi(401, 'Unauthorized'));
    vi.mocked(apiClient.entrar).mockRejectedValue(new ErroDaApi(401, 'E-mail ou senha incorretos.'));
    renderizar('/entrar');

    await user.type(await screen.findByLabelText('E-mail'), 'ana@sbar.test');
    await user.type(screen.getByLabelText('Senha'), 'errada123');
    await user.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('E-mail ou senha incorretos.');
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeEnabled();
  });

  it('quem já tem sessão não vê o login', async () => {
    vi.mocked(apiClient.eu).mockResolvedValue(usuario);
    renderizar('/entrar');
    await waitFor(() => expect(screen.getByText('Página inicial')).toBeInTheDocument());
  });
});

describe('Cadastrar', () => {
  it('só habilita com e-mail, senha de 8+ caracteres e convite; cadastra e entra', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.eu).mockRejectedValue(new ErroDaApi(401, 'Unauthorized'));
    vi.mocked(apiClient.cadastrar).mockResolvedValue(usuario);
    renderizar('/cadastrar');

    const botao = await screen.findByRole('button', { name: 'Criar conta' });
    await user.type(screen.getByLabelText('E-mail'), 'ana@sbar.test');
    await user.type(screen.getByLabelText(/^Senha/), 'curta');
    await user.type(screen.getByLabelText('Código de convite'), 'convite');
    expect(botao).toBeDisabled();

    await user.type(screen.getByLabelText(/^Senha/), '1234');
    expect(botao).toBeEnabled();
    await user.click(botao);

    expect(apiClient.cadastrar).toHaveBeenCalledWith({ email: 'ana@sbar.test', senha: 'curta1234', codigoDeConvite: 'convite' });
    expect(await screen.findByText('Página inicial')).toBeInTheDocument();
  });

  it('mostra a recusa do convite', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.eu).mockRejectedValue(new ErroDaApi(401, 'Unauthorized'));
    vi.mocked(apiClient.cadastrar).mockRejectedValue(new ErroDaApi(403, 'Código de convite inválido.'));
    renderizar('/cadastrar');

    await user.type(await screen.findByLabelText('E-mail'), 'ana@sbar.test');
    await user.type(screen.getByLabelText(/^Senha/), 'Senha1234');
    await user.type(screen.getByLabelText('Código de convite'), 'errado');
    await user.click(screen.getByRole('button', { name: 'Criar conta' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Código de convite inválido.');
  });
});

describe('destinoSeguro', () => {
  it.each([
    ['/casos/abc', '/casos/abc'],
    ['/perfis?x=1', '/perfis?x=1'],
    [null, '/'],
    ['', '/'],
    ['https://outro.site', '/'],
    ['//outro.site', '/'],
    ['/\\outro.site', '/'],
  ])('%s -> %s', (de, esperado) => expect(destinoSeguro(de)).toBe(esperado));
});
