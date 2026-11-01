import { describe, expect, it } from 'vitest';
import { ehGuid, JUSTIFICATIVA_PENDENTE, justificativaEstaPendente } from './validacao';

describe('validacao', () => {
  it('aceita GUIDs em minúsculas e maiúsculas', () => {
    expect(ehGuid('a1b2c3d4-0000-4000-8000-000000000001')).toBe(true);
    expect(ehGuid('A1B2C3D4-0000-4000-8000-00000000000F')).toBe(true);
  });

  it.each(['', 'nao-e-guid', '123', 'a1b2c3d4-0000-4000-8000-00000000000', 'a1b2c3d4-0000-4000-8000-0000000000012', ' a1b2c3d4-0000-4000-8000-000000000001'])(
    'recusa %j',
    (valor) => {
      expect(ehGuid(valor)).toBe(false);
    },
  );
});

describe('justificativa pendente (RN03)', () => {
  it('reconhece o texto-padrão que a migração grava nas Arestas antigas, mesmo com espaços nas pontas', () => {
    expect(justificativaEstaPendente(JUSTIFICATIVA_PENDENTE)).toBe(true);
    expect(justificativaEstaPendente(`  ${JUSTIFICATIVA_PENDENTE}  `)).toBe(true);
  });

  it('uma justificativa de verdade não é pendente', () => {
    expect(justificativaEstaPendente('Paciente acessa o portal')).toBe(false);
    expect(justificativaEstaPendente('')).toBe(false);
  });
});
