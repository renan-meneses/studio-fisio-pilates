import { Servico } from '../../servicos/models/servico.model';

export interface Plano {
  id: string;
  nome: string;
  valor: number;
  descricao?: string;
  ativo: boolean;
  servicos: Servico[];
}

export interface CriarPlanoRequest {
  nome: string;
  valor: number;
  descricao?: string;
}