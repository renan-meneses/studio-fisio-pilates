export interface Aluno {
  id: string;
  nome: string;
  sobrenome?: string;
  nomeCompleto: string;
  endereco?: string;
  telefone?: string;
  email?: string;
  dataNascimento?: string;
  planoId?: string;
  planoNome?: string;
  ativo: boolean;
}

export interface CriarAlunoRequest {
  nome: string;
  sobrenome?: string;
  endereco?: string;
  telefone?: string;
  email?: string;
  dataNascimento?: string;
  planoId?: string;
}