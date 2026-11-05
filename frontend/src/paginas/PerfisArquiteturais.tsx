import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { Carregando, EstadoVazio, MensagemDeErro } from '../componentes/Estados';
import { ExclusaoComConfirmacao } from '../componentes/ExclusaoComConfirmacao';
import { INFO_DO_EIXO } from '../dominio/eixos';
import { useCarregar } from '../hooks/useCarregar';
import { apiClient } from '../servicos/apiClient';
import { EIXOS } from '../tipos/Metricas';
import { campoDoEixo, type DadosDoPerfilArquitetural, type PerfilArquitetural } from '../tipos/PerfilArquitetural';

type Rascunho = { nome: string } & Record<string, string>;

function paraRascunho(dados?: DadosDoPerfilArquitetural): Rascunho {
  const r: Rascunho = { nome: dados?.nome ?? '' };
  for (const eixo of EIXOS) r[campoDoEixo(eixo)] = dados ? String(dados[campoDoEixo(eixo)]) : '';
  return r;
}

function lerNumero(texto: string): number {
  return Number(texto.replace(',', '.'));
}

function validar(r: Rascunho): string | null {
  if (!r.nome.trim()) return 'Dê um nome ao perfil.';
  for (const eixo of EIXOS) {
    const v = lerNumero(r[campoDoEixo(eixo)]);
    if (r[campoDoEixo(eixo)].trim() === '' || Number.isNaN(v) || v < 0 || v > 5) {
      return `O valor de ${eixo} precisa ser um número entre 0 e 5.`;
    }
  }
  return null;
}

function paraDados(r: Rascunho): DadosDoPerfilArquitetural {
  const dados = { nome: r.nome.trim() } as DadosDoPerfilArquitetural;
  for (const eixo of EIXOS) dados[campoDoEixo(eixo)] = lerNumero(r[campoDoEixo(eixo)]);
  return dados;
}

interface FormProps {
  inicial?: PerfilArquitetural;
  rotuloSalvar: string;
  aoSalvar: (dados: DadosDoPerfilArquitetural) => Promise<void>;
  aoCancelar?: () => void;
  aoRemover?: () => Promise<void>;
}

function FormularioDePerfil({ inicial, rotuloSalvar, aoSalvar, aoCancelar, aoRemover }: FormProps) {
  const [rascunho, setRascunho] = useState<Rascunho>(() => paraRascunho(inicial));
  const [erro, setErro] = useState<unknown>();
  const [aviso, setAviso] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [salvo, setSalvo] = useState(false);

  const alterado = JSON.stringify(rascunho) !== JSON.stringify(paraRascunho(inicial));
  const mudar = (campo: string, valor: string) => {
    setRascunho((r) => ({ ...r, [campo]: valor }));
    setSalvo(false);
  };

  async function enviar(e: FormEvent) {
    e.preventDefault();
    const problema = validar(rascunho);
    setAviso(problema);
    if (problema) return;
    setSalvando(true);
    setErro(undefined);
    try {
      await aoSalvar(paraDados(rascunho));
      setSalvo(true);
    } catch (err) {
      setErro(err);
    } finally {
      setSalvando(false);
    }
  }

  return (
    <form className="cartao formulario perfil" onSubmit={enviar}>
      <label className="rotulo" htmlFor={`perfil-nome-${inicial?.id ?? 'novo'}`}>
        Nome do estilo arquitetural
      </label>
      <input
        id={`perfil-nome-${inicial?.id ?? 'novo'}`}
        className="campo perfil__nome"
        value={rascunho.nome}
        onChange={(e) => mudar('nome', e.target.value)}
        placeholder="Ex.: Monólito Modular"
      />
      <div className="perfil__eixos">
        {EIXOS.map((eixo) => {
          const campo = campoDoEixo(eixo);
          const id = `${inicial?.id ?? 'novo'}-${eixo}`;
          return (
            <div key={eixo} className="perfil__eixo">
              <label htmlFor={id} className="perfil__rotulo" title={INFO_DO_EIXO[eixo].nome}>
                <span className="metrica__codigo">{eixo}</span>
                <span className="perfil__nome-eixo">{INFO_DO_EIXO[eixo].nome}</span>
              </label>
              <input
                id={id}
                className="campo campo--numero"
                type="number"
                inputMode="decimal"
                min={0}
                max={5}
                step={0.1}
                value={rascunho[campo]}
                onChange={(e) => mudar(campo, e.target.value)}
              />
            </div>
          );
        })}
      </div>
      {aviso && <p className="aviso-campo">{aviso}</p>}
      {erro !== undefined && <MensagemDeErro erro={erro} />}
      <div className="formulario__acoes">
        {aoRemover && (
          <ExclusaoComConfirmacao
            alvo={`Perfil ${inicial?.nome ?? ''}`.trim()}
            aviso="Excluir este Perfil Arquitetural o tira da comparação da recomendação. Não dá para desfazer."
            aoConfirmar={aoRemover}
          />
        )}
        <span className="espacador" />
        {salvo && !alterado && <span className="texto-sucesso">Salvo ✓</span>}
        {aoCancelar && (
          <button type="button" className="botao botao--secundario" onClick={aoCancelar}>
            Cancelar
          </button>
        )}
        <button type="submit" className="botao botao--primario" disabled={salvando || (!!inicial && !alterado)}>
          {salvando ? 'Salvando…' : rotuloSalvar}
        </button>
      </div>
    </form>
  );
}

// T9 — PerfisArquiteturais: configuração dos valores de referência (0–5) de cada estilo.
// Fora do fluxo linear; acessível pelo menu. CRUD em /api/perfis-arquiteturais.
export function PerfisArquiteturais() {
  const { dados: perfis, erro, carregando, recarregar } = useCarregar(() => apiClient.listarPerfis());
  const [criando, setCriando] = useState(false);

  return (
    <section className="pilha">
      <div className="cabecalho-pagina">
        <div>
          <h1 className="titulo-pagina">Perfis Arquiteturais</h1>
          <p className="texto-suave">
            Valores de referência (0 a 5) de cada estilo arquitetural nos dez eixos do Radar (assinaturas, Quadro 7). O
            perfil mais próximo de cada Estudo de Caso é o de menor distância. O TCC trata esses valores como
            provisórios, sujeitos a validação por especialistas (seções 4.6 e 5.4).
          </p>
          <p className="nota" data-testid="nota-de-versao">
            Cada alteração aqui (criar, editar ou excluir um perfil) gera uma <strong>nova versão dos parâmetros</strong>;
            as análises informam a versão usada e podem reaplicar versões antigas. <Link to="/parametros">Ver parâmetros e histórico</Link>.
          </p>
        </div>
        {!criando && (
          <button type="button" className="botao botao--primario" onClick={() => setCriando(true)}>
            + Novo perfil
          </button>
        )}
      </div>

      {criando && (
        <FormularioDePerfil
          rotuloSalvar="Criar perfil"
          aoCancelar={() => setCriando(false)}
          aoSalvar={async (dados) => {
            await apiClient.criarPerfil(dados);
            setCriando(false);
            await recarregar();
          }}
        />
      )}

      {carregando && !perfis && <Carregando />}
      {erro !== undefined && <MensagemDeErro erro={erro} aoTentarDeNovo={recarregar} />}
      {perfis && perfis.length === 0 && !criando && (
        <EstadoVazio titulo="Nenhum perfil cadastrado">Sem perfis, os Estudos de Caso não recebem recomendação.</EstadoVazio>
      )}
      {perfis?.map((perfil) => (
        <FormularioDePerfil
          key={perfil.id}
          inicial={perfil}
          rotuloSalvar="Salvar alterações"
          aoSalvar={async (dados) => {
            await apiClient.atualizarPerfil(perfil.id, dados);
            await recarregar();
          }}
          aoRemover={async () => {
            await apiClient.removerPerfil(perfil.id);
            await recarregar();
          }}
        />
      ))}
    </section>
  );
}
