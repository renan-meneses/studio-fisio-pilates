export type Papel = 'Administrador' | 'Atendente' | 'Financeiro' | 'Profissional';

export const PAPEIS: { valor: Papel; rotulo: string }[] = [
  { valor: 'Administrador', rotulo: 'Administrador' },
  { valor: 'Atendente', rotulo: 'Atendente' },
  { valor: 'Financeiro', rotulo: 'Financeiro' },
  { valor: 'Profissional', rotulo: 'Profissional' },
];

export interface Usuario {
  id: string;
  nome: string;
  email: string;
  papel: Papel;
  ativo: boolean;
  ultimoLogin?: string;
  createdAt: string;
}
