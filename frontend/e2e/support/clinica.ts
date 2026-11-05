import type { APIRequestContext } from '@playwright/test';
import type { CriarArestaRequest, Peso, TipoDeAresta } from '../../src/tipos/Aresta';
import type { CriarNoRequest } from '../../src/tipos/No';
import { criarAresta, criarFluxo, criarNo, obterCaso } from './api';

// Estudo de Caso 2 — Sistema de Agendamento de Clínica (TCC revisado).
// Fonte: docs/dominio/estudos-de-caso.md — "Nós (15)" (Quadro 57), "Arestas (21)" (Quadro 58) e
// "Fluxos principais F" (seção 7.2). (O backend tem a mesma fixture em C# —
// Sbar.Application.Tests/Grafo/EstudosDeCasoDeReferencia.cs; a duplicação entre linguagens é
// intencional e só serve de DADO DE ENTRADA: nenhuma regra ou fórmula é reimplementada aqui.)
//
// ATENÇÃO À ORDEM: os nós são cadastrados NA ORDEM do Quadro 57 (Paciente — criado pela interface —
// primeiro). A detecção de comunidades não depende da ordem (o TCC explora todos os empates e adota a
// divisão de maior Q), mas a numeração C1…C4 segue a ordem de cadastro, como nos quadros do TCC.
export const NO_ATOR = 'Paciente';

const NOS: Record<string, CriarNoRequest> = {
  A: { nome: NO_ATOR, tipo: 'Ator' },
  TP1: { nome: 'Portal de Agendamento', tipo: 'Touchpoint' },
  TP2: { nome: 'Tela de Seleção de Horário', tipo: 'Touchpoint' },
  TP3: { nome: 'Tela de Confirmação', tipo: 'Touchpoint' },
  TP4: { nome: 'Canal de Notificação', tipo: 'Touchpoint' },
  P1: { nome: 'Verificação de Disponibilidade', tipo: 'Processo' },
  P2: { nome: 'Registro do Agendamento', tipo: 'Processo' },
  P3: { nome: 'Geração da Confirmação', tipo: 'Processo' },
  P4: { nome: 'Job de Lembrete', tipo: 'Processo' },
  P5: { nome: 'Check-in da Consulta', tipo: 'Processo' },
  S1: { nome: 'Autenticação/Cadastro', tipo: 'Suporte' },
  S2: { nome: 'Serviço de Notificações', tipo: 'Suporte' },
  DS: { nome: 'Repositório de Agendas', tipo: 'Dados' },
  E1: { nome: 'Gateway SMS/E-mail', tipo: 'Externo' },
  E2: { nome: 'Prontuário/Convênio', tipo: 'Externo' },
};

// [origem, destino, tipo, peso, justificativa] — a justificativa (RN03) é só dado de entrada.
const ARESTAS: Array<[string, string, TipoDeAresta, Peso, string]> = [
  ['A', 'TP1', 'Sincrona', 3, 'O paciente sempre acessa o portal para agendar.'],
  ['TP1', 'S1', 'Sincrona', 2, 'O portal exige autenticação ou cadastro.'],
  ['S1', 'DS', 'Sincrona', 2, 'Autenticação consulta e grava dados do paciente.'],
  ['TP1', 'TP2', 'Sincrona', 3, 'Depois de acessar, o paciente escolhe especialidade e horário.'],
  ['TP2', 'P1', 'Sincrona', 3, 'A seleção aciona a verificação de disponibilidade.'],
  ['P1', 'DS', 'Sincrona', 3, 'A verificação consulta as agendas.'],
  ['TP2', 'TP3', 'Sincrona', 3, 'Com o horário escolhido, o paciente vai à confirmação.'],
  ['TP3', 'P2', 'Sincrona', 3, 'A confirmação aciona o registro do agendamento.'],
  ['P2', 'DS', 'Sincrona', 3, 'O registro grava o horário na agenda.'],
  ['P2', 'P3', 'Assincrona', 2, 'A confirmação é gerada sem bloquear o agendamento.'],
  ['P3', 'S2', 'Assincrona', 2, 'A confirmação é entregue pelo serviço de notificações.'],
  ['S2', 'E1', 'Assincrona', 2, 'O serviço envia a confirmação pelo gateway externo.'],
  ['E1', 'TP4', 'Assincrona', 2, 'O gateway entrega a confirmação ao canal do paciente.'],
  ['P2', 'P4', 'Batch', 1, 'O agendamento programa o lembrete próximo à consulta.'],
  ['P4', 'S2', 'Batch', 1, 'O job de lembrete usa o serviço de notificações.'],
  ['S2', 'E1', 'Batch', 1, 'O lembrete também segue pelo gateway externo.'],
  ['E1', 'TP4', 'Batch', 1, 'O gateway entrega o lembrete ao canal do paciente.'],
  ['TP4', 'A', 'Assincrona', 2, 'O paciente recebe a notificação.'],
  ['A', 'P5', 'Critica', 3, 'O check-in precisa ser registrado de forma imediata.'],
  ['P5', 'DS', 'Sincrona', 2, 'O check-in atualiza o estado da consulta na agenda.'],
  ['P5', 'E2', 'Batch', 1, 'Os dados do atendimento são sincronizados com o prontuário/convênio.'],
];

// Fluxos principais F (entrada do método, seção 7.2): sequências de nós. F2 e F3 não começam no Ator.
const FLUXOS: Array<[string, string[]]> = [
  ['F1 – Agendamento', ['A', 'TP1', 'TP2', 'TP3', 'P2', 'DS']],
  ['F2 – Confirmação', ['P2', 'P3', 'S2', 'E1', 'TP4', 'A']],
  ['F3 – Lembrete', ['P2', 'P4', 'S2', 'E1', 'TP4', 'A']],
  ['F4a – Check-in e atualização interna', ['A', 'P5', 'DS']],
  ['F4b – Sincronização externa', ['A', 'P5', 'E2']],
];

/** Nós potencialmente críticos esperados (Quadro 31), para conferir o destaque no Mapa. */
export const GARGALOS_ESPERADOS = [NOS.TP2.nome, NOS.P2.nome, NOS.DS.nome];

/** Quantidade de fluxos principais cadastrados pela fixture. */
export const QUANTIDADE_DE_FLUXOS = FLUXOS.length;

/**
 * Cadastra pela API tudo do Estudo de Caso 2, EXCETO o nó Ator ("Paciente"), que o teste cria
 * pela interface — se o nó Ator já existir no caso, ele é reaproveitado. Se não existir, o
 * Paciente é o primeiro a ser criado (a ordem do Quadro 57 se mantém).
 */
export async function cadastrarClinicaPelaApi(request: APIRequestContext, casoId: string) {
  const ids = new Map<string, string>();
  const existentes = (await obterCaso(request, casoId)).nos;
  const paciente = existentes.find((n) => n.nome === NO_ATOR);
  if (paciente) ids.set('A', paciente.id);

  for (const [codigo, no] of Object.entries(NOS)) {
    if (ids.has(codigo)) continue;
    ids.set(codigo, (await criarNo(request, casoId, no)).id);
  }
  for (const [origem, destino, tipo, peso, justificativa] of ARESTAS) {
    const dados: CriarArestaRequest = { noOrigemId: ids.get(origem)!, noDestinoId: ids.get(destino)!, tipo, peso, justificativa };
    await criarAresta(request, casoId, dados);
  }
  for (const [nome, codigos] of FLUXOS) {
    await criarFluxo(request, casoId, { nome, nosIds: codigos.map((c) => ids.get(c)!) });
  }
}
