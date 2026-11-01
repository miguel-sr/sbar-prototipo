import { useState } from 'react';
import { Link } from 'react-router';
import { useCaso } from '../componentes/CasoLayout';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { SeletorDeVersao } from '../componentes/SeletorDeVersao';
import { INFO_DO_EIXO } from '../dominio/eixos';
import { formatarData, formatarNumero } from '../dominio/rotulos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';
import { EIXOS, type Recomendacao as RecomendacaoDaApi } from '../tipos/Metricas';

const DISTANCIA_MAXIMA_PADRAO = 5;

// T7 — Perfil de referência mais próximo (TCC 4.6, RN09): distância até cada Perfil Arquitetural, a
// contribuição de cada eixo (Quadros 26/36), a margem entre as distâncias e a versão dos parâmetros.
// Mesma chamada de T6 (GET /api/casos/{id}/metricas), campo "recomendacao". É um SUBSÍDIO à decisão do
// arquiteto, nunca uma decisão (D14: o nome da rota e do campo da API não mudam; só o texto de tela).
export function Recomendacao() {
  const { caso } = useCaso();
  const [versao, setVersao] = useState<number>();
  const chave = `${caso.id}:${caso.nos.length}:${caso.arestas.length}`;
  const metricas = useCarregar(() => apiClient.obterMetricas(caso.id, versao), [chave, versao]);
  const versoes = useCarregar(() => apiClient.listarVersoesDeParametros(), [chave]);

  if (caso.nos.length === 0) {
    return (
      <EstadoVazio titulo="Sem Mapa, sem perfil mais próximo">
        <Link to="../nos">Cadastre os Nós</Link> e as Arestas primeiro.
      </EstadoVazio>
    );
  }
  if (metricas.carregando && !metricas.dados) return <Carregando texto="Comparando com os Perfis Arquiteturais…" />;
  if (!metricas.dados) return <MensagemDeErro erro={metricas.erro} aoTentarDeNovo={metricas.recarregar} />;

  const resultado = metricas.dados;
  const recomendacao = resultado.recomendacao;
  const vigente = versoes.dados?.reduce((maior, v) => Math.max(maior, v.numero), 0);
  const versaoEmUso = resultado.versaoDosParametros;
  const reaplicando = vigente !== undefined && versaoEmUso !== undefined && versaoEmUso.numero !== vigente;

  const seletor = versoes.dados && versoes.dados.length > 0 && versaoEmUso && (
    <div className="cartao">
      <SeletorDeVersao
        id="versao-dos-parametros"
        versoes={versoes.dados}
        selecionada={versaoEmUso.numero}
        aoMudar={(numero) => setVersao(numero === vigente ? undefined : numero)}
      />
      <p className="nota">
        {reaplicando
          ? `Reaplicando a versão ${versaoEmUso.numero} (${formatarData(versaoEmUso.criadaEm)}) aos mesmos Nós e Arestas; a versão vigente é a ${vigente}. Os parâmetros antigos não são alterados.`
          : `Parâmetros vigentes: versão ${versaoEmUso.numero}. Alterar limites, pesos ou assinaturas cria uma nova versão e o resultado é recalculado.`}{' '}
        <Link to="/parametros">Ver parâmetros</Link>
      </p>
    </div>
  );

  if (!recomendacao) {
    return (
      <div className="pilha">
        {seletor}
        <EstadoVazio titulo="Nenhum Perfil Arquitetural para comparar">
          A comparação relaciona o Radar deste caso com perfis de referência.{' '}
          <Link to="/perfis">Cadastre pelo menos um perfil</Link>.
        </EstadoVazio>
      </div>
    );
  }

  const distancias = [...recomendacao.distancias].sort((a, b) => a.distancia - b.distancia);
  const maximo = recomendacao.distanciaMaxima ?? DISTANCIA_MAXIMA_PADRAO;
  const [primeiro, segundo] = distancias;
  const margem = recomendacao.margem ?? (segundo ? segundo.distancia - primeiro.distancia : null);
  const normalizados = new Map(resultado.eixos.map((e) => [e.eixo, e.valorNormalizado]));
  const eixosComContribuicao = EIXOS.filter((eixo) => distancias.some((d) => d.contribuicoes?.some((c) => c.eixo === eixo)));
  const soma = (d: RecomendacaoDaApi['distancias'][number]) => (d.contribuicoes ?? []).reduce((total, c) => total + c.contribuicao, 0);

  return (
    <div className="pilha">
      {seletor}

      <div className="recomendacao-grade">
        <div className="cartao recomendacao">
          <p className="sobretitulo">Perfil de referência mais próximo</p>
          <h2 className="recomendacao__estilo">{recomendacao.estiloRecomendado}</h2>
          <p>
            É o perfil de referência de <strong>menor distância</strong> em relação ao Radar deste Estudo de Caso
            {segundo && (
              <>
                {' '}— {formatarNumero(primeiro.distancia, 3)}, contra {formatarNumero(segundo.distancia, 3)} de {segundo.perfilNome}
              </>
            )}
            . Um subsídio para a sua análise, não uma decisão.
          </p>
          <div className="alerta alerta--info">
            Este resultado é um <strong>subsídio à decisão</strong>: indica qual perfil de referência mais se parece com o
            serviço mapeado e <strong>não substitui a análise do arquiteto</strong> (custos, competências da equipe,
            requisitos e restrições que o Mapa não captura — TCC 4.6). As assinaturas de referência são provisórias e
            ainda serão validadas por especialistas (TCC 5.4).
          </div>
        </div>

        <div className="cartao">
          <h2 className="cartao__titulo">Distância até cada perfil de referência</h2>
          <p className="texto-suave">
            Distância euclidiana ponderada entre os dez eixos; a distância máxima possível é {formatarNumero(maximo, 0)}.
            Quanto menor, mais parecido.
          </p>
          <ol className="distancias">
            {distancias.map((d, i) => (
              <li key={d.perfilId} className={`distancia${i === 0 ? ' distancia--vencedora' : ''}`}>
                <div className="distancia__cabecalho">
                  <span>{d.perfilNome}</span>
                  <span className="numero">
                    {formatarNumero(d.distancia, 3)}
                    {d.percentualDoMaximo !== undefined && (
                      <span className="texto-suave"> · {formatarNumero(d.percentualDoMaximo, 1)}% do máximo</span>
                    )}
                  </span>
                </div>
                <div className="barra">
                  <div className="barra__preenchida" style={{ width: `${(d.distancia / maximo) * 100}%` }} />
                </div>
              </li>
            ))}
          </ol>
          {margem !== null && (
            <p className="nota" data-testid="margem">
              <strong>Margem entre as duas menores distâncias: {formatarNumero(margem, 3)}</strong> (
              {formatarNumero((margem / maximo) * 100, 1)}% da escala). Quanto menor a margem, mais intermediário é o
              perfil do serviço e menos a comparação, isoladamente, diferencia os estilos (TCC 4.6).
            </p>
          )}
          <p className="nota">
            d = √( Σ pᵢ × (Normᵢ do serviço − Normᵢ do perfil)² ). Os pesos pᵢ e as assinaturas fazem parte dos{' '}
            <Link to="/parametros">parâmetros versionados</Link>; os valores de cada perfil são ajustados em{' '}
            <Link to="/perfis">Perfis Arquiteturais</Link>.
          </p>
        </div>
      </div>

      {eixosComContribuicao.length > 0 && (
        <div className="cartao">
          <h2 className="cartao__titulo">Contribuição de cada eixo para a distância</h2>
          <p className="texto-suave">
            pᵢ · (Normᵢ do serviço − Normᵢ do perfil)². Mostra de onde vem a diferença entre os perfis: os eixos com
            maior contribuição são os que mais afastam o serviço do perfil (TCC 4.6, Quadros 26 e 36).
          </p>
          <div className="tabela-rolagem">
            <table className="tabela" aria-label="Contribuição de cada eixo para a distância">
              <thead>
                <tr>
                  <th>Eixo</th>
                  <th className="coluna-numero">Serviço</th>
                  {distancias.map((d) => (
                    <th key={d.perfilId} className="coluna-numero">
                      {d.perfilNome}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {eixosComContribuicao.map((eixo) => {
                  const normalizado = normalizados.get(eixo);
                  return (
                    <tr key={eixo}>
                      <td>
                        <span className="metrica__codigo">{eixo}</span> {INFO_DO_EIXO[eixo].nome}
                      </td>
                      <td className="coluna-numero numero">{normalizado == null ? '—' : formatarNumero(normalizado, 2)}</td>
                      {distancias.map((d) => {
                        const c = d.contribuicoes?.find((x) => x.eixo === eixo);
                        return (
                          <td key={d.perfilId} className="coluna-numero numero">
                            {c ? formatarNumero(c.contribuicao, 4) : '—'}
                          </td>
                        );
                      })}
                    </tr>
                  );
                })}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row" colSpan={2}>
                    Soma
                  </th>
                  {distancias.map((d) => (
                    <td key={d.perfilId} className="coluna-numero numero">
                      {formatarNumero(soma(d), 4)}
                    </td>
                  ))}
                </tr>
                <tr>
                  <th scope="row" colSpan={2}>
                    d = √soma
                  </th>
                  {distancias.map((d) => (
                    <td key={d.perfilId} className="coluna-numero numero">
                      {formatarNumero(d.distancia, 3)}
                    </td>
                  ))}
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}

      <div className="acoes-fim">
        <Link to="../radar" className="botao botao--secundario">
          ← Voltar ao Radar
        </Link>
        <Link to="../rastreabilidade" className="botao botao--primario">
          Por que? Ver a rastreabilidade →
        </Link>
      </div>
    </div>
  );
}
