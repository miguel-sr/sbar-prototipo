import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { CampoComContador } from '../componentes/CampoComContador';
import { MensagemDeErro } from '../componentes/Estados';
import { LIMITES } from '../dominio/validacao';
import { apiClient } from '../servicos/apiClient';

// T2 — CasoForm: dados gerais do Estudo de Caso. POST /api/casos.
// A edição de nome/descrição de um caso existente fica no cabeçalho da página do caso.
export function CasoForm() {
  const navegar = useNavigate();
  const [nome, setNome] = useState('');
  const [descricao, setDescricao] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<unknown>();

  async function salvar(e: FormEvent) {
    e.preventDefault();
    if (!nome.trim()) return;
    setEnviando(true);
    setErro(undefined);
    try {
      const caso = await apiClient.criarCaso({ nome: nome.trim(), dominioDescricao: descricao.trim() || null });
      navegar(`/casos/${caso.id}/nos`);
    } catch (err) {
      setErro(err);
      setEnviando(false);
    }
  }

  return (
    <section className="coluna-estreita">
      <Link to="/" className="voltar">
        ← Estudos de Caso
      </Link>
      <h1 className="titulo-pagina">Novo Estudo de Caso</h1>
      <p className="texto-suave">
        Dê um nome ao sistema que você vai mapear. Em seguida você cadastra os Nós e as Arestas do Mapa de Interações.
      </p>
      <form className="cartao formulario" onSubmit={salvar}>
        <label className="rotulo" htmlFor="caso-nome">
          Nome do sistema
        </label>
        <CampoComContador
          id="caso-nome"
          valor={nome}
          aoMudar={setNome}
          max={LIMITES.nomeDoCaso}
          placeholder="Ex.: Sistema de Pedidos Digital"
          obrigatorio
          autoFocus
        />
        <label className="rotulo" htmlFor="caso-descricao">
          Domínio de negócio <span className="opcional">(opcional)</span>
        </label>
        <CampoComContador
          id="caso-descricao"
          valor={descricao}
          aoMudar={setDescricao}
          max={LIMITES.descricaoDoCaso}
          placeholder="Ex.: pedidos de restaurante feitos pelo app do cliente"
          multilinha
        />
        {erro !== undefined && <MensagemDeErro erro={erro} />}
        <div className="formulario__acoes">
          <Link to="/" className="botao botao--secundario">
            Cancelar
          </Link>
          <button type="submit" className="botao botao--primario" disabled={enviando || !nome.trim()}>
            {enviando ? 'Criando…' : 'Criar e cadastrar Nós'}
          </button>
        </div>
      </form>
    </section>
  );
}
