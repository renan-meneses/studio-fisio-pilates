export interface Servico {
  id: string;
  nome: string;
  descricao?: string;
  valor: number;
  ativo: boolean;
}

export interface CriarServicoRequest {
  nome: string;
  descricao?: string;
  valor: number;
}