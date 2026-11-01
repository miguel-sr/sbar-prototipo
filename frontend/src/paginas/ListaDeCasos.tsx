import { Link } from 'react-router';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { formatarData } from '../dominio/rotulos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';

// T1 — ListaDeCasos: tela inicial. GET /api/casos.
export function ListaDeCasos() {
  const { dados: casos, erro, carregando, recarregar } = useCarregar(() => apiClient.listarCasos());

  return (
    <section>
      <div className="cabecalho-pagina">
        <div>
          <h1 className="titulo-pagina">Estudos de Caso</h1>
          <p className="texto-suave">
            Cada Estudo de Caso reúne o Mapa de Interações de um sistema. Desenhe o Mapa e o SBAR calcula o Radar
            Arquitetural e o perfil de referência mais próximo.
          </p>
        </div>
        <Link to="/casos/novo" className="botao botao--primario">
          + Novo Estudo de Caso
        </Link>
      </div>

      {carregando && !casos && <Carregando />}
      {erro !== undefined && <MensagemDeErro erro={erro} aoTentarDeNovo={recarregar} />}
      {casos && casos.length === 0 && (
        <EstadoVazio titulo="Nenhum Estudo de Caso ainda">
          Comece criando um — por exemplo, “Sistema de Pedidos Digital”.
          <div className="espaco-topo">
            <Link to="/casos/novo" className="botao botao--primario">
              Criar o primeiro Estudo de Caso
            </Link>
          </div>
        </EstadoVazio>
      )}
      {casos && casos.length > 0 && (
        <ul className="grade-cartoes">
          {casos.map((caso) => (
            <li key={caso.id}>
              <Link to={`/casos/${caso.id}`} className="cartao cartao--link">
                <h2 className="cartao__titulo">{caso.nome}</h2>
                <p className="texto-suave cartao__descricao">{caso.dominioDescricao || 'Sem descrição de domínio.'}</p>
                <div className="cartao__rodape">
                  <span>
                    {caso.nos.length} nós · {caso.arestas.length} arestas
                  </span>
                  <span>{formatarData(caso.dataCriacao)}</span>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
