// Contrato de docs/projeto/API.md (rotas /api/account).
export interface Usuario {
  id: string;
  email: string;
}

export interface EntrarRequest {
  email: string;
  senha: string;
}

export interface CadastrarRequest extends EntrarRequest {
  codigoDeConvite: string;
}
